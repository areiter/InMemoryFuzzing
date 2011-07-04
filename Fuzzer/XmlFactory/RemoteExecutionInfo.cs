// RemoteExecutionInfo.cs
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
using Fuzzer.RemoteControl;
using System.Threading;
using Iaik.Utils;
using System.Collections.Generic;
namespace Fuzzer.XmlFactory
{
	/// <summary>
	/// Contains informations about a program that is being launched on the remote side
	/// </summary>
	public class RemoteExecutionInfo
	{
		public enum ExecutionStatus
		{
			Success,
			Error,
			Timeout
		}
		
		/// <summary>
		/// The command sent to the remote side
		/// </summary>
		private RemoteExecCommand _cmd;
		
		/// <summary>
		/// Constains the execution status after the response has been received
		/// </summary>
		private ExecutionStatus _execStatus;
		
		/// <summary>
		/// Contains the error code, if _execStatus != Success
		/// </summary>
		private int _errorCode = 0;
		
		/// <summary>
		/// Process id of the started process
		/// </summary>
		private int _remotePid = 0;
		
		/// <summary>
		/// Event used to trigger the arrival of the status code
		/// </summary>
		private ManualResetEvent _evt = new ManualResetEvent(false);
		
		public RemoteExecCommand Cmd
		{
			get{ return _cmd;}
		}
		
		public ExecutionStatus ExecStatus
		{
			get{ return _execStatus;}
			set{ _execStatus = value;}
		}
		
		public int ErrorCode
		{
			get{ return _errorCode;}
			set{_errorCode = value;}
		}
		
		public int PID
		{
			get{ return _remotePid; }
			set{ _remotePid = value;}
		}
		
		public ManualResetEvent SyncEvent
		{
			get{ return _evt; }
		}
		
		public RemoteExecutionInfo (RemoteExecCommand cmd)
		{
			_cmd = cmd;
		}
		
		public void SendCommand (RemoteControlProtocol r)
		{
			RemoteProcessInfo[] processes = null;
			ManualResetEvent evt = new ManualResetEvent (false);
			
			r.RemoteProcessInfo += delegate(RemoteProcessInfo[] lProcesses) {
				processes = lProcesses;
				evt.Set ();
			};
			
			
			
			SimpleFormatter f = new SimpleFormatter ();
			f.OnGetParameter += delegate(string parameterName) {
				KeyValuePair<string, string>? keyP = StringHelper.SplitToKeyValue (parameterName, "|");
				if (keyP == null)
					throw new ArgumentException ("Invalid variable speciication");
				
				switch (keyP.Value.Key)
				{
				case "remote-pid":
					if (processes == null)
					{
						r.RemoteProcesses ();
						evt.WaitOne ();
					}
					RemoteProcessInfo procInfo = FindProcess (processes, keyP.Value.Value);
					if (procInfo == null)
						throw new ArgumentException (string.Format ("Could not find process '{0}'", keyP.Value.Value));
					return procInfo.Pid.ToString ();
				
				default:
					throw new NotSupportedException ("The specified variable is not supported");
				}
			};
			
			string newCmd = f.Format (_cmd.Path);
			List<string> newArgs = new List<string> ();
			foreach (string arg in _cmd.Args)
				newArgs.Add (f.Format (arg));
			
			List<string> newEnv = new List<string> ();
			foreach (String env in _cmd.EnvP)
				newEnv.Add (f.Format (env));
			
			r.SendCommand (new RemoteExecCommand (_cmd.Name, newCmd, newArgs, newEnv));
		}
		
		private RemoteProcessInfo FindProcess (RemoteProcessInfo[] processes, string searchString)
		{
			foreach (RemoteProcessInfo procInfo in processes)
			{
				if (procInfo.Command.Contains (searchString))
					return procInfo;
			}
			
			return null;
		}
	}
}

