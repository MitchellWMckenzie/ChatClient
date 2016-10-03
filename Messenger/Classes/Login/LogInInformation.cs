using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Classes.Login
{
    [Serializable]
    class LogInInformation
    {
        public string Username{ get; set; }
        public bool SaveInfo { get; set; }
        public string serverName { get; set; }
    }
}
