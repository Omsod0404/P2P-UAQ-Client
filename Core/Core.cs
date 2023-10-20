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
		private readonly static CoreHandler _instance = new CoreHandler();

		private Connection _localConnection = new Connection(); // Esta variable contiene nuestra info local.
		private Connection _serverConnection = new Connection(); // Nuestra conexion al servidor.
		private Connection _newConnection = new Connection(); // Variable reutilizable para los usuarios conectados.
		private List<Connection> _connections = new List<Connection>(); // Los que estan conectados. 
		private List<Chat> _chats = new List<Chat>(); // Lista para los chats actualmente activos.
		private TcpListener? _server; // Para ser localmente el servidor y aceptar otros clientes P2p
		private TcpClient _client = new TcpClient(); // Para conectarlos al servidor
		private TcpClient _currentRemoteClient = new TcpClient();
		public bool IsConnected { get; private set; }
		public bool UsernameWasChecked { get; private set; }
		public bool UsernameAvailable { get; private set; }

		// Eventos para actualizar la interfaz.

		public event EventHandler<PrivateMessageReceivedEventArgs>? PrivateMessageReceived;
		public event EventHandler<ConnectedStatusEventArgs>? ConnectedStatusEvent;
		public event EventHandler<UsernameCheckedEventArgs>? UsernameCheckedEvent;
		public event EventHandler<UsernameIsAvailableEventArgs>? UsernameAvailableEvent;

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
			var port = FreeTcpPort();
			var localEndPoint = new IPEndPoint(IPAddress.Any, port);

			_localConnection.Port = port;
			_localConnection.IpAddress = localEndPoint.Address.ToString();

			_server = new TcpListener(localEndPoint);
			_server.Start();

			while (true)
			{
				_client = await _server.AcceptTcpClientAsync();

				_newConnection = new Connection();
				_newConnection.Stream = _client.GetStream();
				_newConnection.StreamWriter = new StreamWriter(_newConnection.Stream);
				_newConnection.StreamReader = new StreamReader(_newConnection.Stream);

				var ipFromNewConnection = ((IPEndPoint)_client.Client.RemoteEndPoint!);

				_newConnection.IpAddress = ipFromNewConnection.Address.ToString();
				_newConnection.Port = ipFromNewConnection.Port;

				string data = _newConnection.StreamReader.ReadLine()!;
				var message = JsonConvert.DeserializeObject<Message>(data);

				if (message!.Type == MessageType.ChatRequest)
				{
					string? nick = message.Data as string;

					_newConnection.Nickname = nick;

					var existingConnection = _connections.FindAll(n => n.Nickname == _newConnection.Nickname && n.IpAddress == n.IpAddress && n.Port == _newConnection.Port);

					if (existingConnection.Count == 0)
					{
						var viewModel = new PrivateChatViewModel();
						viewModel.Connection = _newConnection;
						viewModel.Username = nick!;

						var window = new PrivateChatView(viewModel);
						window.Show();
						




						_connections.Add(_newConnection);

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
						var ip = model.IpAddressRequester;
						var port = model.PortRequester;
						var nickname = model.NicknameRequester;

						var requesterList = _chats.FindAll(n => n.RequesterConnection!.IpAddress == ip && n.RequesterConnection.Port == port && n.RequesterConnection.Nickname == nickname);
						var receiverlist = _chats.FindAll(n => n.ReceiverConnection!.IpAddress == ip && n.ReceiverConnection.Port == port && n.ReceiverConnection.Nickname == nickname);

						if (requesterList.Count > 0)
						{
							var chat = requesterList[0];

							chat.PrivateChatViewModel!.AddMessage(message!);

						}

						if (receiverlist.Count > 0)
						{
							var chat = receiverlist[0];

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

					}
				}
				catch
				{

				}
			}
			while (true);
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
		
		public async void ListenToServerAsync()
		{
			while (_client.Connected)
			{
				try
				{
					var dataFromClient = await _serverConnection.StreamReader!.ReadLineAsync();

					var model = JsonConvert.DeserializeObject<Message>(dataFromClient!);

					if (model!.Type == MessageType.UserConnected)
					{
						var dataFromModel = model.Data as Connection;

						var existingConnection = _connections.FindAll(n => n.IpAddress == dataFromModel!.IpAddress && n.Port == dataFromModel.Port && n.Nickname == dataFromModel.Nickname);

						if (existingConnection.Count == 0)
						{

							_connections.Add(dataFromModel!);

						}
					}
					if (model!.Type == MessageType.UserDisconnected)
					{

					}

					if (model!.Type == MessageType.UsernameInUse)
					{
						UsernameAvailable = (bool) model.Data!;
						UsernameWasChecked = true;
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
			var chat = new Chat();
			chat.RequesterConnection = connection;
			_newConnection = connection;

			var client = new TcpClient();

			await client.ConnectAsync(IPAddress.Parse(connection.IpAddress!), connection.Port);

			chat.ReceiverConnection!.Stream = client.GetStream();
			chat.ReceiverConnection!.StreamWriter = new StreamWriter(chat.ReceiverConnection!.Stream);
			chat.ReceiverConnection!.StreamReader = new StreamReader(chat.ReceiverConnection!.Stream);

			_chats.Add(chat);

			Thread thread = new Thread(ListenToRemoteClientAsync);
			thread.Start();
		}

		public async void ListenToRemoteClientAsync()
		{
			var connection = _newConnection;

			while (_currentRemoteClient.Connected)
			{
				try
				{
					var dataReceived = await connection.StreamReader!.ReadLineAsync();
					var model = JsonConvert.DeserializeObject<Message>(dataReceived!);

					// Cuando recibimos un nuevo mensaje
					if (model!.Type == MessageType.ChatMessage)
					{
						var message = model.Data as string;
						var ip = model.IpAddressRequester;
						var port = model.PortRequester;
						var nickname = model.NicknameRequester;

						var requesterList = _chats.FindAll(n => n.RequesterConnection!.IpAddress == ip && n.RequesterConnection.Port == port && n.RequesterConnection.Nickname == nickname);
						var receiverlist = _chats.FindAll(n => n.ReceiverConnection!.IpAddress == ip && n.ReceiverConnection.Port == port && n.ReceiverConnection.Nickname == nickname);

						if (requesterList.Count > 0)
						{
							var chat = requesterList[0];

							chat.PrivateChatViewModel!.AddMessage(message!);

						}

						if (receiverlist.Count > 0)
						{
							var chat = receiverlist[0];

							chat.PrivateChatViewModel!.AddMessage(message!);
						}

					}
				}
				catch { 

				}
			}
		}

		public int FreeTcpPort()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
		
			return port;
		}

		// Metodo para mandar un mensaje
		public async void SendMessageToRemoteClient(Connection connection, string message) => await connection.StreamWriter!.WriteLineAsync(message!);
		

		// Invokes 
		private void OnPrivateMessageReceived(PrivateMessageReceivedEventArgs e) => PrivateMessageReceived?.Invoke(this, e);
		private void OnUsernameCheckedStatusChanged(UsernameCheckedEventArgs e) => UsernameCheckedEvent?.Invoke(this, e);
		private void OnUsernameIsAvailableStatusChanged(UsernameIsAvailableEventArgs e) => UsernameAvailableEvent?.Invoke(this, e);
		private void OnStatusConnectedChanged(ConnectedStatusEventArgs e) => ConnectedStatusEvent?.Invoke(this, e);

		// Handlers
		private void HandlePrivateMessageReceived(string message) => OnPrivateMessageReceived(new PrivateMessageReceivedEventArgs(message));
		private void HandleUsernameChecked(bool value) => OnUsernameCheckedStatusChanged(new UsernameCheckedEventArgs(value));
		private void HandleUsernameAvailable(bool value) => OnUsernameIsAvailableStatusChanged(new UsernameIsAvailableEventArgs(value));
		private void HandleConnectionStatus(bool value) => OnStatusConnectedChanged(new ConnectedStatusEventArgs(value));

	}
}
