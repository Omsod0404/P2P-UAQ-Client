using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using P2P_UAQ_Client.Core.Events;
using P2P_UAQ_Client.Models;

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
		private TcpListener _server; // Para ser localmente el servidor y aceptar otros clientes P2p
		private TcpClient _client; // Para conectarlos al servidor


		// Eventos para actualizar la interfaz.

		public event EventHandler<PrivateMessageReceivedEventArgs> PrivateMessageReceived;

		private CoreHandler()
		{

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
						
						HandlePrivateMessageReceived(message!);
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
				
			}


		}
		
		public async void ListenToServer()
		{

		}

		public async void ConnecToRemoteClient()
		{

		}

		public async void ListenToRemoteClient()
		{

		}


		public int FreeTcpPort()
		{
			var listener = new TcpListener(IPAddress.Loopback, 0);
			listener.Start();
			int port = ((IPEndPoint)listener.LocalEndpoint).Port;
			listener.Stop();
		
			return port;
		}

		// Invokes 
		private void OnPrivateMessageReceived(PrivateMessageReceivedEventArgs e) => PrivateMessageReceived?.Invoke(this, e);

		// Handlers
		private void HandlePrivateMessageReceived(string message) => OnPrivateMessageReceived(new PrivateMessageReceivedEventArgs(message));


	}
}
