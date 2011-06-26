// RemoteControlProtocol.cs
//  
//  Author:
//       Andreas Reiter <andreas.reiter@student.tugraz.at>
// 
//  Copyright 2011  Andreas Reiter
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
namespace Fuzzer.RemoteControl
{
	public class RemoteControlProtocol
	{
		public delegate void HandlerCallbackDelegate(string receiver, byte[] data);
		public delegate void PipeOpenedDelegate(int pipeId, string pipeName);
		public delegate void PipeDataDelegate(int pipeId, string pipeName, byte[] data, int index, int offset);
		public delegate void PipeClosedDelegate(int pipeId, string pipeName);
		public delegate void ExecStatusDelegate(string name, int pid, int status);
		public delegate void RemoteProcessesDelegate(RemoteProcessInfo[] processes);
		
		public event PipeOpenedDelegate PipeOpened;
		public event PipeClosedDelegate PipeClosed;
		public event PipeDataDelegate PipeData;
		public event ExecStatusDelegate ExecStatus;
		public event RemoteProcessesDelegate RemoteProcessInfo;
		
		
		
		private Dictionary<string, HandlerCallbackDelegate> _handlers = new Dictionary<string, HandlerCallbackDelegate>();
		
		/// <summary>
		/// Stream to the remote control application
		/// </summary>
		private Stream _conn; 
		
		/// <summary>
		/// the Buffer
		/// </summary>
		private List<byte> _buffer = new List<byte>();
		
		/// <summary>
		/// Buffer for a single read call;
		/// </summary>
		private byte[] _intermediateReadBuffer = new byte[1000];
		
		private byte[] _prefix = Encoding.ASCII.GetBytes("FUZZ");
		
		/// <summary>
		/// Contains all requested pipes where no RPIR (Response pipe registered) command was received
		/// </summary>
		private Queue<string> _requestedPipes = new Queue<string>();
		
		/// <summary>
		/// Contains all registered pipes
		/// </summary>
		private Dictionary<int, string> _registeredPipes = new Dictionary<int, string>();
		
		public RemoteControlProtocol ()
		{
			RegisterHandler ("RPIR", HandlerRegisteredPipe);
			RegisterHandler ("RPIP", HandlerPipeData);
			RegisterHandler ("RPIC", HandlerPipeClosed);
			RegisterHandler ("REXS", HandlerExecStatus);
			RegisterHandler ("RPRC", HandlerRemoteProcesses);
		}
		
		public void RegisterHandler (string receiver, HandlerCallbackDelegate handlerCallback)
		{
			if (_handlers.ContainsKey (receiver))
				_handlers[receiver] = handlerCallback;
			else
				_handlers.Add (receiver, handlerCallback);
		}
		
		public void SetConnection (Stream conn)
		{
			_conn = conn;
			StartReading ();
		}
		
		private void StartReading ()
		{
			_conn.BeginRead (_intermediateReadBuffer, 0, _intermediateReadBuffer.Length,
				CBRead, null);
		}
				
		private void CBRead (IAsyncResult ar)
		{
			int readBytes = _conn.EndRead (ar);
			
			for (int i = 0; i < readBytes; i++)
				_buffer.Add (_intermediateReadBuffer[i]);
			
			ProcessBuffer ();
			
			if (readBytes > 0)
				_conn.BeginRead (_intermediateReadBuffer, 0, _intermediateReadBuffer.Length, CBRead, null);
		
		}
		
		
		/// <summary>
		/// Extracts and removes complete received packets from the buffer
		/// </summary>
		private void ProcessBuffer ()
		{
			
			while (_buffer.Count > 0)
			{
		
				int? realStart = FindFirstOccurance (0, _prefix);
				if (realStart == null)
					return;
				
				_buffer.RemoveRange (0, realStart.Value);
				
					
				//Check if minimum length is available
				//FUZZ 4 + RECEIVER 4 + DataLength2
				if (_buffer.Count < 10)
					return;
				
				byte[] rawData = _buffer.ToArray ();
				
				short dataLength = BitConverter.ToInt16 (rawData, 8);
			
				//Check if complete packet is available
				if (_buffer.Count < 10 + dataLength)
					return;
				
				//yeah...we got a complete packet			
				string receiver = Encoding.ASCII.GetString (rawData, 4, 4);
				byte[] data = new byte[dataLength];
				Array.Copy (rawData, 10, data, 0, dataLength);
				_buffer.RemoveRange (0, 10 + dataLength);
				
				if (_handlers.ContainsKey (receiver))
					_handlers[receiver] (receiver, data);
				else
					Console.WriteLine ("Cannot find handler for packet with '{0}'", receiver);
			}
		}
		
		/// <summary>
		/// Finds the first occurance of data in the buffer
		/// </summary>
		/// <param name="startIndex"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		private int? FindFirstOccurance (int startIndex, byte[] data)
		{
			for (int i = startIndex; i < _buffer.Count - data.Length; i++)
			{
				if (_buffer[i] == data[0])
				{
					bool match = true;
					for (int c = 1; c < data.Length; c++)
					{
						if (_buffer[i + c] != data[c])
						{
							match = false;
							break;
						}
					}
					
					if (match)
						return i;
				}
			}
			
			return null;
		}
		
		
		private void SendPacket (string receiver, byte[] data)
		{
			byte[] receiverBytes = Encoding.ASCII.GetBytes (receiver);
			
			if (receiverBytes.Length != 4)
				throw new ArgumentException ("4 bytes receiver expected");
			
			_conn.Write (Encoding.ASCII.GetBytes ("FUZZ"), 0, 4);
			_conn.Write (receiverBytes, 0, receiverBytes.Length);
			_conn.Write (BitConverter.GetBytes ((UInt16)data.Length), 0, 2);
			_conn.Write (data, 0, data.Length);
		}
		
		public void SendCommand (RemoteCommand cmd)
		{
			SendPacket (cmd.Receiver, cmd.Data);
		}
		
		public void RemoteEcho (string echoString)
		{
			SendCommand (new EchoCommand (echoString));
		}
		
		/// <summary>
		/// Opens a remote pipe and sends all received data
		/// </summary>
		/// <param name="pipeName"></param>
		public void RemoteRequestPipe (string pipeName)
		{
			_requestedPipes.Enqueue (pipeName);
			SendCommand (new RequestPipeCommand (pipeName));
		}
		
		/// <summary>
		/// Starts the specified application (path) on the remote path
		/// </summary>
		/// <param name="name">Name can be chosen freely and is always used when this program instance is referenced</param>
		/// <param name="path">Path to start</param>
		/// <param name="arguments">Arguments to pass to the application</param>
		/// <param name="environment">Environment variables to set in the form variable=value</param>
		public void RemoteExec (string name, string path, IList<string> arguments, IList<string> environment)
		{
			SendCommand (new RemoteExecCommand (name, path, arguments, environment));
		}
		
		/// <summary>
		/// Requests the remote running processes
		/// </summary>
		public void RemoteProcesses ()
		{
			SendCommand (new RemoteProcessesCommand ());
		}
		#region Internal packet handlers
		/// <summary>
		/// And pipe request was sent. This assigns a unique id to the requested pipe
		/// </summary>
		/// <param name="receiver"></param>
		/// <param name="data"></param>
		private void HandlerRegisteredPipe (string receiver, byte[] data)
		{
			lock (_registeredPipes)
			{
				lock (_requestedPipes)
				{
					
					short pipeId = BitConverter.ToInt16 (data, 0);
					if (_registeredPipes.ContainsKey (pipeId) == false && _requestedPipes.Count > 0)
						_registeredPipes[pipeId] = _requestedPipes.Dequeue ();
					else if (_requestedPipes.Count > 0)
						_registeredPipes.Add (pipeId, _requestedPipes.Dequeue ());
					
					RaisePipeOpened (pipeId);
				}
			}
		
		}
		
		/// <summary>
		/// Pipe data was sent
		/// </summary>
		/// <param name="receiver"></param>
		/// <param name="data"></param>
		private void HandlerPipeData (string receiver, byte[] data)
		{
			lock (_registeredPipes)
			{
				short pipeId = BitConverter.ToInt16 (data, 0);

				RaisePipeData (pipeId, data, 2, data.Length - 2);
			}
		}
		
		private void HandlerPipeClosed (string receiver, byte[] data)
		{
			lock (_registeredPipes)
			{
				short pipeId = BitConverter.ToInt16 (data, 0);
				RaisePipeClosed (pipeId);
				
				_registeredPipes.Remove (pipeId);
			}
		}
		
		private void HandlerExecStatus (string receiver, byte[] data)
		{
			int nameLength = BitConverter.ToInt16 (data, 0);
			string name = Encoding.ASCII.GetString (data, 2, nameLength);
			int pid = BitConverter.ToInt32 (data, 2 + nameLength);
			int status = BitConverter.ToInt32 (data, 2 + nameLength + 4);
			
			RaiseExecStatus (name, pid, status);
		}
		
		private void HandlerRemoteProcesses (string receiver, byte[] data)
		{
			int currentOffset = 0;
			int processCount = BitConverter.ToInt32 (data, currentOffset);
			currentOffset += 4;
			
			List<RemoteProcessInfo> remoteProcesses = new List<RemoteProcessInfo> ();
			
			for (int i = 0; i < processCount; i++)
			{
				remoteProcesses.Add (new RemoteProcessInfo (data, ref currentOffset));
			}
			
			RaiseRemoteProcessInfo (remoteProcesses.ToArray ());
			
		}
		#endregion
		
		private void RaisePipeOpened (int pipeId)
		{
			if (PipeOpened != null)
			{
				PipeOpened (pipeId, GetPipeName (pipeId));
			}
		}
		
		private void RaisePipeClosed (int pipeId)
		{
			if (PipeClosed != null)
			{
				PipeClosed (pipeId, GetPipeName (pipeId));
			}
		}
		
		private void RaisePipeData (int pipeId, byte[] pipeData, int index, int offset)
		{
			if (PipeData != null)
			{
				PipeData (pipeId, GetPipeName (pipeId), pipeData, index, offset);
			}
		
		}
		
		private void RaiseRemoteProcessInfo (RemoteProcessInfo[] remoteProcesses)
		{
			if (RemoteProcessInfo != null)
				RemoteProcessInfo (remoteProcesses);
		}
		
		private string GetPipeName (int pipeId)
		{
			if (_registeredPipes.ContainsKey (pipeId))
				return _registeredPipes[pipeId];
			else
				return "<not available>";
		}
		
		private void RaiseExecStatus (string name, int pid, int status)
		{
			if (ExecStatus != null)
				ExecStatus (name, pid, status);
		}
	}
}

