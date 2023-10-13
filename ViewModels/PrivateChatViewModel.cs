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

		private string _username = "";
		private string _message = "";
		private string _messageLabel;
		private string _windowTitle = "";

		private Window? _window = null;

		private ICommand SendMessageCommand;

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

		public string WindowTitle
		{
			get { return _windowTitle; }
			set
			{
				_windowTitle = $"Chat privado con {Username}";
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

		public PrivateChatViewModel()
        {
			SendMessageCommand = new ViewModelCommand(SendMessage);
			_messageLabel = "Escribe un mensaje";
		}

		public void AddMessage(string message)
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				Messages.Add(message);
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
					Messages.Add(Message);
				}));

				_coreHandler.SendMessageToRemoteClient(Connection!, Message);
				Message = "";
			}
		}

		public void RequestCloseRoom(object sender)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				//_coreClient.RequestCloseRoom(_messageRoom);
			});
		}

		private void LoadFileAndSelect(object sender)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Title = "Open File";
			openFileDialog.Filter = "Todos los archivos (*.*)|*.*";

			if (openFileDialog.ShowDialog() == true)
			{
				string filePath = openFileDialog.FileName;

				if (!string.IsNullOrEmpty(filePath))
				{
					long maxFileSize = 50 * 1024 * 1024;
					byte[] fileData = File.ReadAllBytes(filePath);
					long fileSize = fileData.Length;

					if (fileSize <= maxFileSize)
					{
						//_coreClient.SendFile(fileData, _messageRoom, openFileDialog.SafeFileName);
					}
					else
					{
						MessageBox.Show("El archivo supera los 50 MB");
					}

				}
			}
		}
	}
}
