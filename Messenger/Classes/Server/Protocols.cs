using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Classes.Server
{
    class Protocols
    {
        public const int IM_Hello = 2012;      // Hello
        public const byte IM_OK = 0;           // OK
        public const byte IM_Login = 1;        // Login
        public const byte IM_Register = 2;     // Register
        public const byte IM_TooUsername = 3;  // Too long username
        public const byte IM_TooPassword = 4;  // Too long password
        public const byte IM_Exists = 5;       // Already exists
        public const byte IM_NoExists = 6;     // Doesn't exist
        public const byte IM_WrongPass = 7;    // Wrong password
        public const byte IM_IsAvailable = 8;  // Is user available?
        public const byte IM_Send = 9;         // Send message
        public const byte IM_Received = 10;    // Message received
        public const byte IM_NewUser = 11;
        public const byte IM_RemoveUser = 12;
        public const byte IM_GetAllUsers = 13;
        public const byte IM_PICTURE = 14;
        public const byte IM_Banned = 15;
        public const byte IM_Reconnecting = 16;
        public const byte IM_Get_Info = 17;
        public const byte IM_Get_Emojis = 18;

        public const byte UP_PicStatus = 110;
        public const byte UP_Banned = 111;
        public const byte UP_Name_Color = 112;
        public const byte UP_Font = 113;
        public const byte UP_Font_Size = 114;
        public const byte UP_Message_Color = 115;
        public const byte UP_Settings_Save = 116;
        public const byte UP_Messaging = 117;
        public const byte UP_Remove_Message = 118;
    }
}
