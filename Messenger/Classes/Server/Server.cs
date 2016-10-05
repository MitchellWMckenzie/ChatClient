using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Messenger.Classes.Server;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Windows;
using System.Collections.ObjectModel;
using System.Timers;

namespace Messenger.Classes.Server
{
    class Server
    {

        #region Attributes

        Thread tcpThread;     // Receiver
        bool _conn = false;   //Is connected/connecting?
        bool _trieLoggingIn = false; //Keep track of when they try to log in
        bool _logged = false; //Is logged in?
        bool _restarting = false;  //Is the server restarting?
        string _username;     //Username
        string _password;     //Password
        bool banned = false;  //Is currently banned
        bool _reg;            //Register Mode

        string serverAddress;   //The address of the server
        public int Port = 8081; //The port the server will run on

        TcpClient client;
        NetworkStream netStream;
        SslStream ssl;
        BinaryReader br;
        BinaryWriter bw;

        public List<string> users;
        public List<string> userColors;
        public MainWindow window;

        //Bools for the checkboxes
        public bool CanReceivePics = true;
        public bool PasteToClipboard = false;

        public bool Connected
        {
            get { return _conn; }
        }
        public bool TriedLoggingIn
        {
            get { return _trieLoggingIn; }
        }


        //Timers
        System.Timers.Timer restartTimer;
        #endregion


        /// <summary>
        /// Constructor for the Server
        /// </summary>
        /// <param name="serverAddress">The address of the server</param>
        public Server(string serverAddress, MainWindow window)
        {
            this.serverAddress = serverAddress;
            this.window = window;
            SnippingTool.AreaSelected += OnAreaSelected;
            setUpTimers();
        }

        #region Snipping Tool

        private void OnAreaSelected(object sender, EventArgs e)
        {
            var bmp = SnippingTool.Image;

            Bitmap temp = new Bitmap(bmp);

            ImagetoBase64(temp);
        }

        #endregion

        #region Set Up Connection

        public bool tryConnectionToServer(string serverAddress)
        {
            this.serverAddress = serverAddress;
            bool pingable = false;
            Ping pinger = new Ping();
            try
            {
                PingReply reply = pinger.Send(serverAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (Exception e)
            {
                // Discard PingExceptions and return false;
                Console.WriteLine(e);
            }
            return pingable;
        }

        public static bool ValidateCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; // Allow untrusted certificates.
        }


        public bool CreateConnectionWithServer()
        {
            try
            {
                // Connect to the server.
                client = new TcpClient(serverAddress, Port);

                //Create an authenticated stream
                netStream = client.GetStream();
                ssl = new SslStream(netStream, false, new RemoteCertificateValidationCallback(ValidateCert));
                ssl.AuthenticateAsClient("InstantMessengerServer");
                // Now we have encrypted connection.

                //Create the reader and writer from the server
                br = new BinaryReader(ssl, Encoding.UTF8);
                bw = new BinaryWriter(ssl, Encoding.UTF8);

                // Receive "hello"
                int hello = br.ReadInt32();
                if (hello == Protocols.IM_Hello)
                {
                    //Now the server is ready and waiting for our next communications
                    _conn = true;
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }

        public void LogInWithCredentials(string username, string password)
        {
            try
            {
                connect(username, password, false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void CloseConnectionWithServer()
        {
            br.Close();
            bw.Close();
            ssl.Close();
            netStream.Close();
            client.Close();
            _trieLoggingIn = false;
            _logged = false;
            _conn = false;
        }

        public void Login(string user, string password)
        {
            connect(user, password, false);
        }
        public void Register(string user, string password)
        {
            connect(user, password, true);
        }

        void connect(string user, string password, bool register)
        {
            if (!_trieLoggingIn)
            {
                _conn = true;
                _username = user;
                _password = password;
                _trieLoggingIn = true;
                _reg = register;
                users = new List<String>();
                userColors = new List<string>();
                tcpThread = new Thread(new ThreadStart(SetupConn));
                tcpThread.Start();
            }
        }

        void SetupConn()  // Setup connection and login
        {
            // Hello OK, so answer.
            bw.Write(Protocols.IM_Hello);

            bw.Write(_reg ? Protocols.IM_Register : Protocols.IM_Login);  // Login or register
            bw.Write(_username);
            bw.Write(_password);
            bw.Flush();

            byte ans = br.ReadByte();  // Read answer.
            if (ans == Protocols.IM_OK)  // Login/register OK
            {

                string isMod = br.ReadString();
                string nameColor = br.ReadString();
                string fontType = br.ReadString();
                string fontsize = br.ReadString();
                string messagecolor = br.ReadString();

                byte amountOfEmojiis = br.ReadByte();
                while (amountOfEmojiis > 0)
                {
                    string test = br.ReadString();
                    if (test != "")
                    {
                        string key = test;
                        string imageInB64 = br.ReadString();
                        //window.smilies.Add(key, generateBMI(Base64ToImage(imageInB64)));
                        if (!_restarting)
                            window.addSmilie(key, generateBMI(Base64ToImage(imageInB64)));
                        string replace = br.ReadString();
                        string height = br.ReadString();
                        string width = br.ReadString();
                        if (!_restarting)
                            window.sParams.Add(key, new Dictionary<string, string>()
                                {
                                    { "Replacement", replace},
                                    { "Height", height },
                                    { "Width", width }
                                });
                        amountOfEmojiis--;
                    }
                }

                bool result;
                bool.TryParse(isMod, out result);
                window.updateSettingsInformation(result, nameColor, fontType, fontsize, messagecolor);

                if (_reg)
                    OnRegisterOK();  // Register is OK.
                if (!_restarting)
                    OnLoginOK();  // Login is OK (when registered, automatically logged in)
                window.stopRestarting();
                Receiver(); // Time for listening for incoming messages.
            }
            else
            {
                IMErrorEventArgs err = new IMErrorEventArgs((IMError)ans);
                if (_reg)
                    OnRegisterFailed(err);
                else
                    OnLoginFailed(err);
            }
            if (_conn)
                CloseConnectionWithServer();
        }

        #endregion

        #region Settings
        public void updateStatus(bool check)
        {
            if (_conn && !banned)
            {
                bw.Write(Protocols.UP_PicStatus);
                if (check)
                {
                    CanReceivePics = true;
                    bw.Write("Green");
                }
                else
                {
                    CanReceivePics = false;
                    bw.Write("Red");
                }
                bw.Flush();
            }
        }

        /// <summary>
        /// 1 - Name Color
        /// 2 - Font
        /// 3 - Font Size
        /// 4 - Message Color
        /// </summary>
        /// <param name="setting"></param>
        public void UpdateSetting(int setting, string settingInfo)
        {
            switch (setting)
            {
                case 1:
                    bw.Write(Protocols.UP_Name_Color);
                    bw.Write(settingInfo);
                    bw.Flush();
                    break;
                case 2:
                    bw.Write(Protocols.UP_Font);
                    bw.Write(settingInfo);
                    bw.Flush();
                    break;
                case 3:
                    bw.Write(Protocols.UP_Font_Size);
                    bw.Write(settingInfo);
                    bw.Flush();
                    break;
                case 4:
                    bw.Write(Protocols.UP_Message_Color);
                    bw.Write(settingInfo);
                    bw.Flush();
                    break;
            }
        }
        #endregion

        #region Events

        public event EventHandler LoginOK;
        public event EventHandler RegisterOK;
        public event IMErrorEventHandler LoginFailed;
        public event IMErrorEventHandler RegisterFailed;
        public event EventHandler Disconnected;
        public event IMAvailEventHandler UserAvailable;
        public event IMReceivedEventHandler MessageReceived;

        virtual protected void OnLoginOK()
        {
            _logged = true;
            if (LoginOK != null)
                LoginOK(this, EventArgs.Empty);
        }
        virtual protected void OnRegisterOK()
        {
            if (RegisterOK != null)
                RegisterOK(this, EventArgs.Empty);
        }
        virtual protected void OnLoginFailed(IMErrorEventArgs e)
        {
            if (LoginFailed != null)
                LoginFailed(this, e);
        }
        virtual protected void OnRegisterFailed(IMErrorEventArgs e)
        {
            if (RegisterFailed != null)
                RegisterFailed(this, e);
        }
        virtual protected void OnDisconnected()
        {
            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);
        }
        virtual protected void OnUserAvail(IMAvailEventArgs e)
        {
            if (UserAvailable != null)
                UserAvailable(this, e);
        }
        virtual protected void OnMessageReceived(IMReceivedEventArgs e)
        {
            if (MessageReceived != null)
                MessageReceived(this, e);
        }

        #endregion

        #region Send Message

        public void SendMessage(string to, string msg, string color)
        {
            if (_conn)
            {
                bw.Write(Protocols.IM_Send);
                bw.Write(to);
                bw.Write(msg);
                bw.Write(color);
                bw.Flush();
            }
        }

        public void updateMessaging(bool check)
        {
            if (_conn)
            {
                bw.Write(Protocols.UP_Messaging);
                if (check)
                {
                    bw.Write("true");
                }
                else
                {
                    bw.Write("false");
                }
                bw.Flush();
            }
        }

        #endregion

        #region Receiver
        [STAThread]
        void Receiver()  // Receive all incoming packets.
        {
            _logged = true;

            try
            {

                bw.Write(Protocols.IM_GetAllUsers);
                bw.Flush();

                while (client.Connected)  // While we are connected.
                {
                    byte type = br.ReadByte();  // Get incoming packet type.

                    switch (type)
                    {
                        /***************************************************************
                         * Purpose: Received when new user is available 
                         * In use : NO
                         * Attribs:
                         *        [string] User  - The username of the user
                         *        [bool] isAvail - If the user is available or not  
                         **************************************************************/
                        case Protocols.IM_IsAvailable:
                        {
                            string user = br.ReadString();
                            bool isAvail = br.ReadBoolean();
                            OnUserAvail(new IMAvailEventArgs(user, isAvail));
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a user connects to the server
                         * In use : YES
                         * Attribs:
                         *        [string] User  - The username of the user
                         *        [string] statusColor - Current status color of user
                         **************************************************************/
                        case Protocols.IM_NewUser:
                        {
                            string user = br.ReadString();
                            string statusColor = br.ReadString();
                            users.Add(user);
                            userColors.Add(statusColor);
                            window.addUserToList(new Users(user, statusColor));
                            window.refreshList();
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a user disconnects from the server
                         * In use : YES
                         * Attribs:
                         *        [string] User  - The username of the user
                         **************************************************************/
                        case Protocols.IM_RemoveUser:
                        {
                            string user = br.ReadString();
                            users.Remove(user);
                            window.removeUserFromList(user);
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received during first connection with the server to
                         *          get the current users connected to the server
                         * In use : YES
                         * Attribs:
                         *        [string] User  - A string of usernames separated by +
                         *        [string] statusColors  - the status of the usernames
                         *                                 respectively separated by +
                         **************************************************************/
                        case Protocols.IM_GetAllUsers:
                        {
                            string user = br.ReadString();
                            string[] split = user.Split('+');
                            string statusColors = br.ReadString();
                            string[] splitColors = statusColors.Split('+');
                            for (int x = 0; x < split.Length; x++)
                            {
                                if (!split[x].Equals(_username) && !users.Contains(split[x]))
                                {
                                    users.Add(split[x]);
                                    userColors.Add(splitColors[x]);
                                    window.addUserToList(new Users(split[x], splitColors[x]));
                                    window.refreshList();
                                }
                            }
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a picture is sent to the server. It is
                         *          received in a base64 string format, the program 
                         *          converts the image and displays it.
                         * In use : YES
                         * Attribs:
                         *        [string] personWhoSent - Username of the user
                         *        [string] image  - A base64 representation of the image
                         *                          that was sent.
                         **************************************************************/
                        case Protocols.IM_PICTURE:
                        {
                            string personWhoSent = br.ReadString();
                            string image = br.ReadString();

                            //If the settings to receive pictures is checked
                            if (CanReceivePics)
                            {
                                Image img = Base64ToImage(image);
                                Bitmap temp = new Bitmap(img);
                                BitmapSource bitmap = ConvertBitmap(temp);

                                //If copy to clipboard is checked
                                if (window.chkClipboard.IsChecked == true)
                                {
                                    Application.Current.Dispatcher.Invoke(new Action(() =>
                                    {
                                        System.Windows.Clipboard.SetImage(bitmap);
                                    }));
                                }
                                Application.Current.Dispatcher.Invoke((Action)delegate {
                                    PictureScreen.PictureScreen ps = new PictureScreen.PictureScreen();
                                    ps.setPicture(image, img.Width, img.Height + 35, personWhoSent + "'s picture sent [" + DateTime.Now + "]", window);
                                    ps.Show();
                                });
                            }
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a user changes the status of their
                         *          client
                         * In use : YES
                         * Attribs:
                         *        [string] User  - The username of the user
                         *        [string] Color - The status of the client
                         **************************************************************/
                        case Protocols.UP_PicStatus:
                        {
                            string user = br.ReadString();
                            var tooUpdate = window.listofUsers.Single(x => x.Name.Equals(user));
                            tooUpdate.Color = br.ReadString();
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a new message is sent from a user or
                         *          the server. All messages are sent with this protocol
                         * In use : YES
                         * Attribs:
                         *        [int] id           - The id of the message
                         *        [string] from      - Username of the sender
                         *        [string] msg       - The contents of the message
                         *        [string] color     - The color of the users name
                         *        [string] font      - Font of the message
                         *        [string] size      - Size of the message text
                         *        [string] textcolor - Color of the message text
                         *        [bool] canClear    - If the user has permission to 
                         *                             clear the contents of the message
                         **************************************************************/
                        case Protocols.IM_Received:
                        {
                            int id = int.Parse(br.ReadString());
                            string from = br.ReadString();
                            string msg = br.ReadString();
                            string color = br.ReadString();
                            string font = br.ReadString();
                            string size = br.ReadString();
                            string textColor = br.ReadString();
                            bool canClear = bool.Parse(br.ReadString());
                            OnMessageReceived(new IMReceivedEventArgs(id, from, msg, color, font, size, textColor, canClear));
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when the user gets banned from the server
                         * In use : YES
                         * Attribs:
                         *        None. If the user receives this message, it means that
                         *        they were banned and the connection with the server is
                         *        closed.
                         **************************************************************/
                        case Protocols.UP_Banned:
                        {
                            banned = true;
                            OnMessageReceived(new IMReceivedEventArgs(-1, "SYSTEM", "You have been banned.", "White", "Segoe UI", "16", "White", false));
                            CloseConnectionWithServer();
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a Mod+ deletes a message
                         * In use : YES
                         * Attribs:
                         *        [int] messID - The id of the message to be removed
                         **************************************************************/
                        case Protocols.UP_Remove_Message:
                        {
                            int messID = int.Parse(br.ReadString());
                            Message msg = null;
                            for (int i = 0; i < window.txtMessages.Count; i++)
                            {
                                if (((Message)window.txtMessages.ElementAt(i)).messageID == messID)
                                {
                                    msg = window.txtMessages.ElementAt(i);
                                }
                            }
                            msg.clrBtnWidth = "0";
                            msg.clrVis = Visibility.Hidden;
                            msg.textInformation = new ObservableCollection<TextInfo>();
                            msg.textInformation.Add(new TextInfo(null, "", "", Visibility.Hidden, "<Message Removed>",
                                msg.args.TextColor, msg.args.Font, msg.args.Size, Visibility.Visible));
                            window.updateMessages();
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a user starts typing
                         * In use : YES
                         * Attribs:
                         *        [string] name - The username of the user
                         **************************************************************/
                        case Protocols.UP_Messaging:
                        {
                            string name = br.ReadString();
                            var tooUpdate = window.listofUsers.Single(x => x.Name.Equals(name));

                            if (br.ReadString().Equals("true"))
                            {
                                tooUpdate.Width = 16;
                            }
                            else
                            {
                                tooUpdate.Width = 0;
                            }
                            break;
                        }

                        /***************************************************************
                         * Purpose: Received when a restart command is sent to the 
                         *          server.
                         * In use : YES
                         * Attribs:
                         *        None. Once the command is received the program
                         *        restarts
                         **************************************************************/
                        case Protocols.IM_Reconnecting:
                        {
                            OnMessageReceived(new IMReceivedEventArgs(-1, "SYSTEM", "Server restarting. Disconnecting...", 
                                "White", "Segoe UI", "16", "White", false));
                            window.clearUsers();
                            _restarting = true;
                            
                            //Start the timer
                            restartTimer.Enabled = true;

                            window.startRestarting();
                            
                            CloseConnectionWithServer();
                            break;
                        }
                    }
                }
            }
            catch (IOException) { }

            _logged = false;
        }
        #endregion

        #region Timer Functions

        /// <summary>
        /// Sets up the timers with the settings required
        /// </summary>
        private void setUpTimers()
        {
            //Set up the timer
            restartTimer = new System.Timers.Timer();
            restartTimer.Elapsed += new ElapsedEventHandler(restartTimerTicks);
            restartTimer.Interval = 8000;
        }

        /// <summary>
        /// Ticks when the server is restarting so the client can reconnet.
        /// </summary>
        private void restartTimerTicks(object source, ElapsedEventArgs e)
        {
            OnMessageReceived(new IMReceivedEventArgs(-1, "SYSTEM", "Trying to reconnect to server...",
                                "White", "Segoe UI", "16", "White", false));
            if (!_conn)
            {
                CreateConnectionWithServer();
            }
            if (_conn)
            {
                connect(_username, _password, false);
                window.clearUsers();
                window.refreshList();
                restartTimer.Enabled = false;
            }
        }

        #endregion

        #region Image Conversion Methods

        /// <summary>
        /// Generates the BitmpaImage with a given image
        /// </summary>
        /// <param name="image">The image to convert</param>
        /// <returns>The BitmapIamge</returns>
        public static BitmapImage generateBMI(Image image)
        {
            Bitmap img = (Bitmap)image;
            BitmapImage bmImg = new BitmapImage();

            bool test = System.Drawing.Imaging.ImageFormat.Gif.Equals(image.RawFormat);

            using (MemoryStream memStream2 = new MemoryStream())
            {
                if (test)
                    img.Save(memStream2, System.Drawing.Imaging.ImageFormat.Gif);
                else
                    img.Save(memStream2, System.Drawing.Imaging.ImageFormat.Png);
                memStream2.Position = 0;

                bmImg.BeginInit();
                bmImg.CacheOption = BitmapCacheOption.OnLoad;
                bmImg.UriSource = null;
                bmImg.StreamSource = memStream2;
                bmImg.EndInit();
            }
            return bmImg;
        }

        public void ImagetoBase64(Image image)
        {
            using (MemoryStream m = new MemoryStream())
            {
                image.Save(m, System.Drawing.Imaging.ImageFormat.Bmp);
                byte[] imageBytes = m.ToArray();
                // Convert byte[] to Base64 String
                string base64String = Convert.ToBase64String(imageBytes);
                bw.Write(Protocols.IM_PICTURE);  // Login or register
                bw.Write(base64String);
                bw.Flush();
            }
        }

        public static Image Base64ToImage(string base64String)
        {
            // Convert Base64 String to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            MemoryStream ms = new MemoryStream(imageBytes, 0,
              imageBytes.Length);

            // Convert byte[] to Image
            ms.Write(imageBytes, 0, imageBytes.Length);
            Image image = Image.FromStream(ms, true);
            return image;
        }

        public static BitmapSource ConvertBitmap(Bitmap source)
        {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

        #endregion
    }
}
