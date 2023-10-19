using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace P2P_UAQ_Client.Core.Events
{
	public class UsernameCheckedEventArgs : EventArgs
	{
        public bool UsernameWasChecked;
        public UsernameCheckedEventArgs(bool value)
        {
            UsernameWasChecked = value;
        }
    }
}
