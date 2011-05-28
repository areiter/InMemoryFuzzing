// GDBSubProcess.cs
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
using System.Collections.Generic;

using Fuzzer.IO.ConsoleIO;

using Iaik.Utils;
using Iaik.Utils.CommonAttributes;
using System.Threading;
using System.IO;
using System.Text;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Provides access to a GDB sub prcess
	/// </summary>
	/// <remarks>
	/// Parameters:
	/// "gdb_exec": path to the gdb executable
	/// "args": 	extra arguments to pass to gdb
	/// "gdb_log":  stream:stderr to write gdb output and commands to standard error output
	/// 			stream:stdout standard output
	/// 			file:filename to write to specified file
	/// The executable will be loaded using the file command
	/// 
	/// </remarks>
	public abstract class GDBSubProcess : ConsoleProcess
	{
		public delegate void MyMethodInvoker();		
		
		/// <summary>
		/// Path to the gdb executable
		/// </summary>
		private string _gdbExec = null;
		
		/// <summary>
		/// Extra argument to pass to gdb
		/// </summary>
		private string _extraArguments = null;
		
	
		/// <summary>
		/// Logs the entire GDB communication
		/// </summary>
		protected TextWriter _gdbLog = null;
		
		/// <summary>
		/// Command queue
		/// </summary>
		private Queue<GDBCommand> _commands = new Queue<GDBCommand>();
		
		/// <summary>
		/// Contains the permanent response handlers, that get called if the current command cannot handle a response
		/// </summary>
		private List<GDBResponseHandler> _permanentResponseHandlers = new List<GDBResponseHandler>();

		/// <summary>
		/// Current command, already sent to gdb
		/// </summary>
		private GDBCommand _currentCommand = null;
		
		/// <summary>
		/// Contains if the "(gdb)" prompt has already been received after the last command
		/// </summary>
		protected bool _gdbReadyForInput = false;
		
		public GDBSubProcess()
		{
		}			

			                                                  
		public virtual void Setup (IDictionary<string, string> config)
		{
			_gdbExec = DictionaryHelper.GetString("gdb_exec", config, "gdb");
			_extraArguments = DictionaryHelper.GetString("args", config, "");

			
			if(config.ContainsKey("gdb_log"))
			{
				string[] gdbLogParts = config["gdb_log"].Split(new char[]{':'},2);
				
				if(gdbLogParts.Length == 2 && gdbLogParts[0] == "stream" && gdbLogParts[1] == "stdout")
					_gdbLog =  Console.Out;
				else if(gdbLogParts.Length == 2 && gdbLogParts[0] == "stream" && gdbLogParts[1] == "stderr")
					_gdbLog = Console.Error;
				else if(gdbLogParts.Length == 2 && gdbLogParts[0] == "file")
				{
					_gdbLog = new StreamWriter(gdbLogParts[1]);
				}
			}
		}

		protected override void StartProcess ()
		{
			base.StartProcess ();
			ThreadPool.QueueUserWorkItem(ReadThread);	       
		}		


		#region implemented abstract members of Fuzzer.IO.Console.ConsoleProcess
		
		/// <summary>
		/// Path to the gdb executable
		/// </summary>
		protected override string Execfile 
		{
			get { return _gdbExec; }
		}
		
		/// <summary>
		/// Arguments for starting GDB
		/// --quiet...don't show version information on start up
		/// --fullname...always output full file names (emacs mode)
		/// the file to debug is provided later using the "file" command
		/// </summary>
		protected override string Arguments 
		{
			get { return "-quiet -fullname " + _extraArguments; }
		}
		
		#endregion
		
		/// <summary>
		/// Queues the command, it gets sent as soon as gdb is ready for input
		/// </summary>
		/// <param name="cmd">
		/// A <see cref="GDBCommand"/>
		/// </param>
		public void QueueCommand(GDBCommand cmd)
		{
			lock(_commands)
			{
				_commands.Enqueue(cmd);
			}
			
			ProcessQueue();
		}
		
		
		protected void RegisterPermanentResponseHandler(GDBResponseHandler responseHandler)
		{
			_permanentResponseHandlers.Add(responseHandler);
		}
		
		/// <summary>
		/// Checks if gdb is ready and sends the next command
		/// </summary>
		private void ProcessQueue()
		{
			lock(_commands)
			{
				if(_commands.Count == 0 || _gdbReadyForInput == false)
					return;
				
				_gdbReadyForInput = false;
				WriteLine(_commands.Peek().Command);
				_currentCommand = _commands.Dequeue();
				
				//If no response handler is specified, we don't need the command anymore
				if(_currentCommand.ResponseHandler == null)
					_currentCommand = null;
			}
		}
		
		protected override void WriteLine (string format, params object[] args)
		{
			GdbLogLine(string.Format(format, args));
			
			base.WriteLine (format, args);
		}
		
		protected override void Write (string format, params object[] args)
		{
			GdbLog(string.Format(format, args));
			
			base.Write (format, args);
		}
		
		protected void GdbLog(char c)
		{
			if(_gdbLog != null)
				_gdbLog.Write(c.ToString());
		}
		
		protected void GdbLog(string data)
		{
			if(_gdbLog != null)
				_gdbLog.Write(data);
		}
		
		protected void GdbLogLine(string data)
		{
			if(_gdbLog != null)
				_gdbLog.WriteLine(data);
		}
		
		#region Handle incoming messages
		private void ReadThread(object data)
		{
			StringBuilder currentLine = new StringBuilder();
			List<string> currentLines = new List<string>();
			while(Running)
			{
				char read = ReadChar();
				GdbLog(read);
				
				if(read == '\n' || read == '\r')
				{
					if(currentLine.ToString().Trim().Equals(string.Empty))
						continue;
					
					currentLines.Add(currentLine.ToString());
					currentLine.Remove(0, currentLine.Length);
					
					ReceivedNewLine(currentLines);
				}
				else
					currentLine.Append(read);
				
				lock(_commands)
				{
					if(currentLine.ToString().Trim().Equals("(gdb)"))
					{
						currentLine.Remove(0, currentLine.Length);
						_gdbReadyForInput = true;
						ProcessQueue();
					}
				}
			}
		}
		
		/// <summary>
		/// Processes responses received from GDB.
		/// There may be direct responses to commands or async responses
		/// </summary>
		/// <param name="lines"></param>
		private void ReceivedNewLine(List<string> lines)
		{
			lock(_commands)
			{
				if(_currentCommand != null && _currentCommand.ResponseHandler == null)
					_currentCommand = null;
				
				if(_currentCommand != null && _currentCommand.ResponseHandler != null)
				{
					GDBResponseHandler.HandleResponseEnum responseEnum = (GDBResponseHandler.HandleResponseEnum)
					    _currentCommand.ResponseHandler.HandleResponse(this, lines.ToArray(), !_gdbReadyForInput);

					if(responseEnum == GDBResponseHandler.HandleResponseEnum.NotHandled)
					{
						//TODO: Forward to permanent handlers
					}
					else if(responseEnum == GDBResponseHandler.HandleResponseEnum.Handled)
					{
						//Last command and response processed
						_currentCommand = null;
						lines.Clear();
					}
					else if(responseEnum == GDBResponseHandler.HandleResponseEnum.RequestLine && _gdbReadyForInput)
					{
						//Wrong behaviour
						throw new ArgumentException("Cannot request another response line if gdb is ready for input");
					}
				}
				else
				{
					foreach(GDBResponseHandler permanentResponseHandler in _permanentResponseHandlers)
					{
						GDBResponseHandler.HandleResponseEnum responseEnum = permanentResponseHandler.HandleResponse(this, lines.ToArray(), !_gdbReadyForInput);
						if(responseEnum == GDBResponseHandler.HandleResponseEnum.Handled)
						{
							lines.Clear();
							break;
						}
						else if(responseEnum == GDBResponseHandler.HandleResponseEnum.RequestLine && _gdbReadyForInput)
						{
							//Wrong behaviour
							throw new ArgumentException("Cannot request another response line if gdb is ready for input");
						}
						else if(responseEnum == GDBResponseHandler.HandleResponseEnum.RequestLine)
							break;
						
					}
				}
			}					                                               
		}
		

		#endregion
	}
}

