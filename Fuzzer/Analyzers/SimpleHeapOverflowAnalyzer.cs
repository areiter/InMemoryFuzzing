// SimpleHeapOverflowAnalyzer.cs
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
using Iaik.Utils.CommonAttributes;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
namespace Fuzzer.Analyzers
{
	/// <summary>
	/// Detects simple heap overflows e.g.:
	/// 
	/// Memory is allocated for 300 bytes. But program may also write to 301, 302,... without segmentation fault or similar because
	/// malloc allocates a whole page.
	/// </summary>
	/// <remarks>
	/// Uses the files: *.pipes
	/// 
	/// This analyzer builts a "map" of allocated memory and checks the bounds for each memory write.
	/// If a program allocates 2 times 300 bytes and writes to the 301 byte of the first allocation, the error
	/// may not be detected if the memory chunks are allocated without a gap.
	/// 
	/// Currently only allocated memory is taken into account. Allocated and then freed
	/// is treated as allocated, so this analyzer might miss some errors
	/// </remarks>
	[ClassIdentifier("analyzers/simple_heap_overflow")]
	public class SimpleHeapOverflowAnalyzer : BaseDataAnalyzer
	{
		/// <summary>
		/// Contains informtions about an allocated memory range
		/// </summary>
		private class RangeInfo
		{
			public IList<UInt64> Backtrace;
			public UInt64 StartAddress;			
			public UInt64 Size;
			
			public UInt64 EndAddress
			{
				get{ return StartAddress + Size - 1; }
			}
				
			public RangeInfo(UInt64 startAddress, UInt64 size, IList<UInt64> backtrace)
			{
				StartAddress = startAddress;
				Size = size;
				Backtrace = backtrace;
			}
			
			public bool ContainsAddress(UInt64 address)
			{
				return address >= StartAddress && address <= EndAddress;
			}
		}
		
		public delegate void LineHandlerDelegate(string lineId, int lineIdIndex, string currentLine);
		
		private Dictionary <string, LineHandlerDelegate> _lineHandlers = new Dictionary<string, LineHandlerDelegate>();
		
		private List<RangeInfo> _rangeInfos = new List<RangeInfo>();
		public SimpleHeapOverflowAnalyzer ()
		{
			_lineHandlers.Add("malloc", LH_malloc);
			_lineHandlers.Add("realloc", LH_malloc);
			_lineHandlers.Add("calloc", LH_calloc);
		}
		
		#region implemented abstract members of Fuzzer.Analyzers.BaseDataAnalyzer
		public override string LogIdentifier 
		{
			get { return "SimpleHeapOverflowAnalyzer";}
		}


		public override void Analyze (AnalyzeController ctrl)
		{
			FileInfo pipeFile = GenerateFile("pipes");		
			
			if(pipeFile.Exists == false)
			{
				_log.WarnFormat("Missing '{0}'", pipeFile.FullName);
				return;
			}
			_rangeInfos.Clear();
			
			using(StreamReader pipeReader = new StreamReader(pipeFile.OpenRead()))
			{
				while(!pipeReader.EndOfStream)
				{
					string currentLine = pipeReader.ReadLine();
					
					//Extract line identifier (<id>: ...)
					int lineIdSeperatorIndex = currentLine.IndexOf(':');
					
					if(lineIdSeperatorIndex < 0)
					{
						_log.WarnFormat("Invalid line detected '{0}'", currentLine);
						continue;
					}
					
					string lineId = currentLine.Substring(0, lineIdSeperatorIndex).Trim().ToLower();
					
					if(_lineHandlers.ContainsKey(lineId))
						_lineHandlers[lineId](lineId, lineIdSeperatorIndex, currentLine);
					else
						_log.WarnFormat("Invalid line id detected '{0}'", lineId);
				}
			}
			
		}
		
		#endregion

		#region Helpers
		private IDictionary<string, string> ExtractArguments(string currentLine)
		{
			int argIndex = currentLine.IndexOf("args=[");
			
			if(argIndex < 0)
				return new Dictionary<string, string>();
			
			int argStart = argIndex + "args=[".Length;
			int argEnd = currentLine.IndexOf(']', argStart);
			
			IDictionary<string, string> arguments = new Dictionary<string, string>();
			
			foreach(string argKeyVal in currentLine.Substring(argStart, argEnd - argStart).Split(' '))
			{
				string[] keyVal = argKeyVal.Split(new char[]{'='}, 2);
					if(keyVal.Length == 2 && arguments.ContainsKey(keyVal[0]))
						arguments[keyVal[0]] = keyVal[1];
					else if(keyVal.Length == 2)
						arguments.Add(keyVal[0], keyVal[1]);
			}
			
			return arguments;
			
		}
		
		private IList<UInt64> ExtractBacktrace(string currentLine)
		{
			int btIndex = currentLine.IndexOf("bt=[");
			
			if(btIndex < 0)
				return new List<UInt64>();
			
			int btStart = btIndex + "bt=[".Length;
			int btEnd = currentLine.IndexOf(']', btStart);
			
			List<UInt64> bt = new List<UInt64>();
			foreach(string hexAddress in currentLine.Substring(btStart, btEnd - btStart).Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries))
			{
				if(!hexAddress.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
					_log.WarnFormat("Invalid address detected '{0}'", hexAddress);
				else
				{
					UInt64 address;
					if(!UInt64.TryParse(hexAddress.Substring(2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address))
						_log.WarnFormat("Could not parse hex number '{0}'", hexAddress);
					else
						bt.Add(address);
				}
			}
			
			return bt;
		}
		
		private UInt64? ParseNumber(string name, IDictionary<string, string> arguments)
		{
			if(arguments.ContainsKey(name) == false)
				return null;
			
			UInt64 number;
			
			if(arguments[name].StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
			{
				if(!UInt64.TryParse(arguments[name].Substring(2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out number))
					return null;				
			}
			else if(!UInt64.TryParse(arguments[name], out number))
				return null;
				
			return number;
			
		}
		#endregion
		
		#region Line Handlers
		private void LH_malloc(string lineId, int lineIndex, string currentLine)
		{
			IList<UInt64> bt = ExtractBacktrace(currentLine);
			IDictionary<string, string> arguments = ExtractArguments(currentLine);
			
			UInt64? address = ParseNumber("return", arguments);
			
			if(address == null)
			{
				_log.WarnFormat("[malloc] Could not find 'return' argument for line '{0}'", currentLine);
				return;
			}

			UInt64? size = ParseNumber("size", arguments);
			
			if(size == null)
			{
				_log.WarnFormat("[malloc] Could not find 'size' argument for line '{0}", currentLine);
				return;
			}
			
			_rangeInfos.Add(new RangeInfo(address.Value, size.Value, bt));
		}
		
		private void LH_calloc(string lineId, int lineIndex, string currentLine)
		{
			IList<UInt64> bt = ExtractBacktrace(currentLine);
			IDictionary<string, string> arguments = ExtractArguments(currentLine);
			
			UInt64? address = ParseNumber("return", arguments);
			
			if(address == null)
			{
				_log.WarnFormat("[calloc] Could not find 'return' argument for line '{0}'", currentLine);
				return;
			}

			UInt64? size = ParseNumber("size", arguments);
			
			if(size == null)
			{
				_log.WarnFormat("[calloc] Could not find 'size' argument for line '{0}", currentLine);
				return;
			}
			
			UInt64? num = ParseNumber("num", arguments);
			
			if(num == null)
			{
				_log.WarnFormat("[calloc] Could not find 'num' argument for line '{0}", currentLine);
				return;
			}
			
			_rangeInfos.Add(new RangeInfo(address.Value, size.Value * num.Value, bt));
		}
		#endregion
	}
}

