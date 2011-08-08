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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using Iaik.Utils;

namespace Iaik.Utils.Net
{

    /// <summary>
    /// Implements the FrontEndConnection for named pipes.
    /// This can be used on Server and client side
    /// </summary>
    /// <code>
    /// ...
    /// FrontEndConnection conn = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<FrontEndConnection>("named_pipe", "TPM_csharp");
    /// conn.Connect();
    /// ClientContext ctx = EndpointContext.CreateClientEndpointContext(conn);	
    /// ...
    /// </code>
    /// <remarks>valid arguments: pipe_name</remarks>
	[FrontEndConnection("net/named_pipe")]
    public sealed class NamedPipeConnection : FrontEndConnection
    {
        /// <summary>
        /// Indicates if the Connection was created using a preconnected PipeStream
        /// used for server PipeStreams.
        /// </summary>
        private bool _connectedOnCreation = false;

        /// <summary>
        /// The Anonymous/Named Pipe for communication
        /// </summary>
        private PipeStream _pipeStream = null;

        public PipeStream PipeStream
        {
            get { return _pipeStream; }
        }

        /// <summary>
        /// Contains the pipe name to connect to
        /// </summary>
        private string _pipeName = "TPM_csharp";


        public NamedPipeConnection(PipeStream pipeStream)
        {
            _pipeStream = pipeStream;
            _connectedOnCreation = true;
        }

        public NamedPipeConnection(string pipeName)
        {
			if(pipeName == null)
				throw new ArgumentException("No pipe name specified");
            _connectedOnCreation = false;
            _pipeName = pipeName;
        }
		
		public NamedPipeConnection(CommandLineHandler.CommandLineOptions commandLine)
		{
			CommandLineHandler.CommandOption pipeNameOption = commandLine.FindCommandOptionByName("PipeName");
			
			if(pipeNameOption == null || pipeNameOption.OptionType != 
			   CommandLineHandler.CommandOption.CommandOptionType.Value)
				_logger.WarnFormat("No pipe name has been specified, using default '{0}'", _pipeName);
			else
			{
				_pipeName = pipeNameOption.Arguments[0];
				_logger.DebugFormat("Using pipe name '{0}'", _pipeName);
			}
				
		}
		
		public NamedPipeConnection(IDictionary<string, string> arguments)
			:this(DictionaryHelper.GetString("pipe_name", arguments, null))
		{
		}
		
		public override bool Connected 
		{
			get{ return _pipeStream != null; }
		}

		
        public override void Connect()
        {
            if (_pipeStream == null && _connectedOnCreation == false)
            {
                NamedPipeClientStream pipeClient = new NamedPipeClientStream("localhost", _pipeName, PipeDirection.InOut,  PipeOptions.Asynchronous);
                pipeClient.Connect(5000);
                _pipeStream = pipeClient;
				RaiseConnectedEvent();
            }
        }

        public override void Close()
        {
            if (_pipeStream != null)
            {
                _pipeStream.Dispose();
                _pipeStream = null;
				RaiseDisconnectedEvent();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose();
            Close();
        }

        public override void Write(byte[] buffer, int offset, int length)
        {
            AssertPipeStream();
            _pipeStream.Write(buffer, offset, length);
        }

        public override int Read(byte[] buffer, int offset, int length)
        {
            AssertPipeStream();
            int read = _pipeStream.Read(buffer, offset, length);
            if (read == 0)
                throw new DisconnectedException();
            return read;
        }
		
		public override int ReadByte ()
		{
			AssertPipeStream();
			return _pipeStream.ReadByte();
		}

		public override void WriteByte (byte value)
		{
			AssertPipeStream();
            Write(new byte[] { value }, 0, 1);
		}

		public override void Flush ()
		{
			AssertPipeStream();
			_pipeStream.Flush();
		}

		
		/// <summary>
		/// Checks if the PipeStream is available and connected
		/// </summary>
		private void AssertPipeStream()
		{
			if (_pipeStream == null)
                throw new ConnectionException("Pipe is not connected");
		}
		
    }
}
