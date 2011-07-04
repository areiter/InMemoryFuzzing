// InstructionDescription.cs
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
using System.Text;
using Iaik.Utils;
namespace Fuzzer.TargetConnectors.GDB.CoreDump
{
	/// <summary>
	/// Contains all register and memory changes of a single instruction (including its program counter)
	/// </summary>
	/// <remarks>
	/// Because of the stream layout, the stream deserialization occurs in the section deserializer
	/// </remarks>
	public class InstructionDescription
	{
		private int _signal;
		
		public int Signal
		{
			get { return _signal; }
			set { _signal = value;}
		}
		
		private int _instructionCount;
		
		public int InstructionCount
		{
			get { return _instructionCount; }
			set { _instructionCount = value; }
		}
		
		private List<RegisterChange> _registerChanges = new List<RegisterChange>();
		private List<MemoryChange> _memoryChanges = new List<MemoryChange>();
		
		public RegisterChange[] RegisterChanges
		{
			get { return _registerChanges.ToArray (); }
		}
		
		public MemoryChange[] MemoryChanges
		{
			get { return _memoryChanges.ToArray ();}
		}
		
		public void AddRegChange (RegisterChange regChange)
		{
			_registerChanges.Add (regChange);
		}
		
		public void AddMemChange (MemoryChange memChange)
		{
			_memoryChanges.Add (memChange);
		}
		
		public InstructionDescription ()
		{
		}
		
		public string PrettyPrint (Registers registers)
		{
			StringBuilder str = new StringBuilder ();
			
			str.AppendFormat ("#{0} insn, signal {1}\n", InstructionCount, Signal);
			str.Append ("{\n");
			str.AppendFormat ("  Register changes:\n");
			
			foreach (RegisterChange regChange in RegisterChanges) {
				str.AppendFormat ("    ${0} (#{1}): 0x{2:X} ({3})\n", registers.FindRegisterByNum (regChange.Regnum).Name, regChange.Regnum, 
					ByteHelper.ByteArrayToUInt64 (regChange.Value, 0, (int)registers.FindRegisterByNum (regChange.Regnum).Size), 
					ByteHelper.ByteArrayToHexString (regChange.Value));
			}
			
			str.AppendFormat ("  Memory changes:\n");
			foreach (MemoryChange memchange in MemoryChanges) {
				str.AppendFormat ("    0x{0:X}: {1}\n", memchange.Address, ByteHelper.ByteArrayToHexString (memchange.Value));
			}
			str.Append ("}\n");
			
			
			return str.ToString ();
		}
	}
	
	public class MemoryChange
	{
		private UInt64 _address;
		
		public UInt64 Address
		{
			get { return _address; }
			set { _address = value; }
		}
		
		private byte[] _value;
		
		public byte[] Value
		{
			get { return _value; }
			set { _value = value; }
		}
			
	}
	
	public class RegisterChange
	{
		private UInt32 _regnum;
		
		public UInt32 Regnum
		{
			get { return _regnum; }
			set { _regnum = value; }
		}
		
		private byte[] _value;
		
		public byte[] Value
		{
			get { return _value; }
			set { _value = value; }
		}
			
	}
}

