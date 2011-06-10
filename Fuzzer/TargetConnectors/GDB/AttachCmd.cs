// AttachCmd.cs
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
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Attaches GDB to another process
	/// </summary>
	public class AttachCmd : GDBCommand
	{
		private int _pid;
		private AttachRH _rh;
	
		public AttachCmd (GDBSubProcess gdbProc, int pid, Action<string> cb)
			: base(gdbProc)
		{
			_pid = pid;
			_rh = new AttachRH (_gdbProc, cb);
		}
		
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		public override string Command 
		{
			get { return string.Format ("attach {0}", _pid); }
		}
		
		
		protected override string LogIdentifier 
		{
			get { return "CMD_attach"; }
		}
		
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}
		#endregion
	}
}

