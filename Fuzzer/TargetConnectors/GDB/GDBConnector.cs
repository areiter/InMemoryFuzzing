// GDBConnector.cs
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
	/// Builds a connection to the target using a gdb subprocess
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
	[ClassIdentifier("general/gdb")]
	public class GDBConnector : ConsoleProcess, ITargetConnector
	{
		public enum GdbStopReason
		{
			Breakpoint
		}
		
		public delegate void GdbStopDelegate(GdbStopReason stopReason, GDBBreakpoint breakpoint, UInt64 address);
		
		
		/// <summary>
		/// Path to the gdb executable
		/// </summary>
		private string _gdbExec = null;
		
		/// <summary>
		/// Extra argument to pass to gdb
		/// </summary>
		private string _extraArguments = null;
		
		/// <summary>
		/// Target specifier
		/// </summary>
		private string _target = null;
		
		/// <summary>
		/// Logs the entire GDB communication
		/// </summary>
		private TextWriter _gdbLog = null;
		
		/// <summary>
		/// Command queue
		/// </summary>
		private Queue<GDBCommand> _commands = new Queue<GDBCommand>();
		
		/// <summary>
		/// Contains the permanent response handlers, that get called if the current command cannot handle a response
		/// </summary>
		private List<GDBResponseHandler> _permanentResponseHandlers = new List<GDBResponseHandler>();
		
		/// <summary>
		/// Contains all current breakpoints
		/// </summary>
		private Dictionary<int, GDBBreakpoint> _breakpoints = new Dictionary<int, GDBBreakpoint>();
		
		/// <summary>
		/// Current command, already sent to gdb
		/// </summary>
		private GDBCommand _currentCommand = null;
		
		/// <summary>
		/// Contains if the "(gdb)" prompt has already been received after the last command
		/// </summary>
		private bool _gdbReadyForInput = false;
		
		private ManualResetEvent _gdbStopEventHandler = new ManualResetEvent(false);
		
		public GDBConnector()
		{
			RegisterPermanentResponseHandler(new BreakpointRH(this, GdbStopped));
			RegisterPermanentResponseHandler(new UnhandledRH());
			
		}			
		
		private void GdbStopped(GdbStopReason stopReason, GDBBreakpoint breakpoint, UInt64 address)
		{
			_gdbStopEventHandler.Set();	
		}
		
			                                                  
		#region ITargetConnector implementation
		public void Setup (IDictionary<string, string> config)
		{
			_gdbExec = DictionaryHelper.GetString("gdb_exec", config, "gdb");
			_extraArguments = DictionaryHelper.GetString("args", config, "");
			_target = DictionaryHelper.GetString("target", config, "extended-remote :1234");
			
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

		public void Connect ()
		{
			StartProcess();
			ThreadPool.QueueUserWorkItem(ReadThread);	       

			bool connected = false;
			ManualResetEvent connectedEvt = new ManualResetEvent(false);
			TargetCmd targetCmd = new TargetCmd(_target,
			    (Action<bool>)delegate(bool connectionStatus){
					connected = connectionStatus;
					connectedEvt.Set();
			});
			QueueCommand(targetCmd);
			connectedEvt.WaitOne();
			
			if(!connected)
				throw new Exception("Could not establish connection");
		}
	
		
			                            
		public void Close ()
		{
			QueueCommand(new CloseCmd());
		}

		public ulong ReadMemory (byte[] buffer, ulong address, ulong size)
		{
			throw new NotImplementedException ();
		}

		public ulong WriteMemory (byte[] buffer, ulong address, ulong size)
		{
			throw new NotImplementedException ();
		}

		public IBreakpoint SetSoftwareBreakpoint (ulong address, ulong size, string identifier)
		{
			int breakpointNum = 0;
			ManualResetEvent evt = new ManualResetEvent(false);
			
			
			QueueCommand(new SetBreakpointCmd(address,
			    (Action<int>)delegate(int num){
					breakpointNum = num;
					evt.Set();
			}));
			
			evt.WaitOne();
			GDBBreakpoint breakpoint = new GDBBreakpoint(this, breakpointNum, address, identifier, 
			      BreakpointRemoveFromList );
			
			_breakpoints.Add(breakpointNum, breakpoint);
			
			return breakpoint;
		}

		public GDBBreakpoint LookupBreakpoint(int breakpointNum)
		{
			if(_breakpoints.ContainsKey(breakpointNum))
				return _breakpoints[breakpointNum];
			
			return null;
		}
		
		
		
		public void DebugContinue ()
		{
			bool success = false;
			ManualResetEvent evt = new ManualResetEvent(false);
			_gdbStopEventHandler.Reset();
			QueueCommand(new ContinueCmd(
			    (Action<bool>)delegate(bool bSuc)
                {
					success	= bSuc;
					evt.Set();
				}));
			
			while(true)
			{
				//If continue response was received and a negative response was received,
				//throw an exception
				if(evt.WaitOne(10) && success == false)
					throw new ArgumentException("Continuing failed");
				
				
				if(_gdbStopEventHandler.WaitOne(10))
					break;
			}
		}

		public bool Connected 
		{
			get { return Running; }
		}
		#endregion

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
		
		
		private void RegisterPermanentResponseHandler(GDBResponseHandler responseHandler)
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
		
		private void GdbLog(char c)
		{
			if(_gdbLog != null)
				_gdbLog.Write(c.ToString());
		}
		
		private void GdbLog(string data)
		{
			if(_gdbLog != null)
				_gdbLog.Write(data);
		}
		
		private void GdbLogLine(string data)
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
					GDBResponseHandler.HandleResponseEnum responseEnum = _currentCommand.ResponseHandler.HandleResponse(this, lines.ToArray(), !_gdbReadyForInput);
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
						if(permanentResponseHandler.HandleResponse(this, lines.ToArray(), !_gdbReadyForInput) == GDBResponseHandler.HandleResponseEnum.Handled)
						{
							lines.Clear();
							break;
						}
					}
				}
			}					                                               
		}
		

		#endregion
		
		private void BreakpointRemoveFromList(int breakpointNum)
		{	
			_breakpoints.Remove(breakpointNum);
		}
		
		
	}
}

