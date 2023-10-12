using P2P_UAQ_Client.Core;
using P2P_UAQ_Client.Core.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace P2P_UAQ_Client.ViewModels
{
	public class PrivateChatViewModel : BaseViewModel
	{
		private ObservableCollection<string> _messages = new ObservableCollection<string>();

        private CoreHandler _coreHandler = CoreHandler.Instance;

		public ObservableCollection<string> Messages
		{
			get { return _messages; }
			set
			{
				_messages = value;
				OnPropertyChanged(nameof(Messages));
			}
		}


		public PrivateChatViewModel()
        {
			_coreHandler.PrivateMessageReceived += _coreHandler_PrivateMessageReceived;
        }

		private void _coreHandler_PrivateMessageReceived(object? sender, PrivateMessageReceivedEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				Messages.Add(e.Message);
			}));	
		}
	}
}
