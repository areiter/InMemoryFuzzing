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
using System.Collections.Generic;
using Mono.Unix;
using System.IO;

namespace Iaik.Utils.Net
{

	/// <summary>
	/// Implements the named pipe replacement for unix systems,
	/// as of Mono 2.6.1 NamedPipes are not yet supported on unix systems
	/// </summary>
    /// <code>
    /// ...
    /// FrontEndConnection conn = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<FrontEndConnection>("unix_socket", "/tmp/tpm_socket");
    /// conn.Connect();
    /// ClientContext ctx = EndpointContext.CreateClientEndpointContext(conn);	
    /// ...
    /// </code>
    /// <remarks>
    /// Valid arguments: socket_file
	/// </remarks>
	[FrontEndConnection("net/unix_socket")]
	public sealed class UnixSocketConnection : FrontEndConnection
	{
		public event Action<UnixSocketConnection> Hook_BeforeSocketCreation;
		public event Action<UnixSocketConnection> Hook_AfterSocketCreation;
		public event Action<UnixSocketConnection> Hook_AfterSocketConnect;
		public event Action<UnixSocketConnection> Hook_BeforeSocketClose;
		public event Action<UnixSocketConnection> Hook_AfterSocketClose;
		
		/// <summary>
		/// Specifies the unix socket file to use
		/// </summary>
		private string _socketFile;

		/// <summary>
		/// Specifies the endpoint to use for connecting
		/// </summary>
		private UnixEndPoint _endpoint;
		
		/// <summary>
		/// The socket to connect thru
		/// </summary>
		private Socket _socket = null;
		
		/// <summary>
		/// Indicates if this Connection can be reconnected or if it 
		/// was created using a preconnected socket
		/// </summary>
		private bool _createdFromSocket = false;
		
		/// <summary>
		/// Returns the unix socket, used to retrieve the uid of the running process
		/// </summary>
		public Socket UnixSocket
		{
			get{ return _socket; }
		}
		
		public UnixSocketConnection (string socketFile)
		{
			if(socketFile == null)
				throw new ArgumentException("No socket file specified");
				
			_logger.Debug(string.Format("Creating UnixSocketConnection with socketFile={0}", socketFile));
			_socketFile = socketFile;
			_endpoint = new UnixEndPoint(socketFile);
		}
		
		public UnixSocketConnection(Socket socket)
		{
			_logger.Debug("Creating UnixSocketConnection with preconnected socket");
			_socket = socket;
			_createdFromSocket = true;
		}
		
		public UnixSocketConnection(CommandLineHandler.CommandLineOptions commandLine)
		{
			CommandLineHandler.CommandOption socketFileOption = commandLine.FindCommandOptionByName("SocketFile");
			
			if(socketFileOption == null || socketFileOption.OptionType != 
			   CommandLineHandler.CommandOption.CommandOptionType.Value)
				throw new ArgumentException("No socket file specified!");
			else
			{
				_socketFile = socketFileOption.Arguments[0];
				_logger.DebugFormat("Using socket file '{0}'", _socketFile);
			}
				
		}
		
		public UnixSocketConnection(IDictionary<string, string> arguments)
			:this(DictionaryHelper.GetString("socket_file", arguments, null))
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

		public override void Connect ()
		{
			if (_createdFromSocket)
				throw new ConnectionException ("Cannot reconnect preconnected socket");
			
			if (_socket == null || _socket.Connected == false)
			{
				_logger.Info (string.Format ("Connecting to '{0}'", _socketFile));
				try
				{
					if (Hook_BeforeSocketCreation != null)
						Hook_BeforeSocketCreation (this);
					
					_socket = new Socket (AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);

					if (Hook_AfterSocketCreation != null)
						Hook_AfterSocketCreation (this);
					
					_socket.Connect(_endpoint);
					
					if(Hook_AfterSocketConnect != null)
						Hook_AfterSocketConnect(this);
				}
				catch(Exception ex)
				{
					throw new ConnectionFailureException(ex.Message);
				}
			}
		}
		
		public override void Close ()
		{
			if (Hook_BeforeSocketClose != null)
				Hook_BeforeSocketClose (this);
			
			_logger.Info (string.Format ("Closing '{0}'", _socketFile));
			_socket.Close ();
			
			if (Hook_AfterSocketClose != null)
				Hook_AfterSocketClose (this);
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
