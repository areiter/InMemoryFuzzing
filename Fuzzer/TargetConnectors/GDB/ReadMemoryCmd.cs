// ReadMemoryCmd.cs
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
	/// Reads raw memory starting at the specified position
	/// </summary>
	public class ReadMemoryCmd:GDBCommand
	{

		private UInt64 _address;
		private UInt64 _size;
		private byte[] _buffer;
		private ReadMemoryRH _rh;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}		
		
		public override string Command 
		{
			get{ return string.Format("x/{0}bx 0x{1}", _size, _address.ToString("X")); }				
		}
		
		
		protected override string LogIdentifier 
		{
			get { return "CMD_read memory"; }
		}
		
		#endregion
		public ReadMemoryCmd (UInt64 address, UInt64 size, byte[] buffer, ReadMemoryRH.ReadMemoryDelegate readMemory, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			if((UInt64)buffer.Length < size)
				throw new ArgumentException("Buffer too small");
			
			_address = address;
			_size = size;
			_buffer = buffer;
			
			_rh = new ReadMemoryRH(readMemory, _buffer, _size, _gdbProc);
			                       
		}
	}
}

