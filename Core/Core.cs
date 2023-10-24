using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using P2P_UAQ_Client.Core.Events;
using P2P_UAQ_Client.Models;
using P2P_UAQ_Client.ViewModels;
using P2P_UAQ_Client.Views;

namespace P2P_UAQ_Client.Core
{
	public class CoreHandler
	{
		private readonly static CoreHandler _instance = new(); // Singleton

		private List<Connection> _connections = new(); // Los que estan conectados. 
		private List<Chat> _chats = new List<Chat>(); // Lista para los chats actualmente activos.
		private Connection _localConnection = new(); // Esta variable contiene nuestra info local.
		private Connection _serverConnection = new(); // Nuestra conexion al servidor.
		private Connection _newConnection = new(); // Variable reutilizable para los usuarios conectados.
		private Connection _remoteConnection = new();
		private TcpListener? _server; // Para ser localmente el servidor y aceptar otros clientes P2p
		private TcpClient _client = new TcpClient(); // Para conectarlos al servidor
		private TcpClient _localClient = new TcpClient(); // Para conectarlos al servidor
		private TcpClient _currentRemoteClient = new TcpClient();

		public Connection LocalConnection { get { return _localConnection; } }
		public bool IsConnected { get; private set; }
		public bool UsernameWasChecked { get; private set; }
		public bool UsernameAvailable { get; private set; }

		private bool _clientClosing = false;

		// Eventos para actualizar la interfaz.

		public event EventHandler<PrivateMessageReceivedEventArgs>? PrivateMessageReceived;
		public event EventHandler<ConnectedStatusEventArgs>? ConnectedStatusEvent;
		public event EventHandler<MessageReceivedEventArgs>? MessageReceivedEvent;
		public event EventHandler<UsernameCheckedEventArgs>? UsernameCheckedEvent;
		public event EventHandler<UsernameIsAvailableEventArgs>? UsernameAvailableEvent;
		public event EventHandler<ConnectionAddedEventArgs>? ConnectionAddedEvent;
		public event EventHandler<ConnectionRemovedEventArgs>? ConnectionRemovedEvent;

		// Invokes 
		private void OnPrivateMessageReceived(PrivateMessageReceivedEventArgs e) => PrivateMessageReceived?.Invoke(this, e);
		private void OnUsernameCheckedStatusChanged(UsernameCheckedEventArgs e) => UsernameCheckedEvent?.Invoke(this, e);
		private void OnUsernameIsAvailableStatusChanged(UsernameIsAvailableEventArgs e) => UsernameAvailableEvent?.Invoke(this, e);
		private void OnStatusConnectedChanged(ConnectedStatusEventArgs e) => ConnectedStatusEvent?.Invoke(this, e);
		private void OnMessageReceived(MessageReceivedEventArgs e) => MessageReceivedEvent?.Invoke(this, e);
		private void OnConnectionAdded(ConnectionAddedEventArgs e) => ConnectionAddedEvent?.Invoke(this, e);
		private void OnConnectionRemoved(ConnectionRemovedEventArgs e) => ConnectionRemovedEvent?.Invoke(this, e);

		// Handlers
		private void HandlePrivateMessageReceived(string message) => OnPrivateMessageReceived(new PrivateMessageReceivedEventArgs(message));
		private void HandleConnectionStatus(bool value) => OnStatusConnectedChanged(new ConnectedStatusEventArgs(value));
		private void HandleUsernameChecked(bool value) => OnUsernameCheckedStatusChanged(new UsernameCheckedEventArgs(value));
		private void HandleUsernameAvailable(bool value) => OnUsernameIsAvailableStatusChanged(new UsernameIsAvailableEventArgs(value));
		private void HandleMessageReceived(string value) => OnMessageReceived(new MessageReceivedEventArgs(value));
		private void HandleConnectionAdded(Connection value) => OnConnectionAdded(new ConnectionAddedEventArgs(value));
		private void HandleConnectionRemoved(Connection value) => OnConnectionRemoved(new ConnectionRemovedEventArgs(value));

		private CoreHandler()
		{
			IsConnected = false;
			UsernameWasChecked = false;
			UsernameAvailable = false;
		}

		public static CoreHandler Instance
		{
			get
			{
				return _instance;
			}
		}


		public async void InitializeLocalServer()
		{
			List<string> ips = new List<string>();

			var entry = Dns.GetHostEntry(Dns.GetHostName());

			foreach (IPAddress ip in entry.AddressList)
				if (ip.AddressFamily == AddressFamily.InterNetwork)
					ips.Add(ip.ToString());

			
			var port = FreeTcpPort(ips[0]);
			var localEndPoint = new IPEndPoint(IPAddress.Parse(ips[0]), port);

			_localConnection.Port = port;
			_localConnection.IpAddress = localEndPoint.Address.ToString();

			_server = new TcpListener(localEndPoint);
			_server.Start();

			while (true)
			{
				_localClient = await _server.AcceptTcpClientAsync();

				_newConnection = new Connection();
				_newConnection.Stream = _localClient.GetStream();
				_newConnection.StreamWriter = new StreamWriter(_newConnection.Stream);
				_newConnection.StreamReader = new StreamReader(_newConnection.Stream);

				var ipFromNewConnection = ((IPEndPoint)_localClient.Client.RemoteEndPoint!);

				_newConnection.IpAddress = ipFromNewConnection.Address.ToString();
				_newConnection.Port = ipFromNewConnection.Port;

				string data = _newConnection.StreamReader.ReadLine()!;
				var message = JsonConvert.DeserializeObject<Message>(data);

				if (message!.Type == MessageType.ChatRequest)
				{
					string? nick = message.Data as string;

					_newConnection.Nickname = nick;

					var existingChat = _chats.FindAll(n => n.RequesterConnection!.Nickname == _newConnection.Nickname || n.ReceiverConnection!.Nickname == _newConnection.Nickname);

					if (existingChat.Count == 0)
					{
						var chat = new Chat();
						chat.ReceiverConnection = _localConnection;
						chat.RequesterConnection = _newConnection;

						var viewModel = new PrivateChatViewModel(_newConnection);
						chat.PrivateChatViewModel = viewModel;

						var window = new PrivateChatView(viewModel);
						window.Show();


						_chats.Add(chat);

						Thread thread = new Thread(ListenAsLocalServerAsync);
						thread.Start();
					}
				}
			}
		}

		private async void ListenAsLocalServerAsync()
		{
			Connection connection = _newConnection;

			do
			{
				try
				{
					var dataReceived = await connection.StreamReader!.ReadLineAsync();
					var model = JsonConvert.DeserializeObject<Message>(dataReceived!);

					// Cuando recibimos un nuevo mensaje
					if (model!.Type == MessageType.ChatMessage)
					{
						var message = model.Data as string;
						var nickname = model.NicknameRequester;

						var chatList = _chats.FindAll(n => string.Equals(n.RequesterConnection!.Nickname, nickname) || string.Equals(n.ReceiverConnection!.Nickname, nickname));

						if (chatList.Count > 0)
						{
							var chat = chatList[0];
							chat.PrivateChatViewModel!.AddMessage(message!);
						}
					}

					// Cuando recibimos un archivo
					if (model!.Type == MessageType.File)
					{

					}

					// Cuando se cierra el chat del otro lado.
					if (model!.Type == MessageType.ChatCloseRequest)
					{
						var nickname = model.NicknameRequester;
						var chatList = _chats.FindAll(n => string.Equals(n.RequesterConnection!.Nickname, nickname) || string.Equals(n.ReceiverConnection!.Nickname, nickname));

						if (chatList.Count > 0)
						{
							var chat = chatList[0];
							chat.PrivateChatViewModel!.RequestedClosed = true;
							chat.PrivateChatViewModel!.CloseWindow();
							_chats.Remove(chat);
							_chats.RemoveAll(n => string.Equals(n.RequesterConnection!.Nickname, nickname) || string.Equals(n.ReceiverConnection!.Nickname, nickname));
							_currentRemoteClient.Close();
						}
					}
				}
				catch
				{

				}
			}
			while (!_clientClosing);
		}

		public async void ConnectoToServerAsync(string ip, string port, string username)
		{
			_serverConnection.IpAddress = ip;
			_serverConnection.Port = int.Parse(port);

			_localConnection.Nickname = username;

			await _client.ConnectAsync(IPAddress.Parse(_serverConnection.IpAddress), _serverConnection.Port);

			if (_client.Connected)
			{
				_serverConnection.Stream = _client.GetStream();
				_serverConnection.StreamWriter = new StreamWriter(_serverConnection.Stream);
				_serverConnection.StreamReader = new StreamReader(_serverConnection.Stream);

				// Avisamos al server cual sera nuestro ip, puerto y nombre de usuario
				// Mandaremos un connection solo con esos datos.

				var connection = new Connection();

				connection.Port = _localConnection.Port;
				connection.IpAddress = _localConnection.IpAddress;
				connection.Nickname = _localConnection.Nickname;

				var message = new Message
				{
					Type = MessageType.UserConnected,
					Data = JsonConvert.SerializeObject(connection),
					IpAddressRequester = _localConnection.IpAddress,
					PortRequester = _localConnection.Port,
					NicknameRequester = _localConnection.Nickname,
				};

				var json = JsonConvert.SerializeObject(message);

				_serverConnection.StreamWriter.WriteLine(json);
				_serverConnection.StreamWriter.Flush();

				Thread thread = new Thread(ListenToServerAsync);
				thread.Start();
			}
		}
		
		public void ListenToServerAsync()
		{
			while (_client.Connected)
			{
				try
				{
					var dataFromClient = _serverConnection.StreamReader!.ReadLine();

					var model = JsonConvert.DeserializeObject<Message>(dataFromClient!);

					if (model!.Type == MessageType.UserConnected)
					{
						string? json = model!.Data! as string;

						var dataFromModel = JsonConvert.DeserializeObject<Connection>(json!);
                        var existingConnection = _connections.FindAll(n => n.IpAddress == dataFromModel!.IpAddress && n.Port == dataFromModel.Port && n.Nickname == dataFromModel.Nickname);

						if (existingConnection.Count == 0)
						{
							Application.Current.Dispatcher.Invoke(() =>
							{
								HandleConnectionAdded(dataFromModel!);
							});
							
							_connections.Add(dataFromModel!);
						}
                    }

					if (model!.Type == MessageType.Message)
					{
						if(model!.Data is string)
						{
							var message = model!.Data! as string;
							HandleMessageReceived(message!);
						}
					}

					if (model!.Type == MessageType.UserDisconnected)
					{
						string? json = model!.Data! as string;

						var dataFromModel = JsonConvert.DeserializeObject<Connection>(json!);
						var existingConnection = _connections.FindAll(n => n.IpAddress == dataFromModel!.IpAddress && n.Port == dataFromModel.Port && n.Nickname == dataFromModel.Nickname);

						if (existingConnection.Count > 0)
						{
							Application.Current.Dispatcher.Invoke(() =>
							{
								HandleConnectionRemoved(existingConnection[0]);
							});

							_connections.Remove(existingConnection[0]);
						}
					}

					if (model!.Type == MessageType.UsernameInUse)
					{
						UsernameAvailable = (bool) model.Data!;
						UsernameWasChecked = true;

						//if (!UsernameAv ailable) Dispose();

						HandleUsernameChecked(UsernameWasChecked);

						Application.Current.Dispatcher.Invoke(new Action(() =>
						{
							HandleUsernameAvailable(UsernameAvailable);
						}));						
					}
				}
				catch 
				{
					
				}
			}
		}

		public async void ConnectToRemoteClientAsync(Connection connection)
		{
			if (connection.Port == _localConnection.Port && connection.Nickname == _localConnection.Nickname)
			{
				MessageBox.Show("No te puedes conectar a ti mismo.");

				return;
			}
			else
			{
				var existingChat = _chats.FindAll(n => n.RequesterConnection!.Nickname == _newConnection.Nickname || n.ReceiverConnection!.Nickname == _newConnection.Nickname);

				if (existingChat.Count == 0)
				{

					var chat = new Chat();
					chat.RequesterConnection = _localConnection;

					chat.ReceiverConnection = connection;
					_remoteConnection = chat.ReceiverConnection;

					_currentRemoteClient = new();

					await _currentRemoteClient.ConnectAsync(IPAddress.Parse(connection.IpAddress!), connection.Port);

					var stream = _currentRemoteClient.GetStream();
					var streamWriter = new StreamWriter(stream);
					var streamReader = new StreamReader(stream);

					chat.ReceiverConnection.Stream = stream;
					chat.ReceiverConnection.StreamWriter = streamWriter;
					chat.ReceiverConnection.StreamReader = streamReader;

					var message = new Message();

					message.Type = MessageType.ChatRequest;
					message.NicknameRequester =	_localConnection.Nickname;
					message.Data = _localConnection.Nickname;

					streamWriter.WriteLine(JsonConvert.SerializeObject(message));
					streamWriter.Flush();


					var viewModel = new PrivateChatViewModel(_remoteConnection);
					chat.PrivateChatViewModel = viewModel;

					var window = new PrivateChatView(viewModel);
					window.Show();

					_chats.Add(chat);

					Thread thread = new Thread(ListenToRemoteClientAsync);
					thread.Start();
				}
			}
		}

		public void ListenToRemoteClientAsync()
		{
			var connection = _remoteConnection;

			while (_currentRemoteClient.Connected)
			{
				try
				{
					var dataReceived = connection.StreamReader!.ReadLine();
					var model = JsonConvert.DeserializeObject<Message>(dataReceived!);

					// Cuando recibimos un nuevo mensaje
					if (model!.Type == MessageType.ChatMessage)
					{
						var message = model.Data as string;
						var nickname = model.NicknameRequester;

						var chatList = _chats.FindAll(n => string.Equals(n.RequesterConnection!.Nickname, nickname) || string.Equals(n.ReceiverConnection!.Nickname, nickname));

						if (chatList.Count > 0)
						{
							var chat = chatList[0];
							chat.PrivateChatViewModel!.AddMessage(message!);
						}
					}

					// Cuando se cierra el chat del otro lado.
					if (model!.Type == MessageType.ChatCloseRequest)
					{
						var nickname = model.NicknameRequester;
						var chatList = _chats.FindAll(n => string.Equals(n.RequesterConnection!.Nickname, nickname) || string.Equals(n.ReceiverConnection!.Nickname, nickname));

						if (chatList.Count > 0)
						{
							var chat = chatList[0];
							chat.PrivateChatViewModel!.RequestedClosed = true;
							chat.PrivateChatViewModel!.CloseWindow();
							_chats.Remove(chat);
							_chats.RemoveAll(n => string.Equals(n.RequesterConnection!.Nickname, nickname) || string.Equals(n.ReceiverConnection!.Nickname, nickname));
							_currentRemoteClient.Close();
						}
					}

					// Cuando recibimos un archivo
					if (model!.Type == MessageType.File)
					{

					}

					
				}
				catch 
				{ 

				}
			}
		}

		public void SendMessageToRemoteClient(Connection connection, string message)
		{
			var messageVar = new Message();

			messageVar.Type = MessageType.ChatMessage;
			messageVar.Data = message!;
			messageVar.NicknameRequester = _localConnection.Nickname;

			connection.StreamWriter!.WriteLine(JsonConvert.SerializeObject(messageVar));
			connection.StreamWriter!.Flush();
		}

		// Hay un bug cuando de repente cierra el chat. Hay bugs.
		// Checar

		public void RequestToCloseChat(Connection connection)
		{
			var messageVar = new Message();
			messageVar.Type = MessageType.ChatCloseRequest;
			messageVar.NicknameRequester = _localConnection.Nickname;
			messageVar.Data = true;

			connection.StreamWriter!.WriteLine(JsonConvert.SerializeObject(messageVar));
			connection.StreamWriter!.Flush();

			var chatList = _chats.FindAll(n => string.Equals(n.RequesterConnection!.Nickname, _localConnection.Nickname) || string.Equals(n.ReceiverConnection!.Nickname, _localConnection.Nickname));

			if (chatList.Count > 0)
			{
				var chat = chatList[0];
				_chats.Remove(chat);
				_chats.RemoveAll(n => string.Equals(n.RequesterConnection!.Nickname, _localConnection.Nickname) || string.Equals(n.ReceiverConnection!.Nickname, _localConnection.Nickname));
				//_currentRemoteClient.Close();
			}
		}

		public int FreeTcpPort(string ip)
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
		
			return port;
		}

		public void SendFileToChat()
		{
			// Para mandar el archivo
		}

		public void Dispose()
		{
			_clientClosing = true;
			//_server!.Stop();
			_client.Close();
			_localClient.Close();
			_currentRemoteClient.Close();
		}
	}
}
