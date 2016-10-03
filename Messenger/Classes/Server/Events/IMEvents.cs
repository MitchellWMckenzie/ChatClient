using Messenger.Classes.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Classes
{
    public enum IMError : byte
    {
        TooUserName = Protocols.IM_TooUsername,
        TooPassword = Protocols.IM_TooPassword,
        Exists = Protocols.IM_Exists,
        NoExists = Protocols.IM_NoExists,
        WrongPassword = Protocols.IM_WrongPass
    }

    public class IMErrorEventArgs : EventArgs
    {
        IMError err;

        public IMErrorEventArgs(IMError error)
        {
            this.err = error;
        }

        public IMError Error
        {
            get { return err; }
        }
    }
    public class IMAvailEventArgs : EventArgs
    {
        string user;
        bool avail;

        public IMAvailEventArgs(string user, bool avail)
        {
            this.user = user;
            this.avail = avail;
        }

        public string UserName
        {
            get { return user; }
        }
        public bool IsAvailable
        {
            get { return avail; }
        }
    }
    public class IMReceivedEventArgs : EventArgs
    {
        int id;
        string user;
        string msg;
        string color;
        string font;
        string size;
        string textColor;
        bool canClear;

        public IMReceivedEventArgs(int id, string user, string msg, string color, string font, string size, string textColor, bool canClear)
        {
            this.id = id;
            this.user = user;
            this.msg = msg;
            this.color = color;
            this.font = font;
            this.size = size;
            this.textColor = textColor;
            this.canClear = canClear;
        }

        public bool CanClear
        {
            get { return canClear; }
        }

        public int ID
        {
            get { return id; }
        }
        public string From
        {
            get { return user; }
        }
        public string Message
        {
            get { return msg; }
        }
        public string Color
        {
            get { return color; }
        }
        public string Font
        {
            get { return font; }
        }
        public string Size
        {
            get { return size; }
        }
        public string TextColor
        {
            get { return textColor; }
        }
    }

    public delegate void IMErrorEventHandler(object sender, IMErrorEventArgs e);
    public delegate void IMAvailEventHandler(object sender, IMAvailEventArgs e);
    public delegate void IMReceivedEventHandler(object sender, IMReceivedEventArgs e);
}
