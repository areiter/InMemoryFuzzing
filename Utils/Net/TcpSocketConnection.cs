/* Copyright 2010 Andreas Reiter <andreas.reiter@student.tugraz.at>, 
 *                Georg Neubauer <georg.neubauer@student.tugraz.at>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


// 
//
// Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;

using System.Net.Sockets;
using log4net;
using Iaik.Utils;
using System.Net;
using System.Collections.Generic;

namespace Iaik.Utils.Net
{

	/// <summary>
	/// Implements a raw tcp socket connection without encryption. Don't use in productional
	/// environment
	/// </summary>
    /// <code>
    /// ...
    /// FrontEndConnection conn = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<FrontEndConnection>("tcp_socket", "localhost", 1234);
    /// conn.Connect();
    /// ClientContext ctx = EndpointContext.CreateClientEndpointContext(conn);	
    /// ...
    /// </code>
	[FrontEndConnection("net/tcp_socket")]
	public class TcpSocketConnection : FrontEndConnection
	{

		
		/// <summary>
		/// Specifies the remote host to connect to
		/// </summary>
		protected string _remoteHost;

		/// <summary>
		/// Specifies the port to connect to
		/// </summary>
		protected int _remotePort;
		
		/// <summary>
		/// Specifies the endpoint to use for connecting
		/// </summary>
		protected IPEndPoint _endpoint;
		
		/// <summary>
		/// The socket
		/// </summary>
		protected Socket _socket = null;
		
		/// <summary>
		/// Indicates if this Connection can be reconnected or if it 
		/// was created using a preconnected socket
		/// </summary>
		/// <remarks>Valid arguments: host, port</remarks>
		protected bool _createdFromSocket = false;
		
		
		public TcpSocketConnection(string remoteHost, string port)
			:this(remoteHost, int.Parse(port))
		{
		}
		
		public TcpSocketConnection (string remoteHost, int port)
		{
			if(remoteHost == null)
				throw new ArgumentException("No remote host specified");
				
			_logger.Debug(string.Format("Creating TcpSocketConnection with host={0}, port={1}", remoteHost, port));
			_remoteHost = remoteHost;
			_remotePort = port;
			
			IPHostEntry hostEntry = Dns.GetHostEntry(remoteHost);
			if(hostEntry == null || hostEntry.AddressList == null || hostEntry.AddressList.Length == 0)
			{
				_logger.ErrorFormat("Could not resolve host '{0}'", remoteHost);
				throw new ConnectionException(string.Format("Could not resolve host '{0}'", remoteHost));
			}
			
			_endpoint = new IPEndPoint(hostEntry.AddressList[0], port);
		}
		
		public TcpSocketConnection(Socket socket)
		{
			_logger.Debug("Creating TcpSocketConnection with preconnected socket");
			_socket = socket;
			_createdFromSocket = true;
		}
		
		public TcpSocketConnection(IDictionary<string, string> arguments)
			:this(DictionaryHelper.GetString("host", arguments, null),
			      DictionaryHelper.GetInt("port", arguments, -1))
		{				
		}
		
		#region FrontEndConnection overrides
		
		public override bool Connected 
		{
			get { return _socket != null && _socket.Connected;}
		}

		
		public override void Flush ()
		{
			//No flushing required
		}

		public override void Connect()
		{
			if(_createdFromSocket)
				throw new ConnectionException("Cannot reconnect preconnected socket");
				
			if(_socket == null || _socket.Connected == false)
			{
				_logger.Info(string.Format("Connecting to '{0}:{1}'", _remoteHost, _remotePort));
				try
				{
					_socket = new Socket(_endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					_socket.Connect(_endpoint);
				}
				catch(Exception ex)
				{
					throw new ConnectionFailureException(ex.Message);
				}
			}
		}
		
		public override void Close ()
		{
			_logger.Info(string.Format("Closing '{0}:{1}'", _remoteHost, _remotePort));
			_socket.Close();
		}

	
		public override void Write (byte[] buffer, int offset, int length)
		{
			if(Connected == false)
				throw new ConnectionException("Socket not connected");
			
			_socket.Send(buffer, offset, length, SocketFlags.None);
		}
		
		public override int Read (byte[] buffer, int offset, int length)
		{
			if(Connected == false)
				throw new ConnectionException("Socket not connected");
			
			int read = _socket.Receive(buffer, offset, length, SocketFlags.None);
			
			if(read == 0)
			{
				RaiseDisconnectedEvent();
				throw new DisconnectedException();
			}
			
			return read;
		}


	
		#endregion
		
	}
}
