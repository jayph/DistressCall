using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DistressCallPlugin
{
    public partial class DistressCallControl : UserControl
    {
        /// <summary>
        /// Reference to the plugin
        /// </summary>
        private DistressCallPlugin Plugin { get; }

        /// <summary>
        /// Empty constructor for the UI
        /// </summary>
        private DistressCallControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="plugin"></param>
        public DistressCallControl(DistressCallPlugin plugin) : this()
        {
            Plugin = plugin;
            DataContext = plugin.Config;

            // load the player ListView
            UpdatePlayerList();

            // indicate the server status on the ui
            ShowServerStatus();

        }

        /// <summary>
        /// Shows the server status on the UI
        /// </summary>
        public void ShowServerStatus()
        {
            // Indicate server running status on the UI
            if (Plugin.IsServerRunning)
            {
                ShowResponse("Server is running. All functions are available");

                // Enable AddGroup and AddEntity buttons as appropriate
                if (PlayerListView.SelectedIndex != -1)
                {
                    GroupAddButton.IsEnabled = true;
                }

                if (GroupListView.SelectedIndex != -1)
                {
                    EntityAddButton.IsEnabled = true;
                }
            }
            else
            {
                ShowResponse("Server is not running. Some functions are not available");

                // if we are in AddGroup UI we need to cancel it
                if (NewGroupName.Visibility == Visibility.Visible)
                {
                    // clear the text box
                    NewGroupName.Text = "";

                    // restore the UI
                    ShowGroupNameUI(false);

                }

                // if we are in the AddEntiy UI we need to cancel it
                if (NewEntityName.Visibility == Visibility.Visible)
                {
                    // clear the text box
                    NewEntityName.Text = "";

                    // restore the UI
                    ShowEntityNameUI(false);

                }

                // make sure AddGroup and AddEntity buttons are not enabled
                GroupAddButton.IsEnabled = false;
                EntityAddButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Load the ListView of PlayerNames from the plugin's database
        /// </summary>
        private void UpdatePlayerList()
        {
            ObservableCollection<string> players = new ObservableCollection<string>();
            foreach (DistressCallPlugin.PlayerEntry playerentry in DistressCallPlugin.DistressCallData.playerlist)
            {
                players.Add(playerentry.PlayerName);
            }
            // sort alphabetically
            players = new ObservableCollection<string>(players.OrderBy(i => i));
            PlayerListView.ItemsSource = players;
        }

        #region ListView Operations

        /// <summary>
        /// User has select a player. Show associated groups.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayerListViewItem_PreviewMouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            ListViewItem s = sender as ListViewItem;
            if ((s != null) && s.IsSelected)
            {
                // clear the group and entity lists
                GroupListView.ItemsSource = null;
                EntityListView.ItemsSource = null;

                // Get the PlayerEntry
                string name = PlayerListView.SelectedItem.ToString();
                //ShowResponse("Player selected = " + name); // BUGBUG

                if (name != string.Empty)
                {
                    if (GroupListViewUpdate(name))
                    {
                        // adjust buttons
                        GroupAddButton.IsEnabled = Plugin.IsServerRunning;
                        GroupRemoveButton.IsEnabled = false;
                        EntityAddButton.IsEnabled = false;
                        EntityRemoveButton.IsEnabled = false;
                    }
                }
            }
            else
            {
                // PlayerListViewItem unselected
                //ShowResponse("PlayerListViewItem unselected");

                // Clear the Group and Entity lists
                GroupListView.ItemsSource = null;
                EntityListView.ItemsSource = null;

                // adjust buttons
                GroupAddButton.IsEnabled = false;
                GroupRemoveButton.IsEnabled = false;
                EntityAddButton.IsEnabled = false;
                EntityRemoveButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Group item selected. Show associated entities.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupListViewItem_PreviewMouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            var s = sender as ListViewItem;
            if ((s != null) && s.IsSelected)
            {
                // clear the entity list
                EntityListView.ItemsSource = null;

                // Get the Player name
                string playername = PlayerListView.SelectedItem.ToString();
 
                // Get the Group name
                string groupname = GroupListView.SelectedItem.ToString();

                // update the Entity ListView
                if (!EntityListViewUpdate(playername, groupname))
                {
                    return;
                }

                // adjust buttons
                GroupAddButton.IsEnabled = Plugin.IsServerRunning;
                GroupRemoveButton.IsEnabled = true;
                EntityAddButton.IsEnabled = Plugin.IsServerRunning;
                EntityRemoveButton.IsEnabled = false;
            }
            else
            {
                // GroupListViewItem unselected
                //ShowResponse("GroupListViewItem unselected");

                // Clear the Entity lists
                EntityListView.ItemsSource = null;

                // adjust buttons
                GroupAddButton.IsEnabled = Plugin.IsServerRunning;
                GroupRemoveButton.IsEnabled = false;
                EntityAddButton.IsEnabled = false;
                EntityRemoveButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Entity item selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntityListViewItem_PreviewMouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            var s = sender as ListViewItem;
            if ((s != null) && s.IsSelected)
            {
                // adjust buttons
                GroupAddButton.IsEnabled = Plugin.IsServerRunning;
                GroupRemoveButton.IsEnabled = true;
                EntityAddButton.IsEnabled = Plugin.IsServerRunning;
                EntityRemoveButton.IsEnabled = true;
            }
            else
            {
                // EntityListViewItem unselected
                //ShowResponse("EntityListViewItem unselected");

                // adjust buttons
                EntityAddButton.IsEnabled = Plugin.IsServerRunning;
                EntityRemoveButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Update the group ListView to show groups for the specified player
        /// </summary>
        /// <param name="playername"></param>
        /// <returns></returns>
        private bool GroupListViewUpdate(string playername)
        {
            DistressCallPlugin.PlayerEntry player = DistressCallPlugin.FindPlayerDataByName(playername);

            if (player != null)
            {
                // populate the Group ListView

                ObservableCollection<string> groups = new ObservableCollection<string>();
                foreach (DistressCallPlugin.GroupEntry group in player.grouplist)
                {
                    groups.Add(group.GroupName);
                }

                // sort alphabetically
                groups = new ObservableCollection<string>(groups.OrderBy(i => i));
                GroupListView.ItemsSource = groups;
                return true;
            }
            else
            {
                GroupListView.ItemsSource = null;
                return false;
            }
        }

        #endregion

        #region Group Name UI

        /// <summary>
        /// User clicked the 'add group name" button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupAdd_OnClick(object sender, RoutedEventArgs e)
        {
            // All we do is show the add group UI and wait for them to enter something
            ShowGroupNameUI(true);
            NewGroupName.Focus();
        }

        /// <summary>
        /// User clicked the 'Remove Group' Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupRemove_OnClick(object sender, RoutedEventArgs e)
        {
            // verify group is selected
            if (GroupListView.SelectedIndex >= 0)
            {
                // remove group from DB
                DistressCallPlugin.RemoveGroup((string)PlayerListView.SelectedItem, (string)GroupListView.SelectedItem);

                // update group list
                GroupListViewUpdate((string)PlayerListView.SelectedItem);

                // update ui
                EntityAddButton.IsEnabled = false;  // because removing the group means no group is selected
            }
        }

        /// <summary>
        /// new group name 'accept' button clicked. Validate entry, add to database and restore UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGroupNameAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            //ShowResponse(string.Empty);
            //OutputText.InvalidateVisual();
            // Validate contents of NewGroupName
            string playername = PlayerListView.SelectedItem.ToString();   // currently selected player

            if ((NewGroupName.Text != "") && (DistressCallPlugin.FindGroupDataByName(playername, NewGroupName.Text) == null))
            {
                // attempt to add new group name to the player's groups
                if (DistressCallPlugin.AddGroup(playername, NewGroupName.Text))
                {
                    //ShowResponse("Group '" + NewGroupName.Text + "' added to Player " + playername);
                    // update the group ListView
                    GroupListViewUpdate(playername);
                }
                else
                {
                    ShowResponse("AddGroup failed, likely because group already exists.");
                }
            }
            else
            {
                ShowResponse("Group name is empty.");
            }

            // restore UI
            NewGroupName.Text = "";
            ShowGroupNameUI(false);
        }

        /// <summary>
        /// New group name 'cancel' button clicked. Clear text box and restore UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGroupNameCancelButton_Click(object sender, RoutedEventArgs e)
        {
            // clear the text box
            NewGroupName.Text = "";

            // restore the UI
            ShowGroupNameUI(false);
        }

        /// <summary>
        /// Show or hide the group name UI and hide the add and remove group name buttons
        /// </summary>
        /// <param name="show"></param>
        private void ShowGroupNameUI(bool show)
        {
            Visibility v = show ? Visibility.Visible : Visibility.Hidden;

            NewGroupName.Visibility = v;
            NewGroupNameAcceptButton.Visibility = v;
            NewGroupNameCancelButton.Visibility = v;
            GroupAddButton.IsEnabled = !show;
            GroupRemoveButton.IsEnabled = !show;
        }

        #endregion

        #region Entity Name UI

        /// <summary>
        /// AddEntity button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntityAdd_OnClick(object sender, RoutedEventArgs e)
        {
            ShowEntityNameUI(true);
            NewEntityName.Focus();
        }

        /// <summary>
        /// RemoveEntity button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntityRemove_OnClick(object sender, RoutedEventArgs e)
        {
            // verify entity is selected
            if ((GroupListView.SelectedIndex >= 0) && (EntityListView.SelectedIndex >= 0))
            {
                // remove entity from DB
                DistressCallPlugin.RemoveEntity((string)PlayerListView.SelectedItem, (string)GroupListView.SelectedItem, (string)EntityListView.SelectedItem);

                // update entity list
                EntityListViewUpdate((string)PlayerListView.SelectedItem, (string)GroupListView.SelectedItem);

                // update ui
                EntityAddButton.IsEnabled = false;  // because removing the group means no group is selected
            }
        }

        /// <summary>
        /// NewEntityName Accept button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewEntityNameAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            //ShowResponse("NewEntityNameAcceptButton_Click");
            //return;

            // Validate faction or person name
            if (Plugin.IsValidFactionNameOrTag(NewEntityName.Text))
            {
                //ShowResponse("Faction: '" + NewEntityName.Text + "' is valid.");
                // attempt to add the entity
                if (DistressCallPlugin.AddEntity(PlayerListView.SelectedItem.ToString(), GroupListView.SelectedItem.ToString(), NewEntityName.Text))
                {
                    // attempt successful
                    //ShowResponse("Faction: '" + NewEntityName.Text + "' added.");

                    // update ListView
                    EntityListViewUpdate(PlayerListView.SelectedItem.ToString(), GroupListView.SelectedItem.ToString());
                    // restore UI
                    NewEntityName.Text = "";
                    ShowEntityNameUI(false);
                    return;
                }

                // add entity failed
                ShowResponse("Faction: '" + NewEntityName.Text + "' failed to add.");

                // restore UI
                NewEntityName.Text = "";
                ShowEntityNameUI(false);
                return;
            }
            else
            {
                // invalid faction name or tag
                ShowResponse("Faction/Person: '" + NewEntityName.Text + "' not valid.");
            }

            // restore UI
            NewEntityName.Text = "";
            ShowEntityNameUI(false);
        }

        /// <summary>
        /// NewEntityName Cancel button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewEntityNameCancelButton_Click(object sender, RoutedEventArgs e)
        {
            // clear the text box
            NewEntityName.Text = "";

            // restore the UI
            ShowEntityNameUI(false);
        }

        /// <summary>
        /// Update the Entity ListView
        /// </summary>
        /// <param name="playername"></param>
        /// <param name="groupname"></param>
        /// <returns></returns>
        private bool EntityListViewUpdate(string playername, string groupname)
        {
            if ((playername == string.Empty) || (groupname ==string.Empty))
            {
                return false;
            }

            // clear the entity list
            EntityListView.ItemsSource = null;

            // Get the PlayerEntry
            DistressCallPlugin.PlayerEntry player = DistressCallPlugin.FindPlayerDataByName(playername);
            if (player == null)
            {
                return false;
            }

            // Get the Group entry
            DistressCallPlugin.GroupEntry group = DistressCallPlugin.FindGroupDataByName(playername, groupname);

            if (group == null)
            {
                return false;
            }

            // populate the Fation/Person ListView

            ObservableCollection<string> entities = new ObservableCollection<string>();
            foreach (string faction in group.factionlist)
            {
                entities.Add("Fac: " + faction);
            }
            foreach (string person in group.personlist)
            {
                entities.Add("Per: " + person);
            }

            // sort alphabetically
            entities = new ObservableCollection<string>(entities.OrderBy(i => i));
            EntityListView.ItemsSource = entities;

            return true;
        }

        /// <summary>
        /// Show or hide the NewEntityName UI
        /// </summary>
        /// <param name="show"></param>
        private void ShowEntityNameUI(bool show)
        {
            Visibility v = show ? Visibility.Visible : Visibility.Hidden;

            EntityTypeComboBox.Visibility = v;
            EntityTypeComboBox.SelectedIndex = 0;
            NewEntityName.Visibility = v;
            NewEntityNameAcceptButton.Visibility = v;
            NewEntityNameCancelButton.Visibility = v;
            EntityAddButton.IsEnabled = !show;
            EntityRemoveButton.IsEnabled = !show;
        }

        #endregion

        /// <summary>
        /// Provide feedback in the TextBlock at the bottom of the UI
        /// </summary>
        /// <param name="response"></param>
        /// <param name="append"></param>
        private void ShowResponse(string response, bool append = true)
        {
            if (append)
            {
                OutputText.Text += ("\n" + response);
            }
            else
            {
                OutputText.Text = response;
            }
            ResponseScroll.ScrollToBottom();
        }
    }
}
