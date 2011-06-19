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

