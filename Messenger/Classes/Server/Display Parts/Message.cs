using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Messenger.Classes
{
    public class Message: ItemsControl
    {
        public IMReceivedEventArgs args { get; set; }
        public int messageID { get; private set; }
        public MainWindow window { get; private set; }

        //Name Settings
        public string name { get; private set; }
        public SolidColorBrush nameForeground { get; private set; }

        public Visibility clrVis { get; set; }
        public string clrBtnWidth { get; set; }

        public ObservableCollection<TextInfo> textInformation { get; set; }

        public Message(int messageID, IMReceivedEventArgs args, MainWindow window)
        {
            this.DataContext = this;
            this.args = args;
            this.messageID = messageID;
            this.window = window;
            textInformation = new ObservableCollection<TextInfo>();
            createMessageRun();
        }

        public void createMessageRun()
        {

            if (args.CanClear)
            {
                clrVis = Visibility.Visible;
                clrBtnWidth = "20";
            }
            else
            {
                clrVis = Visibility.Hidden;
                clrBtnWidth = "0";
            }


            name = args.From + ": ";

            if (window.colors.ContainsKey(args.Color))
            {
                if (window.chkAllowColor.IsChecked == true)
                {
                    nameForeground = window.colors[window.ToTitleCase(args.Color)];
                }
                else
                {
                    nameForeground = window.colors["Black"];
                }
            }
            else if (args.Color != "" && args.Color != "-")
            {
                nameForeground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#" + args.Color));
            }
            else
            {
                nameForeground = window.colors["Black"];
            }


            createNewMessages();
            
            if (bool.Parse(window.chkAvailable.IsChecked.ToString()))
                FlshWindow.FlashWindow(window, 3000);
        }


        string messageToBeAdd = "";
        bool messageToFind = false;
        private void createNewMessages()
        {
            string[] messages = getEmojiiSplit(args.Message);


            for (int i = 0; i < messages.Length; i++)
            {
                if (messages[i].Trim() != "")
                {
                    textInformation.Add(checkMessage(messages[i], int.Parse(args.Size)));
                    if (messageToFind)
                    {
                        messageToFind = false;
                        textInformation.Add(checkMessage(messageToBeAdd, int.Parse(args.Size)));
                    }
                }
            }
        }



        System.Windows.Controls.Image emojii;
        private Regex regex =
        new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{2,256}\.[a-z]{2,4}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)",
        RegexOptions.Compiled);
        private TextInfo checkMessage(string message, int size)
        {
            size += 6;
            emojii = new System.Windows.Controls.Image();
            emojii.Width = size;
            emojii.Height = size;
            emojii.Visibility = Visibility.Visible;

            bool found = false;
            for (int x = 0; x < window.sParams.Count; x++)
            {
                string replacement;
                window.sParams.ElementAt(x).Value.TryGetValue("Replacement", out replacement);
                if (replacement == message)
                {
                    string height;
                    window.sParams.ElementAt(x).Value.TryGetValue("Height", out height);
                    string width;
                    window.sParams.ElementAt(x).Value.TryGetValue("Width", out width);

                    found = true;
                    BitmapImage img;
                    window.smilies.TryGetValue(window.sParams.ElementAt(x).Key, out img);
                    emojii.Source = img;

                    if (width == "+6")
                    {
                        emojii.Width += 6;
                    }
                    else if (width == "*2")
                    {
                        emojii.Width *= 2;
                    }
                    else if (width != "def")
                    {
                        emojii.Width = Int32.Parse(width);
                    }

                    if (height == "+6")
                    {
                        emojii.Height += 6;
                    }
                    else if (height == "*2")
                    {
                        emojii.Height *= 2;
                    }
                    else if (height != "def")
                    {
                        emojii.Height = Int32.Parse(height);
                    }

                    //window.txtChat.Inlines.Add(emojii);
                    return new TextInfo(emojii.Source, emojii.Width.ToString(), emojii.Height.ToString(), Visibility.Visible);
                }
            }
            if (!found)
            {
                Match match = regex.Match(message);
                if (match.Success)
                {
                    //Hyperlink link = new Hyperlink(message);
                    //link.NavigateUri = new Uri(match.Value);
                    //link.RequestNavigate += new RequestNavigateEventHandler(window.Hyperlink_RequestNavigate);
                    //window.txtChat.Inlines.Add(link);
                    string nesMsg = message.Replace(match.Value, "~");
                    string[] split = nesMsg.Split('~');
                    textInformation.Add(checkMessage(split[0], int.Parse(args.Size)));
                    if (split.Length > 1 && split[1] != "")
                    {
                        messageToBeAdd = split[1];
                        messageToFind = true;
                    }
                    return new TextInfo(null, "", "", Visibility.Hidden, "", args.TextColor, args.Font, args.Size, Visibility.Visible, match.Value);
                }
                else
                {
                    //window.txtChat.Inlines.Add(message);
                    return new TextInfo(null, "", "", Visibility.Hidden, message, args.TextColor, args.Font, args.Size, Visibility.Visible);
                }
            }
            return null;
        }

        private string[] getEmojiiSplit(string message)
        {
            for (int x = 0; x < window.sParams.Count; x++)
            {
                string replacement;
                window.sParams.ElementAt(x).Value.TryGetValue("Replacement", out replacement);
                message = message.Replace(replacement, "|" + replacement + "|");
            }
            string[] msg = message.Split('|');

            return msg;
        }


    }
}
