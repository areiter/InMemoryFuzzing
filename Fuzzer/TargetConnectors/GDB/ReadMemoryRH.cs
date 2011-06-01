// ReadMemoryRH.cs
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
using System.Text.RegularExpressions;
using System.Globalization;
namespace Fuzzer.TargetConnectors.GDB
{
	public class ReadMemoryRH : GDBResponseHandler
	{
		public delegate void ReadMemoryDelegate(UInt64 readSize, byte[] buffer);
		
		private ReadMemoryDelegate _readMemory;
		private byte[] _buffer;
		private UInt64 _size;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			if(allowRequestLine)
				return GDBResponseHandler.HandleResponseEnum.RequestLine;
			
			Regex rError = new Regex(@"0x[\s*\S*]*:\s*Cannot access memory at address[\s*\S*]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rSuccess = new Regex(@"0x[\s*\S*]*:\s*(?<values>[\s*\S*]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			UInt64 readBytes = 0;
			foreach(string responseLine in responseLines)
			{
				if(readBytes >= (UInt64)_buffer.Length)
					break;
				
				if(rError.Match(responseLine).Success)
					break;
				
				Match m = rSuccess.Match(responseLine);
				
				if(m.Success)
				{
					string values = m.Result("${values}").Trim();
					string[] splittedValues = values.Split('\t');
					
					foreach(string v in splittedValues)
					{
						if(readBytes >= (UInt64)_buffer.Length)
							break;
				
						_buffer[readBytes] = Byte.Parse(v.Substring(2), NumberStyles.HexNumber);
						readBytes++;
					}
				}
			}
			
			_readMemory(readBytes, _buffer);
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}
		
		
		protected override string LogIdentifier 
		{
			get { return "RH_read memory"; }
		}
		
		#endregion
		public ReadMemoryRH (ReadMemoryDelegate readMemory, byte[] buffer, UInt64 size)
		{
			_readMemory = readMemory;
			_buffer = buffer;
			_size = size;
		}
	}
}

