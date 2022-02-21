using NLog;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Serialization;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;

namespace DistressCallPlugin
{
    public class DistressCallPlugin : TorchPluginBase, IWpfPlugin
    {
        /// <summary>
        /// reference to torch logging system
        /// </summary>
        public static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// plugin configuration filename
        /// </summary>
        private static readonly string CONFIG_FILE_NAME = "DistressCallConfig.cfg";

        /// <summary>
        /// this is the container for our UI
        /// </summary>
        private DistressCallControl _control;
        public UserControl GetControl() => _control ?? (_control = new DistressCallControl(this));

        /// <summary>
        /// our serialized configuration
        /// </summary>
        private Persistent<DistressCallConfig> _config;
        public DistressCallConfig Config => _config?.Data;

        /// <summary>
        /// xml file used to store distress call players, groups, and group members
        /// </summary>
        private const string DistressCallFile = "DistressCall.cfg";

        /// <summary>
        /// interface to the chat system
        /// </summary>
        public static IChatManagerServer chatManager = null;

        /// <summary>
        /// Indicates whther or not server is running
        /// Set by callbacks. Used to limit access in the UI.
        /// </summary>
        public bool IsServerRunning = false;


        #region Database definitions

        /// <summary>
        /// Distress Call Database
        /// 
        /// Structure
        ///
        ///  players
        ///      groups
        ///          factions
        ///          persons
        /// </summary>
        public static DistressCallDB DistressCallData = new DistressCallDB();

        public class DistressCallDB
        {
            [XmlElement("Player")]
            public List<PlayerEntry> playerlist = new List<PlayerEntry>();
        }

        public class PlayerEntry
        {
            [XmlAttribute("Name")]
            public string PlayerName { get; set; }
            [XmlElement("Group")]
            public List<GroupEntry> grouplist = new List<GroupEntry>();
        }

        public class GroupEntry
        {
            [XmlAttribute("Name")]
            public string GroupName { get; set; }
            [XmlElement("Faction")] 
            public List<string> factionlist = new List<string>();
            [XmlElement("Person")]
            public List<string> personlist = new List<string>();
        }
        #endregion

        /// <summary>
        /// Plugin initialization
        /// </summary>
        /// <param name="torch"></param>
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            SetupConfig();

            GetDistressCallData();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
                sessionManager.SessionStateChanged += SessionChanged;
            else
                Log.Warn("No session manager loaded!");

            Save();
        }

        /// <summary>
        /// Callback handler for session state change.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="state"></param>
        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state)
            {
                case TorchSessionState.Loaded:
                    Log.Info("Session Loaded!");
                    chatManager = session.Managers.GetManager<IChatManagerServer>();
                    IsServerRunning = true;
                    _control.Dispatcher.Invoke(() =>
                    {
                        _control.ShowServerStatus();
                    });
                    break;

                case TorchSessionState.Unloading:
                    Log.Info("Session Unloading!");
                    chatManager = null;
                    IsServerRunning = false;
                    _control.Dispatcher.Invoke(() =>
                    {
                        _control.ShowServerStatus();
                    });
                    break;
            }
        }

        /// <summary>
        /// Initialization of the configuration system
        /// </summary>
        private void SetupConfig()
        {
            var configFile = Path.Combine(StoragePath, CONFIG_FILE_NAME);

            try
            {
                _config = Persistent<DistressCallConfig>.Load(configFile);
            }
            catch (Exception e)
            {
                Log.Warn(e);
            }

            if (_config?.Data == null)
            {
                Log.Info("Create Default Config, because none was found!");

                _config = new Persistent<DistressCallConfig>(configFile, new DistressCallConfig());
                _config.Save();
            }
        }

        /// <summary>
        /// Configuration save
        /// </summary>
        public void Save()
        {
            try
            {
                _config.Save();
                Log.Info("Configuration Saved.");
            }
            catch (IOException e)
            {
                Log.Warn(e, "Configuration failed to save");
            }
        }

        /// <summary>
        /// Load the Distress Call database from its xml file
        /// </summary>
        public void GetDistressCallData()
        {
            // BUGBUG - How do I know that the files are in the 'Instance' sub-dir? Run with it for now.
            string path = Directory.GetCurrentDirectory() + @"\Instance\" + DistressCallFile;

            //Log.Info("GetDistressCallData: path = " + path);
            if (!File.Exists(path))
            {
                // Create an empty file by writing the empty database
                SaveDistressCallData();
                return;
            }

            // Deserialize the player distress call data
            XmlSerializer deserializer = new XmlSerializer(typeof(DistressCallDB));
            TextReader reader = new StreamReader(path);
            DistressCallData = (DistressCallDB)deserializer.Deserialize(reader);
        }

        /// <summary>
        /// Save the Distress Call database to its xml file.
        /// </summary>
        public static void SaveDistressCallData()
        {
            // BUGBUG - How do I know that the files are in the 'Instance' sub-dir? Run with it for now.
            string path = Directory.GetCurrentDirectory() + @"\Instance\" + DistressCallFile;

            //Log.Info("WriteDistressCallData: path = " + path);
            XmlSerializer serializer = new XmlSerializer(typeof(DistressCallDB));
            using (StreamWriter writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, DistressCallData);
            }

        }

        /// <summary>
        /// Using the playername string, find the matching player record
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static PlayerEntry FindPlayerDataByName(string name)
        {
            if (name != "")
            {
                PlayerEntry player = DistressCallData.playerlist.Find(x => x.PlayerName == name);
                return player;
            }
            return null;
        }

        /// <summary>
        /// Using the playername and groupname strings, find the matching group record
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        public static GroupEntry FindGroupDataByName(string playername, string groupname)
        {
            if ((playername == "") || (groupname == ""))
            {
                return null;
            }

            PlayerEntry player = DistressCallData.playerlist.Find(x => x.PlayerName == playername);
            if (player == null)
            {
                return null;
            }
            GroupEntry group = player.grouplist.Find(x => x.GroupName == groupname);
            return group;
        }

        /// <summary>
        /// Add a new player to the database along with the default groups.
        /// </summary>
        /// <param name="playername"></param>
        /// <returns></returns>
        public static PlayerEntry AddPlayer(string playername)
        {
            // create player data
            PlayerEntry pd = new PlayerEntry();
            pd.PlayerName = playername;

            // add it to the database
            DistressCallData.playerlist.Add(pd);

            // Save the database
            SaveDistressCallData();

            // create 'Friendly' and 'Neutral' call groups
            if (!AddGroup(playername, "Friendly") || !AddGroup(playername, "Neutral"))
            {
                // Something failed
                return null;
            }

            return pd;
        }

        /// <summary>
        /// Attempt to add a group to the given player.
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="groupname"></param>
        /// <param name="addplayer= false"></param>
        /// <returns>Fail if playername does not exists, or if groupname already exists</returns>
        public static bool AddGroup(string playername, string groupname, bool addplayer = false)
        {
            PlayerEntry player = FindPlayerDataByName(playername);
            if (player == null)
            {
                if (!addplayer)
                {
                    return false;   // no such player
                }

                // add new player to the database
                player = AddPlayer(playername);
            }

            GroupEntry group = FindGroupDataByName(playername, groupname);
            if (group != null)
            {
                return false;   // group already exists
            }

            // Add the group to the player
            group = new DistressCallPlugin.GroupEntry();
            group.GroupName = groupname;

            // add the group to the player's data
            player.grouplist.Add(group);

            // Save the database
            SaveDistressCallData();

            return true;
        }

        /// <summary>
        /// Remove the named group from the named player
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="groupname"></param>
        public static void RemoveGroup(string playername, string groupname)
        {
            PlayerEntry player = FindPlayerDataByName(playername);
            if (player == null)
            {
                return;
            }

            GroupEntry group = FindGroupDataByName(playername, groupname);
            if (group != null)
            {
                player.grouplist.Remove(group);
                // Save the database
                SaveDistressCallData();
            }

        }

        /// <summary>
        /// Attempts to add either a faction entity or a person entity to the specified player and group.
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="groupname"></param>
        /// <param name="entityname"></param>
        /// <returns></returns>
        public static bool AddEntity(string playername, string groupname, string entityname)
        {
            GroupEntry group = FindGroupDataByName(playername, groupname);
            if (group == null)
            {
                return false;
            }

            MyPlayer gameplayer = MySession.Static.Players.GetPlayerByName(entityname);
            bool entityTypePlayer = (gameplayer != null);

            if (entityTypePlayer)
            {
                // NPC entities not allowed
                if (!gameplayer.IsRealPlayer)
                {
                    return false;
                }

                // don't add if already in database
                if (group.personlist.Contains(entityname))
                {
                    // already in list, we can report success
                    return true;
                }

                // add the person
                group.personlist.Add(entityname);
                // Save the database
                SaveDistressCallData();

                return true;
            }
            else
            {
                string factionname = "";
                string factiontag = "";
                bool isNPC = false;

                MyFaction[] factionlist = MySession.Static.Factions.GetAllFactions();
                foreach (MyFaction faction in factionlist)
                {
                    if (faction.Name == entityname)
                    {
                        factionname = faction.Name;
                        factiontag = faction.Tag;
                        isNPC = faction.IsEveryoneNpc();
                        break;
                    }
                    else if (faction.Tag == entityname)
                    {
                        factionname = faction.Name;
                        factiontag = faction.Tag;
                        isNPC = faction.IsEveryoneNpc();
                        break;
                    }
                }

                // don't allow NPC factions
                if ((factiontag == "") || isNPC)
                {
                    return false;
                }

                // make combination name of tag + name
                string comboname = factiontag + " - " + factionname;
                // don't add if already in database
                if (group.factionlist.Contains(comboname))
                {
                    // already in list so we can report success
                    return true;
                }
                // add the faction tag
                group.factionlist.Add(comboname);
                // Save the database
                SaveDistressCallData();
                return true;
            }
        }

        /// <summary>
        /// Remove the faction entity or person entity from the specified player and group
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="groupname"></param>
        /// <param name="entityname"></param>
        public static void RemoveEntity(string playername, string groupname, string entityname)
        {
            GroupEntry group = FindGroupDataByName(playername, groupname);
            if (group == null)
            {
                // no such group or player, so we are done
                return;
            }

            // strip prefix from entity name
            // BUGBUG - this is hard coded to remove the first 5 chars: "Fac: " or "Per: "
            string strippedEntityName = entityname.Substring(5);

            // don't know whether entityname is a faction or a person, so we'll try to remove both
            // BUGBUG - could be more specific
            group.factionlist.Remove(strippedEntityName);
            group.personlist.Remove(strippedEntityName);

            // either it was never there, or we just removed it.
        }

        /// <summary>
        /// Returns a list of steamIds for everyone is the specified group, for the specified player
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="groupname"></param>
        /// <returns>List of steamIds</returns>
        public static List<ulong> GetSteamIds(string playername, string groupname)
        {
            // get a reference to the requested player/group combination
            GroupEntry group = FindGroupDataByName(playername, groupname);
            if (group == null)
            {
                // no such group or player
                return null;
            }

            List<ulong> steamIds = new List<ulong>();

            // Make a list of all currently online players
            ICollection<MyPlayer> playerList = MySession.Static.Players.GetOnlinePlayers();

            // Look at each online player. If they are in this group (either as a person or a faction member) add their steamId to the list
            foreach (MyPlayer player in playerList)
            {
                // if they are in one of the factions in this group
                foreach (var faction in group.factionlist)
                {
                    MyFaction myfaction = MySession.Static.Factions.TryGetFactionByTag(faction);
                    if (myfaction == null)
                    {
                        continue;
                    }

                    if (myfaction.IsMember((long)player.Id.SteamId) && !steamIds.Contains(player.Id.SteamId))
                    {
                        // add it to the list
                        steamIds.Add(player.Id.SteamId);
                    }
                }

                // or if they are in the person list
                foreach (string pname in group.personlist)
                {
                    if ((pname == player.DisplayName) && !steamIds.Contains(player.Id.SteamId))
                    {
                        // add it to the list
                        steamIds.Add(player.Id.SteamId);
                    }
                }
            }

            return steamIds;
        }

        /// <summary>
        /// Check if the faction string is either a valid faction name or valid faction tag
        /// </summary>
        /// <param name="factionstr"></param>
        /// <returns></returns>
        public bool IsValidFactionNameOrTag(string factionstr)
        {
            // see if the param is a valid faction tag
            if (MySession.Static.Factions.TryGetFactionByTag(factionstr) != null)
            {
                // found the faction tag
                return true;
            }

            // see if the param is a valid faction name
            if (MySession.Static.Factions.FactionNameExists(factionstr))
            {
                // found the faction name
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the playername string is a valid player in the game
        /// </summary>
        /// <param name="playername"></param>
        /// <returns></returns>
        public bool IsValidPlayerName(string playername)
        {
            // see if the param is a valid player name
            if (MySession.Static.Players.GetPlayerByName(playername) != null)
            {
                // found the player name
                return true;
            }
            return false;
        }
    }
}
