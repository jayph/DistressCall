using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System.Collections;
using System.Collections.Generic;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Collections;
using VRage.Game.ModAPI;
using VRageMath;



namespace DistressCallPlugin
{
    [Category("DistressCall")]
    public class DistressCallCommands : CommandModule
    {
        private static string AdminDisabledMessage = "This command is currently disabled";

        public DistressCallPlugin Plugin => (DistressCallPlugin)Context.Plugin;

        [Category("distress")]
        public class Distress : CommandModule
        {

            /// <summary>
            /// !distress addgroup <group name>
            /// </summary>
            /// <param name="groupname"></param>
            [Command("addgroup", "Adds specified group name to a player's call groups. Group names 'Friendly' and 'Neutral' are predefined.")]
            [Permission(MyPromoteLevel.None)]
            public void DistressAddGroup(string groupname)
            {
                //Context.Respond("distress addgroup: " + groupname);
                DistressCallPlugin foo = (DistressCallPlugin)Context.Plugin;
                
                if (!((DistressCallPlugin)Context.Plugin).Config.Enabled)
                {
                    Context.Respond(AdminDisabledMessage);
                    return;
                }

                if (DistressCallPlugin.AddGroup(Context.Player.DisplayName, groupname, true))
                {
                    Context.Respond("distress addgroup: '" + groupname + "' added for player: " + Context.Player.DisplayName);
                    return;
                }
                else
                {
                    Context.Respond("distress addgroup: '" + groupname + "' already exists for player: " + Context.Player.DisplayName);
                }

            }

            /// <summary>
            /// !distress deletegroup <group name>
            /// </summary>
            /// <param name="groupname"></param>
            [Command("deletegroup", "Deletes specified group name to a player's call groups. Group names 'Friendly' and 'Neutral' are predefined and cannot be removed.")]
            [Permission(MyPromoteLevel.None)]
            public void DistressDeleteGroup(string groupname)
            {
                //Context.Respond("distress deletegroup: " + groupname);
                if (!((DistressCallPlugin)Context.Plugin).Config.Enabled)
                {
                    Context.Respond(AdminDisabledMessage);
                    return;
                }

                DistressCallPlugin.RemoveGroup(Context.Player.DisplayName, groupname);
            }

            /// <summary>
            /// !distress add <faction tag | player name> <group name>
            /// </summary>
            /// <param name="entityname"></param>
            /// <param name="groupname"></param>
            [Command("add", "Adds specified faction (tag) or player to a specified group.")]
            [Permission(MyPromoteLevel.None)]
            public void DistressAdd(string entityname, string groupname)
            {
                //Context.Respond("distress add: " + entityname + ", " + groupname);
                if (!((DistressCallPlugin)Context.Plugin).Config.Enabled)
                {
                    Context.Respond(AdminDisabledMessage);
                    return;
                }

                if (!DistressCallPlugin.AddEntity(Context.Player.DisplayName, groupname, entityname))
                {
                    Context.Respond("distress add: failed. Either no player or faction found with this name or tag: " + entityname + ", or player/faction was NPC");
                }
            }

            /// <summary>
            /// !distress delete <faction tag | player name> <group name>
            /// </summary>
            /// <param name="entityname"></param>
            /// <param name="groupname"></param>
            [Command("delete", "Deletes the specified faction (tag) or player from a group.")]
            [Permission(MyPromoteLevel.None)]
            public void DistressDelete(string entityname, string groupname)
            {
                //Context.Respond("distress delete: " + entityname + ", " + groupname);
                if (!((DistressCallPlugin)Context.Plugin).Config.Enabled)
                {
                    Context.Respond(AdminDisabledMessage);
                    return;
                }

                DistressCallPlugin.RemoveEntity(Context.Player.DisplayName, groupname, entityname);
            }

            /// <summary>
            /// !distress call <group name>
            /// </summary>
            /// <param name="groupname"></param>
            [Command("call", "Sends automated distress notification to all members online of a specified group.")]
            [Permission(MyPromoteLevel.None)]
            public void DistressCallCmd(string groupname)
            {
                //Context.Respond("distress call: " + groupname);
                if (!((DistressCallPlugin)Context.Plugin).Config.Enabled)
                {
                    Context.Respond(AdminDisabledMessage);
                    return;
                }

                // list of steam IDs to receive the message
                List<ulong> steamIds = DistressCallPlugin.GetSteamIds(Context.Player.DisplayName, groupname);
                if (steamIds == null)
                {
                    Context.Respond("distress call: no such player or group");
                    return;
                }

                string msg = "DISTRESS CALL - " + Context.Player.DisplayName;
                //Vector3D playerPos = Context.Player.GetPosition();
                //string gpsPosition = "GPS:" + Context.Player.DisplayName + " Distress Call:" + playerPos.X + ":" + playerPos.Y + ":" + playerPos.Z + ":" + "#FF9D7F:";
                var gridGPS = MyAPIGateway.Session?.GPS.Create(Context.Player.DisplayName + " Distress Call", "Distress Call", Context.Player.GetPosition(), true);
                gridGPS.GPSColor = new Color(251, 51, 255);

                // send message to each steamId in the list
                MyPlayer player;
                foreach (ulong id in steamIds)
                {
                    DistressCallPlugin.chatManager?.SendMessageAsOther(Context.Player.DisplayName, msg, Color.Yellow, id);
                    MySession.Static.Players.TryGetPlayerBySteamId(id, out player);
                    MyAPIGateway.Session?.GPS.AddGps(player.Identity.IdentityId, gridGPS);
                    //VRage.Game.ModAPI.IMyGpsCollection.ModifyGps(player.Identity.IdentityId, gridGPS);
                }
            }

            /// <summary>
            /// !distress list <group name | null>
            /// </summary>
            /// <param name="groupname"></param>
            [Command("list", "Lists specified group or Lists all groups.")]
            [Permission(MyPromoteLevel.None)]
            public void DistressList(string groupname = "")
            {
                //Context.Respond("distress list: " + groupname);
                if (!((DistressCallPlugin)Context.Plugin).Config.Enabled)
                {
                    Context.Respond(AdminDisabledMessage);
                    return;
                }

                // get players data
                DistressCallPlugin.PlayerEntry playerentry = DistressCallPlugin.FindPlayerDataByName(Context.Player.DisplayName);
                if (playerentry == null)
                {
                    Context.Respond("no player record found");
                }
                foreach (var group in playerentry.grouplist)
                {
                    string line = group.GroupName + ": Factions: ";
                    bool first = true;
                    foreach (var faction in group.factionlist)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            line += ", ";
                        }
                        line += faction;
                    }

                    line += "; Persons: ";
                    first = true;
                    foreach (var person in group.personlist)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            line += ", ";
                        }
                        line += person;
                    }

                    Context.Respond(line);
                }
            }
        }

    }

}
