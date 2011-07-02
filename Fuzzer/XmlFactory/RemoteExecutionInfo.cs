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
	}
}

