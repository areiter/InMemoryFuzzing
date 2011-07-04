// InfoLineRH.cs
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
	public class InfoLineRH : GDBResponseHandler
	{
		public delegate void AddressResolvedCB(string lineArg, IAddressSpecifier address);
		
		private AddressResolvedCB _addressResolved;
		private string _lineArg;
		
		public InfoLineRH (GDBSubProcess gdbProc, string lineArg, AddressResolvedCB addressResolved)
			: base(gdbProc)
		{
			_addressResolved = addressResolved;
			_lineArg = lineArg;
		}
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex rOutOfRange = new Regex (@"\s*Line number \S* is out of range[\s*\S*]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			Regex rAddress = new Regex (@"\s*Line \S* of [\s*\S*]* \S* at address 0x(?<address>\S*) [\s*\S*]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

			
			foreach (string line in responseLines)
			{
				if (rOutOfRange.Match (line).Success)
				{
					_addressResolved (_lineArg, null);
					continue;
				}
				
				Match m = rAddress.Match (line);
				
				if (m.Success)
				{
					UInt64 address;
					if (UInt64.TryParse (m.Result ("${address}"), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address))
						_addressResolved (_lineArg, new StaticAddress (address));
					else
						_addressResolved (_lineArg, null);
				}
			
			}
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}


		public override string LogIdentifier 
		{
			get { return "RH_info line"; }
		}
		
		#endregion
	}
}

