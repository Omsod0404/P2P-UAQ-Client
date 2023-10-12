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
using P2P_UAQ_Client.Models;

namespace P2P_UAQ_Client.Core
{
	public class CoreHandler
	{
		private readonly static CoreHandler _instance = new CoreHandler();

		private Connection _localConnection = new Connection();
		private Connection _newConnection = new Connection();
		private List<Connection> _connections = new List<Connection>();
		private TcpListener _server;
		private TcpClient _client;

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

						Thread thread = new Thread(ListenAsLocalServer);
						thread.Start();
					}
				}
			}
		}

		private async void ListenAsLocalServer()
		{
			Connection connection = _newConnection;

			do
			{
				try
				{
					var dataReceived = await connection.StreamReader!.ReadLineAsync();
					var model = JsonConvert.DeserializeObject<Message>(dataReceived!);

					// Cuando recibimos un nuevo usuario
					if (model!.Type == MessageType.ChatMessage)
					{
						var nick = model.Data;
					}
					else
					{

					}

				}
				catch
				{

				}
			}
			while (true);
		}

		public async void ConnectoToServer()
		{

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
	}
}
