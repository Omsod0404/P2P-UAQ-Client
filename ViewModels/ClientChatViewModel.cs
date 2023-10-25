using P2P_UAQ_Client.Core;
using P2P_UAQ_Client.Core.Events;
using P2P_UAQ_Client.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace P2P_UAQ_Client.ViewModels
{
    public class ClientChatViewModel : BaseViewModel
    {
        private CoreHandler _coreHandler;
        private ObservableCollection<Connection> _connectionsUI = new ObservableCollection<Connection>();
        private List<string> _messages = new List<string>();
        private string _messageLabel;
        private string _message;
        
        // Commands

        public ICommand ExecuteSendMessage { get; }
		public ICommand RequestPrivateRoomCommand { get; }

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

        public ObservableCollection<Connection> ConnectionsUI 
        { 
            get { return _connectionsUI; } 
            set
            {
                _connectionsUI = value;
                OnPropertyChanged(nameof(ConnectionsUI));
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

        public List<string> Messages
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
			RequestPrivateRoomCommand = new ViewModelCommand(RequestPrivateRoom);
			_coreHandler.MessageReceivedEvent += _coreHandler_MessageReceivedEvent;
			_coreHandler.ConnectionAddedEvent += _coreHandler_ConnectionAddedEvent;
			_coreHandler.ConnectionRemovedEvent += _coreHandler_ConnectionRemovedEvent;
            OnPropertyChanged(nameof(AllMessages));
        }

		private void _coreHandler_ConnectionRemovedEvent(object? sender, ConnectionRemovedEventArgs e)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				ConnectionsUI.Remove(n => n.IpAddress == e.Connection.IpAddress && n.Port == e.Connection.Port && n.Nickname == e.Connection.Nickname);
			});
		}

		private void _coreHandler_ConnectionAddedEvent(object? sender, ConnectionAddedEventArgs e)
		{
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
				ConnectionsUI.Add(e.Connection);
			}));
		}

		private void _coreHandler_MessageReceivedEvent(object? sender, MessageReceivedEventArgs e)
		{
			Messages.Add(e.Message);
            OnPropertyChanged(nameof(AllMessages));
		}

		public string AllMessages
		{
			get { return string.Join(Environment.NewLine, Messages); }
            set 
            { 

            }
		}

		public void SendMessageCommand(object sender)
        {

        }

		private void RequestPrivateRoom(object sender)
		{
			var model = sender as Connection;
            _coreHandler.ConnectToRemoteClientAsync(model!);
		}
	}
}