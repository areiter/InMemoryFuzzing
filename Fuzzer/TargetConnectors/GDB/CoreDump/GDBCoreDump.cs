// GDBCoreDump.cs
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
using Iaik.Utils.libbfd;
using System.IO;
using Iaik.Utils;
using System.Collections.Generic;
using System.Collections;
namespace Fuzzer.TargetConnectors.GDB.CoreDump
{
	/// <summary>
	/// Represents a coredump with attached reverse debugging information
	/// </summary>
	public class GDBCoreDump : IDisposable
	{
		
		
		private string _coreDumpFile;
		private string _target;
		private Registers _targetRegisters;
		
		public GDBCoreDump (string coredDumpFile, string target, Registers targetRegisters)
		{
			_coreDumpFile = coredDumpFile;
			_targetRegisters = targetRegisters;
			
			if (target == null)
				_target = "elf64-x86-64";
			else
				_target = target;
		}
	

		public GDBProcessRecordSection GetProcessRecordSection ()
		{
			using (BfdStream stream = Open ())
			{
				stream.SelectSection ("null0");
				return new GDBProcessRecordSection (stream, _targetRegisters);
			}
		}
		
		private BfdStream Open ()
		{
			return BfdStream.CreateFromCoreFile (_coreDumpFile, FileAccess.Read, _target);
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
		}
		#endregion
	}
	
	public abstract class GDBCoreDumpSection
	{
		public abstract void Read(Stream src);
	}
	
	
	/// <summary>
	/// Reads the Process record section and rebuilts the memory and register changes for each command
	/// </summary>
	public class GDBProcessRecordSection : GDBCoreDumpSection, IEnumerable<InstructionDescription>
	{
		public enum RecordType : byte
		{
			RecordEnd = 0,
			RecordReg = 1,
			RecordMem = 2
		}
		
		/// <summary>
		/// Magic number of the Record alias 'null0' bfd section
		/// </summary>
		private const int RECORD_FILE_MAGIC = 0x20091016; 
		
		
		/// <summary>
		/// All available registers with its num and size on the target platform.
		/// Gets retrieved from the connector on creation
		/// </summary>
		private Registers _registers;
		
		/// <summary>
		/// Once read, it contains all the instruction descriptions and its memory and register changes recorded
		/// in the process record
		/// </summary>
		private List<InstructionDescription> _recordedInstructions = null;
		
		public GDBProcessRecordSection (Stream stream, Registers registers)
		{
			_registers = registers;
			Read (stream);
		}
		
		public override void Read (Stream src)
		{
			byte[] magicNumber = new byte[4];
			if (src.Read (magicNumber, 0, 4) != 4)
				ThrowEndOfStreamException ();
			
			//Record files are always saved in net-endian-order = bigendian = !littleendian ;->
			int myMagicNumber = (int)ByteHelper.ByteArrayToUInt64 (magicNumber, 0, 4, false);

			if (myMagicNumber != RECORD_FILE_MAGIC)
				throw new ArgumentException ("Magic numbers do not match");
			
			
			List<InstructionDescription> instructions = new List<InstructionDescription> ();
			
			// The changes for this instruction are currently processed
			InstructionDescription pendingInstruction = new InstructionDescription ();
			
			byte[] buffer = new byte[100];
			// The Stream contains multiple mem and reg entries and a final end entry to set the end of a single instruction
			// then again multiple mem and reg entries with an end entry will follow.
			while (src.Position != src.Length)
			{
				int recType = src.ReadByte ();
				
				//EOF
				if (recType < 0)
					break;
				
				if (pendingInstruction == null)
					pendingInstruction = new InstructionDescription ();
				
				switch ((RecordType)recType)
				{
				
				case RecordType.RecordEnd:
					//The current instruction ends.
					ReadWithException (src, buffer, 0, 8);
					pendingInstruction.Signal = (int)ByteHelper.ByteArrayToUInt64 (buffer, 0, 4, false);
					pendingInstruction.InstructionCount = (int)ByteHelper.ByteArrayToUInt64 (buffer, 4, 4, false);
					instructions.Add (pendingInstruction);
					pendingInstruction = null;
					break;
				
				case RecordType.RecordMem:
					MemoryChange memChange = new MemoryChange ();
					ReadWithException (src, buffer, 0, 4);
					UInt32 dataLen = (UInt32)ByteHelper.ByteArrayToUInt64 (buffer, 0, 4, false);
					
					ReadWithException (src, buffer, 0, 8);
					memChange.Address = ByteHelper.ByteArrayToUInt64 (buffer, 0, 8, false);
					
					memChange.Value = new byte[dataLen];
					ReadWithException (src, memChange.Value, 0, dataLen);
					
					pendingInstruction.AddMemChange (memChange);
					break;
				
				case RecordType.RecordReg:
					RegisterChange regChange = new RegisterChange ();
					
					ReadWithException (src, buffer, 0, 4);
					regChange.Regnum = (UInt32)ByteHelper.ByteArrayToUInt64 (buffer, 0, 4, false);
					
					Register currentReg = _registers.FindRegisterByNum (regChange.Regnum);
					
					if (currentReg == null)
						throw new ArgumentException (string.Format ("CoreDump contains informations about not available register with number '{0}', cannot continue because size is unknown", 
								regChange.Regnum));
					
					regChange.Value = new byte[currentReg.Size];
					ReadWithException (src, regChange.Value, 0, currentReg.Size);
					
					pendingInstruction.AddRegChange (regChange);
					break;
				}
			}
			
			// If there is still a pending instruction, the end record is missing
			if (pendingInstruction != null)
				instructions.Add (pendingInstruction);

			_recordedInstructions = instructions;
		}
		
		private void ThrowEndOfStreamException ()
		{
			throw new ArgumentException ("Unexpected end of stream");
		}
		
		private void ReadWithException (Stream s, byte[] buffer, int offset, uint length)
		{
			if (s.Read (buffer, offset, (int)length) != length)
				ThrowEndOfStreamException ();
		}
	

		#region IEnumerable[InstructionDescription] implementation
		public IEnumerator<InstructionDescription> GetEnumerator ()
		{
			return _recordedInstructions.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return _recordedInstructions.GetEnumerator ();
		}
		#endregion
}
}

