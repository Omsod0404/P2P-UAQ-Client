using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_UAQ_Client.Core.Events
{
	/// <summary>
	/// This listens for a new message receive that is going to be updated on the UI.
	/// on the ClientChatViewModel.
	/// </summary>
	public class MessageReceivedEventArgs : EventArgs
	{
		public string Message { get; set; }

		public MessageReceivedEventArgs(string value) 
		{
			Message = value;
		}
	}
}
