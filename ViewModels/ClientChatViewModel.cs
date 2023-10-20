﻿using P2P_UAQ_Client.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace P2P_UAQ_Client.ViewModels
{
    public class ClientChatViewModel : BaseViewModel
    {
        private CoreHandler _coreHandler;
        private ObservableCollection<StackPanel> _connectionsUI = new ObservableCollection<StackPanel>();
        private ObservableCollection<string> _messages = new ObservableCollection<string>();
        private string _messageLabel;
        private string _message;
        
        public ICommand ExecuteSendMessage { get; }
        public string Message
        {
            get { return _message; }
            set 
            {
				_message = value;
				if (!string.IsNullOrEmpty(_message))
                {
					MessageLabel = "";
                    OnPropertyChanged(nameof(Message));
				}
                else
                {
					MessageLabel = "Escribe un mensaje";
				}
            }        
        }

        public string MessageLabel 
        {
            get { return _messageLabel; } 
            set
            {
                _messageLabel = value;
                OnPropertyChanged(nameof(MessageLabel));
            }
        }

        public ObservableCollection<string> Messages
        {
            get { return this._messages; }
            set
            {
                this._messages = value;
            }
        }

        public ClientChatViewModel()
        {
            _coreHandler = CoreHandler.Instance;
            _message = "";
            _messageLabel = "Escribe un mensaje";
            ExecuteSendMessage = new ViewModelCommand(SendMessageCommand);
			_coreHandler.MessageReceivedEvent += _coreHandler_MessageReceivedEvent;
        }

		private void _coreHandler_MessageReceivedEvent(object? sender, Core.Events.MessageReceivedEventArgs e)
		{
			Messages.Add(e.Message);
            OnPropertyChanged(nameof(AllServerStatusMessages));
		}

		public string AllServerStatusMessages
		{
			get { return string.Join(Environment.NewLine, Messages); }
		}

		public void SendMessageCommand(object sender)
        {

        }
    }
}
