using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_UAQ_Client.Core.Events
{
	public class UsernameIsAvailableEventArgs : EventArgs
	{
        public bool UsernameIsAvailable { get; private set; }
        public UsernameIsAvailableEventArgs(bool value)
        {
            UsernameIsAvailable = value;
        }
    }
}
