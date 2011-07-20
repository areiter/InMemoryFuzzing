// SavedRegisterAnalyzer.cs
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
using System.IO;
using Iaik.Utils;
using Fuzzer.TargetConnectors;
using Fuzzer.TargetConnectors.GDB.CoreDump;
using System.Xml;
using Fuzzer.TargetConnectors.RegisterTypes;
using Iaik.Utils.CommonAttributes;

namespace Fuzzer.Analyzers
{
	/// <summary>
	/// Checks if stack saved registers of the caller gets overwritten by the executed code
	/// </summary>
	/// <remarks>
	/// <para>Uses the files: *.stackframeinfo</para>
	/// </remarks>
	[ClassIdentifier("analyzers/saved_registers")]
	public class SavedRegisterAnalyzer : BaseDataAnalyzer
	{
		public SavedRegisterAnalyzer ()
		{
		}
		
		#region implemented abstract members of Fuzzer.Analyzers.BaseDataAnalyzer
		public override string LogIdentifier 
		{
			get { return "SavedRegisterAnalyzer"; }
		}


		public override void Analyze (AnalyzeController ctrl)
		{

			FileInfo fileStackFrameInfo = GenerateFile ("stackframeinfo");
			
			if (fileStackFrameInfo.Exists == false)
			{
				_log.WarnFormat ("[prefix={0}] Missing stackframeinfo file", _prefix);
				return;
			}
			
			IStackFrameInfo stackFrameInfo;
			using (FileStream src = fileStackFrameInfo.OpenRead ())
				stackFrameInfo = StreamHelper.ReadTypedStreamSerializable<IStackFrameInfo> (src);

			foreach(String reg in stackFrameInfo.SavedRegisters)
				_log.DebugFormat("Saved Register '{0} at 0x{1:X})'", reg, stackFrameInfo.GetSavedRegisterAddress(reg).ResolveAddress());
			
			foreach (InstructionDescription insn in ctrl.ExecutedInstructions)
			{
				foreach (MemoryChange memChange in insn.MemoryChanges)
				{
					SavedRegistersInRange (stackFrameInfo, memChange.Address, memChange.Value.Length, insn, ctrl);
				}
			}
					
		}
		
		
		
		private void SavedRegistersInRange (IStackFrameInfo stackFrameInfo, UInt64 address, int size, InstructionDescription insn, AnalyzeController ctrl)
		{
			_log.DebugFormat ("address=0x{0:X} size={1}", address, size);
			foreach (string savedRegisterName in stackFrameInfo.SavedRegisters)
			{
				IAddressSpecifier savedRegisterAddressSpecifier = stackFrameInfo.GetSavedRegisterAddress (savedRegisterName);
				UInt64? savedRegisterAddress = savedRegisterAddressSpecifier.ResolveAddress ();
				
				if (savedRegisterAddress != null)
				{
					UInt64 regSize = ctrl.TargetRegisters.FindRegisterByName(savedRegisterName).Size;
					
					if((savedRegisterAddress.Value + regSize > address) &&
					   (savedRegisterAddress.Value + regSize < address + (UInt64)size ||
					   (savedRegisterAddress.Value  < address + (UInt64)size && savedRegisterAddress.Value + regSize >= address + (UInt64)size)))
						LogSavedRegister (savedRegisterName, savedRegisterAddress.Value, address, size, insn, ctrl);
				}
			}
		}
		
		private void LogSavedRegister (string registerName, UInt64 registerAddress, UInt64 targetAddress, int size, InstructionDescription insn, AnalyzeController ctrl)
		{
			XmlElement root = GenerateNode ("saved_register");
			XmlHelper.WriteString (root, "Reg", registerName);
			XmlHelper.WriteString (root, "RegAddr", string.Format ("0x{0:X}", registerAddress));
			
			UInt64? pc = FindProgramCounter (insn, ctrl.RegisterTypeResolver, ctrl.TargetRegisters);
			
			if (pc != null)
				XmlHelper.WriteString (root, "At", string.Format ("0x{0:X}", pc.Value));
			else
				XmlHelper.WriteString (root, "At", "[unspecified]");
			
			XmlHelper.WriteString (root, "TargetAddr", string.Format ("0x{0:X}", targetAddress));
			XmlHelper.WriteInt (root, "TargetSize", size);
		}
		
		#endregion
	}
}

