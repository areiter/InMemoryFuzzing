// SetBreakpointCmd.cs
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
	public class SetBreakpointCmd : GDBCommand
	{
		/// <summary>
		/// Break address
		/// </summary>
		private UInt64 _address;
		
		private GDBResponseHandler _rh;
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}
		
		
		public override string Command 
		{
			get { return string.Format("break *0x{0:X}", _address);} 
		}
		
		
		protected override string LogIdentifier 
		{
			get { return "CMD_break"; }
		}
		
		#endregion
		
		/// <summary>
		/// Constructs a new break command
		/// </summary>
		/// <param name="address">Address to set a breakpoint at. Use Symbol Table to translate named symbols to addresses</param>
		public SetBreakpointCmd (UInt64 address, SetBreakpointRH.SetBreakpointDelegate rhCb, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_address = address;
			
			_rh = new SetBreakpointRH(rhCb, _gdbProc);
		}
	}
}

