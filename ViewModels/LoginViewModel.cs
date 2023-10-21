using P2P_UAQ_Client.Core;
using P2P_UAQ_Client.Core.Events;
using P2P_UAQ_Client.View;
using P2P_UAQ_Client.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace P2P_UAQ_Client.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
		private CoreHandler _coreHandler;

        private ICommand _executeLoginCommand;

        private string _username;
        private string _ip;
        private string _port;
        private string _usernameLabel;
		private string _ipLabel;
		private string _portLabel;
		private bool _isConnected;
		private bool _isUsernameAvailable;
		private bool _isUsernameWasChecked;

		public ICommand ExecuteLoginCommand
        {
            get { return _executeLoginCommand; }
        }

        public string Username
        {
            get { return _username; }
            set
            {
				_username = value;
				OnPropertyChanged(nameof(Username));

				if (string.IsNullOrEmpty(value))
					UsernameLabel = "Nombre de usuario";
				else
					UsernameLabel = "";
            }
        }

		public string UsernameLabel
		{
			get { return _usernameLabel; }
			set
			{
				_usernameLabel = value;
				OnPropertyChanged(nameof(UsernameLabel));
			}
		}

		public string IPLabel
		{
			get { return _ipLabel; }
			set 
			{ 
				_ipLabel = value;
				OnPropertyChanged(nameof(IPLabel));
			}
		}

		public string IP
		{
			get { return _ip; }
			set
			{
				_ip = FormatIPv4(value);

				if (string.IsNullOrEmpty(_ip))
					IPLabel = "Dirección IP";
				else
					IPLabel = "";

				OnPropertyChanged(nameof(IP));
			}
		}

		public string Port
		{
			get { return _port; }
			set
			{
				if (string.IsNullOrEmpty(value) || ValidatePort(value))
				{
					_port = value;

					if (string.IsNullOrEmpty(_port))
						PortLabel = "Puerto";
					else
						PortLabel = "";

					OnPropertyChanged(nameof(Port));
				}
			}
		}

		public string PortLabel
		{
			get { return _portLabel; }
			set
			{
				_portLabel = value;
				OnPropertyChanged(nameof(PortLabel));
			}
		}

		public bool IsConnected
		{
			get { return _isConnected; }
			set
			{
				_isConnected = value;
				OnPropertyChanged(nameof(IsConnected));
			}
		}
		public bool IsUsernameAvailable
		{
			get { return _isUsernameAvailable; }
			set
			{
				_isUsernameAvailable = value;
				OnPropertyChanged(nameof(IsUsernameAvailable));
			}
		}
		public bool IsUsernameWasChecked
		{
			get { return _isUsernameWasChecked; }
			set
			{
				_isUsernameWasChecked = value;
				OnPropertyChanged(nameof(IsUsernameWasChecked));
			}
		}

		public LoginViewModel()
        {
			CoreHandler.Instance.InitializeLocalServer();
			_coreHandler = CoreHandler.Instance;
			_executeLoginCommand = new ViewModelCommand(LoginAction);
            _username = "";
            _ip = "";
			_port = "";
			_ipLabel = "Dirección IP";
			_portLabel = "Puerto";
			_usernameLabel = "Nombre de usuario";
			_coreHandler.ConnectedStatusEvent += _coreHandler_ConnectedStatusEvent;
			_coreHandler.UsernameAvailableEvent += _coreHandler_UsernameAvailableEvent;
			_coreHandler.UsernameCheckedEvent += _coreHandler_UsernameCheckedEvent;
		}

		private void _coreHandler_UsernameCheckedEvent(object? sender, UsernameCheckedEventArgs e)
		{
			IsUsernameWasChecked = e.UsernameWasChecked;
		}

		private void _coreHandler_UsernameAvailableEvent(object? sender, UsernameIsAvailableEventArgs e)
		{
			IsUsernameAvailable = e.UsernameIsAvailable;

			if (!IsUsernameAvailable && IsUsernameWasChecked)
			{
				var view = new ClientChatView();
				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					Application.Current.MainWindow.Close();
					Application.Current.MainWindow = view;
					view.Show();
				}));
				
			}
		}

		private void _coreHandler_ConnectedStatusEvent(object? sender, ConnectedStatusEventArgs e)
		{
			IsConnected = e.Connected;
		}

		private void LoginAction(object sender)
        {
			if (!string.IsNullOrEmpty(Port) && !string.IsNullOrEmpty(IP) && !string.IsNullOrEmpty(Username))
			{
				CoreHandler.Instance.ConnectoToServerAsync(IP, Port, Username);
			}
		}

		private string FormatIPv4(string input)
		{
			var filteredInput = new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());

			var formattedInput = new StringBuilder();
			int digitCount = 0;
			int periodCount = 0;

			for (int i = 0; i < filteredInput.Length; i++)
			{
				if (filteredInput[i] == '.')
				{
					digitCount = 0;
					periodCount++;

					if (periodCount >= 4) break;
				}
				else if (digitCount > 0 && digitCount % 3 == 0)
				{
					formattedInput.Append('.');
					periodCount++;

					if (periodCount >= 4) break;
				}

				formattedInput.Append(filteredInput[i]);
				if (filteredInput[i] != '.') digitCount++;
			}

			return formattedInput.ToString();
		}

		private bool ValidatePort(string value)
		{
			Regex regex = new Regex(@"^\d{1,5}$");
			return regex.IsMatch(value);
		}
    }
}
