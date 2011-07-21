// MemoryZonesAnalyzer.cs
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
using Iaik.Utils;
using Fuzzer.TargetConnectors.GDB.CoreDump;
using Iaik.Utils.CommonAttributes;
using System.Xml;
namespace Fuzzer.Analyzers
{
	[ClassIdentifier("analyzers/memory_zones")]
	public class MemoryZonesAnalyzer : BaseDataAnalyzer
	{
		private class MemoryZone
		{
			public enum ZoneType
			{
				WhiteZone,
				RedZone
			}
			
			private UInt64 _startAddress;
			
			public UInt64 StartAddress
			{
				get{ return _startAddress;}
			}
			
			private UInt64 _endAddress;
			
			public UInt64 EndAddress
			{
				get{ return _endAddress; }
			}
			
			private ZoneType _zone;
			
			public ZoneType Zone
			{
				get{ return _zone; }
			}
			
			public MemoryZone(ZoneType zone, UInt64 startAddress, UInt64 endAddress)
			{
				_zone = zone;
				_startAddress = startAddress;
				_endAddress = endAddress;
			}
			
			public MemoryZone(ZoneType zone, string zoneSpecification)
			{
				string[] addresses = zoneSpecification.Split(new Char[]{':'}, 2);
				
				if(addresses.Length != 2)
					throw new ArgumentException(string.Format("Invalid zone specification '{0}'", zoneSpecification));
				
				_startAddress = StringHelper.StringToUInt64(addresses[0]);
				_endAddress = StringHelper.StringToUInt64(addresses[1]);
				_zone = zone;
			}
			
			public bool ContainsAddress (UInt64 address)
			{
				return address >= StartAddress && address <= EndAddress;
			}
			
			public bool IntersectsWith (UInt64 address, UInt64 size)
			{
				UInt64 localEndAddress = address + size - 1;
				
				return (address <= StartAddress && localEndAddress >= StartAddress) ||
					   (address >= StartAddress && address <= EndAddress);
			}
			
		}
		
		private List<MemoryZone> _zones = new List<MemoryZone>();
		
		public MemoryZonesAnalyzer ()
		{
		}
		
		#region implemented abstract members of Fuzzer.Analyzers.BaseDataAnalyzer
		public override void Init (IDictionary<string, string> configValues, List<KeyValuePair<string, string>> values)
		{
			base.Init (configValues, values);
		
			foreach(KeyValuePair<string, string> val in values)
			{
				if(val.Key.Equals("WhiteZone", StringComparison.InvariantCultureIgnoreCase))
					_zones.Add(new MemoryZone(MemoryZone.ZoneType.WhiteZone, val.Value));
				else if(val.Key.Equals("RedZone", StringComparison.InvariantCultureIgnoreCase))
					_zones.Add(new MemoryZone(MemoryZone.ZoneType.RedZone, val.Value));
			}
			
		}
		
		public override string LogIdentifier 
		{
			get {return "MemoryZonesAnalyzer";}
		}
		
		
		public override void Analyze (AnalyzeController ctrl)
		{
			foreach (InstructionDescription insn in ctrl.ExecutedInstructions)
			{
				foreach (MemoryChange memChange in insn.MemoryChanges)
				{
					foreach (MemoryZone memZone in _zones)
					{
						if (memZone.IntersectsWith (memChange.Address, (ulong)memChange.Value.Length) &&
						   memZone.Zone == MemoryZonesAnalyzer.MemoryZone.ZoneType.RedZone)
						{
							Log (memZone, memChange.Address, memChange.Address + (ulong)memChange.Value.Length - 1, 
								(ulong)memChange.Value.Length, insn, ctrl);
							break;
						}
					}
				}
			}
		}
		
		private void Log (MemoryZone memZone, UInt64 memStart, UInt64 memEnd, UInt64 size, InstructionDescription insn, AnalyzeController ctrl)
		{
			XmlElement root = GenerateNode ("memory_zones");
			XmlHelper.WriteString (root, "ZoneType", memZone.Zone.ToString());
			XmlHelper.WriteString(root, "ZoneStart", string.Format("0x{0:X}", memZone.StartAddress));
			XmlHelper.WriteString(root, "ZoneEnd", string.Format("0x{0:X}", memZone.EndAddress));
			XmlHelper.WriteString (root, "MemStart", string.Format ("0x{0:X}", memStart));
			XmlHelper.WriteString (root, "MemEnd", string.Format ("0x{0:X}", memEnd));
			XmlHelper.WriteString (root, "At", FindProgramCounter(insn, ctrl.RegisterTypeResolver, ctrl.TargetRegisters).ToString());			
		}
		
		#endregion

	}
}

