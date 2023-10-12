using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2P_UAQ_Client.Models
{
	public enum MessageType
	{
		UserConnected = 1,
		UserDisconnected,
		Message,
		ChatRequest,
		ChatCloseRequest,
		ChatMessage,
		File,
	}
}
