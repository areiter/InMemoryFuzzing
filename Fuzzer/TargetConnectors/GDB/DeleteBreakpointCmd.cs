// DeleteBreakpointCmd.cs
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
	public class DeleteBreakpointCmd : GDBCommand
	{
		/// <summary>
		/// Breakpoint id
		/// </summary>
		private int _breakpointNum;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		public override GDBResponseHandler ResponseHandler 
		{
			get { return null; }
		}
		
		
		public override string Command 
		{
			get { return string.Format("delete breakpoints {0}",_breakpointNum);} 
		}
		
		
		protected override string LogIdentifier 
		{
			get { return "CMD_delete breakpoints"; }
		}
		
		#endregion
		
		/// <summary>
		/// Constructs a new delete breakpoints command
		/// </summary>
		/// <param name="num">Number of the breakpoint to delete</param>
		public DeleteBreakpointCmd (int num, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_breakpointNum = num;
		}
	}
}

