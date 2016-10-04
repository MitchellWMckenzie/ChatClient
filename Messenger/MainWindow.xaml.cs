using MahApps.Metro.Controls.Dialogs;
using Messenger.Classes;
using Messenger.Classes.Server;
using Messenger.Classes.Extension_Methods.ObservableList;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Messenger.Classes.PictureScreen;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Messenger.Classes.Login;

namespace Messenger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        #region Attributes

        Server server = null;

        //Logging in/Registering flyover
        ProgressDialogController progCntrler;

        //Stores the Smilies with their parameters
        public Dictionary<string, BitmapImage> smilies = new Dictionary<string, BitmapImage>() { };
        public Dictionary<string, Dictionary<string, string>> sParams = new Dictionary<string, Dictionary<string, string>>() { };

        //Store the list of users
        public ObservableCollection<Users> listofUsers = new ObservableCollection<Users>();

        //Stores the different colors used
        public Dictionary<string, SolidColorBrush> colors = new Dictionary<string, SolidColorBrush>()
        {
            { "Blue", (SolidColorBrush)(new BrushConverter().ConvertFrom("#2196F3")) },
            { "Green", new SolidColorBrush(Colors.LawnGreen) },
            { "Orange", new SolidColorBrush(Colors.Orange) },
            { "Purple", new SolidColorBrush(Colors.MediumPurple) },
            {
                "Yellow", (SolidColorBrush)(new BrushConverter().ConvertFrom("#FADA5E"))
            },
            { "Red", (SolidColorBrush)(new BrushConverter().ConvertFrom("#F44336")) },
            { "Black", new SolidColorBrush(Colors.Black) },
            { "White", new SolidColorBrush(Colors.White) }
        };

        //Event for new messages
        IMReceivedEventHandler receivedHandler;

        //Messages
        public List<Message> txtMessages = new List<Message>();

        #endregion


        public MainWindow()
        {
            InitializeComponent();

            //Load previous username
            loadUsername();

            //Set the item source of the messages
            txtMessagesView.ItemsSource = txtMessages;
            lstUsers.ItemsSource = listofUsers;
        }


        #region Load information from file
        string usersFileName = Environment.CurrentDirectory + "\\information.dat";
        public void loadUsername()
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = new FileStream(usersFileName, FileMode.Open, FileAccess.Read);
                if (file.Length > 0)
                {
                    LogInInformation infos = (LogInInformation)bf.Deserialize(file);      // Deserialize UserInfo array
                    txtServerLocation.Text = infos.serverName;
                    if (infos.SaveInfo)
                    {
                        if (infos.Username != "----")
                            txtLogIn.Text = infos.Username;
                        chkRememberName.IsChecked = true;
                        txtPassword.Focus();
                    }
                    else
                    {
                        txtLogIn.Focus();
                    }
                }
                else
                {
                    //No saved information found
                }
                file.Close();
            }
            catch { }
        }

        public void saveUsernameToFile()
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = new FileStream(usersFileName, FileMode.Create, FileAccess.Write);
                LogInInformation lg = new LogInInformation();
                if (chkRememberName.IsChecked == true)
                    lg.Username = txtLogIn.Text;
                else
                    lg.Username = "----";
                lg.serverName = txtServerLocation.Text;
                lg.SaveInfo = (bool)chkRememberName.IsChecked;
                bf.Serialize(file, lg);  // Serialize UserInfo array
                file.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        #endregion

        #region Snipping Tool
        private void btnSnip_Click(object sender, RoutedEventArgs e)
        {
            SnippingTool.Snip();
        }
        #endregion

        #region User Methods
        public void addUserToList(Users user)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                user.setMainWindow(this);
                user.Source = smilies["message"];
                listofUsers.Add(user);
            }));
        }

        public void clearUsers()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                listofUsers = new ObservableCollection<Users>();
                lstUsers.ItemsSource = listofUsers;
            }));
        }

        public void removeUserFromList(string user)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                listofUsers.Remove(x => x.Name.Equals(user));
            }));
        }

        public void refreshList()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                lstUsers.Items.Refresh();
            }));
        }

        #endregion

        #region Send Message

        /// <summary>
        /// Sends the message to the server when the button is clicked or when called from another method
        /// </summary>
        private void SendMessage(object sender, RoutedEventArgs e)
        {
            if (txtMessage.Text.Trim().Length != 0)
            {
                server.SendMessage("All", txtMessage.Text, clrMessage.Text);
                Dispatcher.Invoke(new Action(() =>
                {
                    txtMessage.Clear();
                    server.updateMessaging(false);
                }));
            }
        }

        /// <summary>
        /// Called everytime a key is typed in the message sending box. Used to keep track of multiple lines
        /// in the message box
        /// </summary>
        bool keyDown = false;
        private void UpdateMessanging(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    base.OnKeyDown(e);
                }
                else if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    e.Handled = true;
                    SendMessage(sender, e);
                    keyDown = false;
                }
            }
            else if (txtMessage.Text.Trim() != "")
            {
                if (!keyDown)
                {
                    server.updateMessaging(true);
                    keyDown = true;
                }
            }
            else
            {
                if (keyDown)
                {
                    server.updateMessaging(false);
                    keyDown = false;
                }
            }
        }

        /// <summary>
        /// Refreshes the messages in the listview
        /// </summary>
        public void updateMessages()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                txtMessagesView.Items.Refresh();
            }));
        }

        #endregion

        #region Smilies

        /// <summary>
        /// Adds a smilies to the list of images
        /// </summary>
        /// <param name="key">The key for the Dictionary</param>
        /// <param name="img">The Image</param>
        public void addSmilie(string key, BitmapImage img)
        {
            //Have to freeze the image to be able to use it from another thread
            img.Freeze();

            //On the proper thread add the smilie to the dictionary
            Dispatcher.Invoke(new Action(() =>
            {
                this.smilies.Add(key, img);
            }));
        }

        #endregion

        #region Setting User Information

        public void updateSettingsInformation(bool isMod, string nameColor, string font, string fontsize, string messageColor)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (isMod)
                {
                    ModeratorSettings.Visibility = Visibility.Visible;
                }

                int index = -1;
                for (int x = 0; x < clrMessage.Items.Count && index == -1; x++)
                {
                    if (clrMessage.Items.GetItemAt(x).ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last() == ToTitleCase(nameColor))
                    {
                        index = x;
                    }
                }
                clrMessage.SelectedIndex = index;

                index = -1;
                for (int x = 0; x < clrFont.Items.Count && index == -1; x++)
                {
                    string testing = clrFont.Items.GetItemAt(x).ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last();
                    if (testing.ToLower().Equals(font.ToLower().Trim()))
                    {
                        index = x;
                    }
                }
                if (index == -1)
                    index = 0;
                clrFont.SelectedIndex = index;

                index = -1;
                for (int x = 0; x < clrFontSize.Items.Count && index == -1; x++)
                {
                    if (clrFontSize.Items.GetItemAt(x).ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last() == fontsize)
                    {
                        index = x;
                    }
                }
                clrFontSize.SelectedIndex = index;

                index = -1;
                for (int x = 0; x < clrMessageColor.Items.Count && index == -1; x++)
                {
                    if (clrMessageColor.Items.GetItemAt(x).ToString().Split(new string[] { ": " }, StringSplitOptions.None).Last() == ToTitleCase(messageColor))
                    {
                        index = x;
                    }
                }
                clrMessageColor.SelectedIndex = index;
            }));
        }

        #endregion

        #region Window Events
        private void mainWindow_Activated(object sender, EventArgs e)
        {
            FlshWindow.StopFlashingWindow(this);
        }
        #endregion

        #region Events

        void im_MessageReceived(object sender, IMReceivedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                txtMessages.Add(new Classes.Message(e.ID, e, this));
                txtMessagesView.Items.Refresh();

                var scrollViewer = GetScrollViewer(txtMessagesView) as ScrollViewer;
                scrollViewer.CanContentScroll = false;
                scrollViewer.ScrollToEnd();
            }));
        }

        /// <summary>
        /// Event called when log in was valid and good
        /// </summary>
        void im_LoginOK(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this.ResizeMode = ResizeMode.CanResize;
                //Finish the progressbar and close it
                progCntrler.SetTitle("Success!");
                #pragma warning disable CS4014 // Makes the warning go away.
                IncrementProgressBar(progCntrler, "Successfully logged in to the account!", 800, 1000, 0);
                CloseFlyover(progCntrler, 2000, true);
            }));
        }

        /// <summary>
        /// Event called when Registration was valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void im_RegisterOK(object sender, EventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //Finish the progressbar and close it
                progCntrler.SetTitle("Success!");
                #pragma warning disable CS4014 // Makes the warning go away.
                IncrementProgressBar(progCntrler, "Successfully created a new account!", 800, 1000, 0);
                CloseFlyover(progCntrler, 2000, true);
            }));
        }

        /// <summary>
        /// Event called when registration failed
        /// </summary>
        void im_RegisterFailed(object sender, IMErrorEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //Check the type of error
                string error = "";
                switch ((int)e.Error)
                {
                    case Protocols.IM_TooUsername:
                        error = "Username entered is to long";
                        break;
                    case Protocols.IM_TooPassword:
                        error = "Password entered is to short";
                        break;
                    case Protocols.IM_NoExists:
                    case Protocols.IM_WrongPass:
                        error = "Invlaid Username or Password";
                        break;
                    case Protocols.IM_Banned:
                        error = "Account currently banned. Unable to log in.";
                        break;
                    case Protocols.IM_Exists:
                        error = "Username selected is invalid.";
                        break;
                    default:
                        error = "Error Registering!";
                        break;
                }

                //Close the progress bar
                progCntrler.SetTitle("Error!");
                progCntrler.SetMessage(error);
                CloseFlyover(progCntrler, 3000);
            }));
        }

        /// <summary>
        /// Event called when log in failed
        /// </summary>
        void im_LoginFailed(object sender, IMErrorEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                //Check the type of error
                string error = "";
                switch ((int)e.Error)
                {
                    case Protocols.IM_TooUsername:
                        error = "Username entered is to long";
                        break;
                    case Protocols.IM_TooPassword:
                        error = "Password entered is to short";
                        break;
                    case Protocols.IM_NoExists:
                    case Protocols.IM_WrongPass:
                        error = "Invlaid Username or Password";
                        break;
                    case Protocols.IM_Banned:
                        error = "Account currently banned. Unable to log in.";
                        break;
                    default:
                        error = "Error logging in!";
                        break;
                }

                //Finish the progress bar
                progCntrler.SetTitle("Error!");
                progCntrler.SetMessage(error);
                CloseFlyover(progCntrler, 3000);
            }));
        }
        #endregion

        #region Log In Button

        /// <summary>
        /// Handles when the user presses enter on the password box
        /// </summary>
        private void keyDownPassword(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                LogIn(sender, e);
        }

        /// <summary>
        /// Used for when loading the program.
        /// </summary>
        private string[] randomLoadingMessages =
        {
            "Checking weather...",
            "Squishing Bugs...",
            "Oh crap, I am still loading...",
            "Searching for Trysten's Lung...",
            "Contacting Facebook to report usage...",
            "Playing a quick game in my T110E5...",
            "Attacking the Wall of Flesh...",
            "Trying to play n++...",
            "Destorying all the bugs...",
            "Destroying the world...",
            "Gaining Sentience...",
            "We didn't penetrate their armor!",
            "Deleting System32...",
            "Compiling Errors...",
            "Squeezing Lemons...",
            "Boarding the Hype Train...",
            "Playing some Club Penguin...",
            "Getting banned on Club Penguin..."
        };

        /// <summary>
        /// After the log in button is clicked
        /// </summary>
        private async void LogIn(object sender, RoutedEventArgs e)
        {
            //Create an Async popup
            progCntrler = await this.ShowProgressAsync("Logging in...", "Gathering Information");

            //Set the progress bar min and max amounts
            progCntrler.Maximum = 1000;
            progCntrler.Minimum = 0;

            //Get the information in from a UI thread
            string username = txtLogIn.Text.Trim();
            string password = txtPassword.Password.Trim();
            string serverAddress = txtServerLocation.Text.Trim();

            saveUsernameToFile();

            //Start the task for running the checks and progress bar moving
            await Task.Run(async () =>
            {
                //Task parts
                string errorReason = "";
                bool passed = false;
                int progress = 0;
                Random rnd = new Random();

                //Check the input fields for blanks
                passed = getUserInformation(username, password, ref errorReason);
                await IncrementProgressBar(progCntrler, "Gathering User Information...", progress, 100, 1);
                progress = 100;

                //If the input fields: pass output random messages
                if (passed)
                {
                    await IncrementProgressBar(progCntrler, randomLoadingMessages[rnd.Next(randomLoadingMessages.Length)], progress, 200, 1);
                    progress = 200;
                    await IncrementProgressBar(progCntrler, randomLoadingMessages[rnd.Next(randomLoadingMessages.Length)], progress, 300, 1);
                    progress = 300;
                }

                //Ping the server to make sure that it is up and running
                if (passed)
                {
                    await IncrementProgressBar(progCntrler, "Pinging machine server is running on...", progress, 400, 1);
                    progress = 400;

                    //Creates a new server object if it is null
                    if (server == null)
                    {
                        server = new Server(serverAddress, this);
                        //Initalize the events
                        server.LoginOK += new EventHandler(im_LoginOK);
                        //server.RegisterOK += new EventHandler(im_RegisterOK);
                        server.LoginFailed += new IMErrorEventHandler(im_LoginFailed);
                        server.RegisterFailed += new IMErrorEventHandler(im_RegisterFailed);

                        //Add the new message listener
                        receivedHandler = new IMReceivedEventHandler(im_MessageReceived);
                        server.MessageReceived += receivedHandler;
                    }
                    passed = pingServer(serverAddress, ref errorReason);

                    if (passed)
                    {
                        await IncrementProgressBar(progCntrler, "Machine is online...", progress, 500, 1);
                        progress = 500;
                    }
                }

                //If the ping passed, output random messages
                if (passed)
                {
                    await IncrementProgressBar(progCntrler, randomLoadingMessages[rnd.Next(randomLoadingMessages.Length)], progress, 600, 1);
                    progress = 600;
                    await IncrementProgressBar(progCntrler, randomLoadingMessages[rnd.Next(randomLoadingMessages.Length)], progress, 700, 1);
                    progress = 700;
                }

                if (passed)
                {
                    //Check if the server is already connected to the server
                    if (!server.Connected)
                    {
                        await IncrementProgressBar(progCntrler, "Connecting to server...", progress, 750, 1);
                        progress = 750;
                        passed = ConnectToServer(ref errorReason);
                        if (passed)
                        {
                            await IncrementProgressBar(progCntrler, "Successfully connected to server...", progress, 800, 1);
                            progress = 800;
                        }
                    }
                    else
                    {
                        //If they tried logging in already
                        //**NOTE** the reason for this check is becuase after the first log in try the server doesn't allow
                        //         other attempts and a new connection is required **NOTE**
                        if (server.TriedLoggingIn)
                        {
                            server.CloseConnectionWithServer();
                            await IncrementProgressBar(progCntrler, "Connecting to server...", progress, 750, 1);
                            progress = 750;
                            passed = ConnectToServer(ref errorReason);
                            if (passed)
                            {
                                await IncrementProgressBar(progCntrler, "Successfully connected to server...", progress, 800, 1);
                                progress = 800;
                            }
                        }
                        else
                        {
                            await IncrementProgressBar(progCntrler, "Already connected to server...", progress, 800, 1);
                            progress = 800;
                        }
                    }
                }

                //Check the credentials
                if (passed)
                {
                    //Log in to the server
                    server.LogInWithCredentials(username, password);
                }

                //Error Message
                if (!passed)
                {
                    progCntrler.SetTitle("Error!");
                    progCntrler.SetMessage(errorReason);
                    CloseFlyover(progCntrler, 3000);
                }
            });
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        /// <param name="error">The reference with the error</param>
        /// <returns>true if passed, false otherwise</returns>
        private bool ConnectToServer(ref string error)
        {
            //Connect to the server
            bool passed = server.CreateConnectionWithServer();
            if (passed)
            {
                return true;
            }
            else
            {
                //Error connecting
                if (error != "")
                    error += "\n";
                error += "Failed to connect to server on machine.";
                return false;
            }
        }

        /// <summary>
        /// Called after the flyover is finished
        /// </summary>
        /// <param name="message">The flyover</param>
        /// <param name="delay">the delay before it gets closed</param>
        private async void CloseFlyover(ProgressDialogController message, int delay, params bool[] sParams)
        {
            await Task.Delay(delay);
            if (sParams.Length > 0 && sParams[0] == true)
                RouteToChatScreen();
            await message.CloseAsync().ConfigureAwait(false); 
        }

        /// <summary>
        /// Called for checking the user information for blanks
        /// </summary>
        /// <param name="username">The username</param>
        /// <param name="password">The password</param>
        /// <param name="error">Reference to the error string</param>
        /// <returns>true if passed, false otherwise</returns>
        private bool getUserInformation(string username, string password, ref string error)
        {
            bool passed = true;
            if (username == "")
            {
                passed = false;
                error += "Invalid Username entered";
            }
            if (password == "")
            {
                passed = false;
                if (error != "")
                    error += "\n";
                error += "Invalid Password entered";
            }
            return passed;
        }

        /// <summary>
        /// Called to ping the machine to make sure that it is a valid address
        /// **NOTE** this does not guarantee that a server is running no the machine, only that the machine is online
        /// </summary>
        /// <param name="serverAddress">The address of the machine</param>
        /// <param name="error">Reference to the error string</param>
        /// <returns>true if machine repsonded, false otherwise</returns>
        private bool pingServer(string serverAddress, ref string error)
        {
            //Calls the server to ping
            bool passed = server.tryConnectionToServer(serverAddress);
            if (passed)
            {
                //Create the events for the server
                return true;
            }
            else
            {
                if (error != "")
                    error += "\n";
                error += "No response received from the target machine [" + serverAddress + "]";
                return false;
            }
        }

        /// <summary>
        /// Increments the progressbar on the flyover
        /// </summary>
        /// <param name="message">The flyover</param>
        /// <param name="msgTxt">The message to be updated</param>
        /// <param name="progress">The progress currently</param>
        /// <param name="gotoAmount">The amount for the progress to go to</param>
        /// <param name="delay">The delay between each increment</param>
        /// <returns>Used for the 'await' call</returns>
        private async Task IncrementProgressBar(ProgressDialogController message, string msgTxt, int progress, int gotoAmount, int delay)
        {
            message.SetMessage(msgTxt);
            for (int x = progress; x < gotoAmount; x++)
            {
                progress += 1;
                message.SetProgress(progress);
                await Task.Delay(delay);
            }
        }

        #endregion

        #region Navigation
        /// <summary>
        /// Navigates to the Existing user field
        /// </summary>
        private void RouteToExistingUser(object sender, RoutedEventArgs e)
        {
            //Hide Register window
            gridRegister.Visibility = Visibility.Hidden;
            //Show Login Window
            gridLogIn.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Navigates to the New user field
        /// </summary>
        private void RouteToNewUser(object sender, RoutedEventArgs e)
        {
            //Hide Login window
            gridLogIn.Visibility = Visibility.Hidden;
            //Show Register Window
            gridRegister.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Navigates to the Chat screen
        /// </summary>
        private void RouteToChatScreen()
        {
            //Make both Register and Login disappear
            gridLogIn.Visibility = Visibility.Hidden;
            gridRegister.Visibility = Visibility.Hidden;

            //Make the chat screen visible
            gridChat.Visibility = Visibility.Visible;

            //Toggle in information in the top bar
            toggleSettings();
        }
        #endregion

        #region Settings
        /// <summary>
        /// Hides the server address selection and shows the settings button
        /// </summary>
        public void toggleSettings()
        {
            settingsButton.Visibility = Visibility.Visible;
            ServerLocationInfo.Visibility = Visibility.Hidden;
            ServerLocationInfo.Width = 0;
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            RightWindowCommandsOverlayBehavior = MahApps.Metro.Controls.WindowCommandsOverlayBehavior.Never;
            SettingsFlyout.IsOpen = true;
        }
        #endregion

        #region Close Window
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (server!= null && server.Connected)
                server.CloseConnectionWithServer();
        }
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            if (server.Connected)
                server.CloseConnectionWithServer();
            this.Close();
        }
        #endregion

        #region List Of Users
        private void ListOfUsersClicked(object sender, MouseButtonEventArgs e)
        {
            var item = sender as System.Windows.Controls.ListViewItem;
            if (item != null && item.IsSelected)
            {
                txtMessage.AppendText(((Users)item.Content).Name);
            }
        }
        #endregion

        #region Hyperlink

        public void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        #endregion

        #region Conversions
        public string ToTitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
        }
        #endregion

        #region Get Parts of a Object
        public static DependencyObject GetScrollViewer(DependencyObject o)
        {
            // Return the DependencyObject if it is a ScrollViewer
            if (o is System.Windows.Controls.ScrollViewer)
            { return o; }

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(o); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(o, i);

                var result = GetScrollViewer(child);
                if (result == null)
                {
                    continue;
                }
                else
                {
                    return result;
                }
            }
            return null;
        }

        #endregion

    }
}
