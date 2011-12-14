// InfoStackRH.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
namespace Fuzzer.TargetConnectors.GDB
{
	public class InfoFrameRH : GDBResponseHandler
	{	
		public delegate void FrameInfoDelegate(IDictionary<string, IAddressSpecifier> savedRegisterAddresses);
		
		private FrameInfoDelegate _frameInfo;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			if (allowRequestLine)
				return GDBResponseHandler.HandleResponseEnum.RequestLine;
			
			Regex rSavedRegisterStart = new Regex (@"[\s*\S*]*Saved registers:(?<reg_definitions>[\s*\S*]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		
			//Current parsing status,
			//The response may also come in a single line (new behaviour)
			// 0 ... Waiting for next "key-line"
			// 1 ... Reading Saved register addressed (appear after "Saved registers:" line)
			int status = 0;
			Dictionary<string, IAddressSpecifier> savedRegisters = new Dictionary<string, IAddressSpecifier> ();
			
			foreach (string line in responseLines)
			{

				if (status == 0)
				{
					Match savedRegMatch = rSavedRegisterStart.Match (line);
					
					if(savedRegMatch.Success)
					{
						status = 1;
						string regDefinitions = savedRegMatch.Result("${reg_definitions}");
						
						if(regDefinitions != null && !regDefinitions.Equals(String.Empty))
						{
							ParseSavedRegisterLine(regDefinitions, savedRegisters);
						}
					}
					
				}
				else if (status == 1)
					ParseSavedRegisterLine (line, savedRegisters);
			}
			
			_frameInfo (savedRegisters);
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		
		}
		
		
		private void ParseSavedRegisterLine (string line, IDictionary<string, IAddressSpecifier> parsedRegisters)
		{
			string[] sLine = line.Split (',');
			
			Regex extractReg = new Regex (@"\s*(?<regname>\S*)\s*at\s*0x(?<regaddress>\S*)\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

			foreach (string linePart in sLine)
			{
				Match m = extractReg.Match (linePart);
				
				if (m.Success)
				{
					UInt64 address;
					if (UInt64.TryParse (m.Result ("${regaddress}"), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo,  out address))
					{
						if (parsedRegisters.ContainsKey (m.Result ("${regname}")))
							parsedRegisters[m.Result ("${regname}")] = new StaticAddress (address);
						else
							parsedRegisters.Add (m.Result ("${regname}"), new StaticAddress (address));
					}
				}
			}
			
			
		}
		
		
		public override string LogIdentifier 
		{
			get { return "RH_info"; }
		}
		
		#endregion
		public InfoFrameRH (GDBSubProcess gdbProc, FrameInfoDelegate frameInfo)
			: base(gdbProc)
		{
			_frameInfo = frameInfo;
		}
	}
}

