using P2P_UAQ_Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_UAQ_Client.Core.Events
{
	public class ConnectionAddedEventArgs : EventArgs
	{
        public Connection Connection { get; set; }
        public ConnectionAddedEventArgs(Connection connection)
        {
            Connection = connection;
        }
    }
}
