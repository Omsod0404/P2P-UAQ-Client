using Microsoft.Win32;
using P2P_UAQ_Client.Core;
using P2P_UAQ_Client.Core.Events;
using P2P_UAQ_Client.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace P2P_UAQ_Client.ViewModels
{
	public class PrivateChatViewModel : BaseViewModel
	{
		private ObservableCollection<string> _messages = new ObservableCollection<string>();
		private CoreHandler _coreHandler = CoreHandler.Instance;
		private Window? _window = null;

		private string _username = "";
		private string _message = "";
		private string _messageLabel;
		private string _windowTitle = "Chat privado con";

		public bool RequestedClosed { get; set; }

		public ICommand SendMessageCommand { get; set; }
		public ICommand FileCommand { get; set; }

		public Connection? Connection { get; set; }

		public ObservableCollection<string> Messages
		{
			get { return _messages; }
			set
			{
				_messages = value;
				OnPropertyChanged(nameof(Messages));
			}
		}

		public string AllMessages
		{
			get { return string.Join(Environment.NewLine, Messages); }
			set
			{

			}
		}

		public string WindowTitle
		{
			get { return _windowTitle; }
			set
			{
				_windowTitle = value;
				OnPropertyChanged(nameof(WindowTitle));
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

		public string Message
		{
			get { return _message; }
			set
			{
				_message = value;
				OnPropertyChanged(nameof(Message));
			}
		}

		public string Username
		{
			get { return _username; }
			set
			{
				_username = value;
			}
		}

		public PrivateChatViewModel(Connection connection)
        {
			SendMessageCommand = new ViewModelCommand(SendMessage);
			FileCommand = new ViewModelCommand(SendFile);
			Connection = connection;
			WindowTitle = $"Chat privado con {Connection.Nickname}";
			_messageLabel = "Escribe un mensaje";
			RequestedClosed = false;
		}

		public void AddMessage(string message)
		{
			Application.Current.Dispatcher.Invoke(new Action(() =>
			{
				Messages.Add(message);
				OnPropertyChanged(nameof(AllMessages));
			}));
		}

		public void CloseWindow()
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				_window!.Close();
			});
		}

		public void SetWindowReference(Window window)
		{
			_window = window;
		}

		private void SendMessage(object sender)
		{
			if (!string.IsNullOrEmpty(Message))
			{
				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					Messages.Add($"{CoreHandler.Instance.LocalConnection!.Nickname}: {Message}");
					OnPropertyChanged(nameof(AllMessages));
				}));

				_coreHandler.SendMessageToRemoteClient(Connection!, $"{CoreHandler.Instance.LocalConnection!.Nickname}: {Message}");
				Message = "";
			}
		}

		private void SendFile(object sender)
		{
			// Para mandar el arhcivo
			var fileExplorer = new OpenFileDialog()
			{
				Title = "Seleccionar Archivo",
				Filter = "Todos los Archivos (*.*)|*.*",
            };
            

            if (fileExplorer.ShowDialog() == true)
            {
                string path = fileExplorer.FileName;

                if (!string.IsNullOrEmpty(path))
                {
                    long maxSize = 25 * 1024 * 1024;

                    byte[] file = File.ReadAllBytes(path);

                    long fileSize = file.Length;

                    if (fileSize <= maxSize)
                    {
                        _coreHandler.SendFileToChat(Connection!, file);
                    }
                    else
                    {
                        MessageBox.Show("El archivo es mayor a 25MB");
                    }
                }
            }
        }

		public void RequestCloseRoom()
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				_coreHandler.RequestToCloseChat(Connection!);
			});
		}
	}
}
