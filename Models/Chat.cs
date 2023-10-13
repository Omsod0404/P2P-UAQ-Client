using P2P_UAQ_Client.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_UAQ_Client.Models
{
	public class Chat
	{
		public Connection? RequesterConnection { get; set; }
		public Connection? ReceiverConnection { get; set; }
		public PrivateChatViewModel? PrivateChatViewModel { get; set; }
	}
}
