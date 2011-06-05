// SignalRH.cs
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
	/// <summary>
	/// Handles Program exits.
	/// </summary>
	public class SignalRH : GDBResponseHandler
	{
		/// <summary>
		/// Is called if gdb receives a break
		/// </summary>
		private GDBConnector.GdbStopDelegate _gdbStopped;
		
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override string LogIdentifier 
		{
			get { return "RH_signal"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess connector, string[] responseLines, bool allowRequestLine)
		{
			Regex r = new Regex(@"Program received signal (?<signal_name>\S*),\s*(?<friendly_signal_name>[\S*\s*]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rAddress = new Regex(@"0x(?<at>\S*)\s*in[\S*\s*]*");
			for(int i = 0; i<responseLines.Length ; i++)
			{
				string line = responseLines[i];
				
				Match match = r.Match(line);

				if(match.Success)
				{
					UInt64? address = null;
					
					//Request another line if the address is not included yet
					if(i + 1 == responseLines.Length && allowRequestLine)
						return GDBResponseHandler.HandleResponseEnum.RequestLine;
					
					//Iterate through all other response lines and look for an address line
					for(int addressLineNum = i+1 ; addressLineNum<responseLines.Length;addressLineNum++)
					{
						Match addressMatch = rAddress.Match(responseLines[addressLineNum]);
						
						if(addressMatch.Success)
						{
							address = UInt64.Parse(addressMatch.Result("${at}"), NumberStyles.HexNumber);
							break;
						}
					}
					
					//If no address line is present, try to request another line,
					//otherwise give up...no address is available
					if(address == null && allowRequestLine)
						return GDBResponseHandler.HandleResponseEnum.RequestLine;
					
					
					string signal = match.Result("${signal_name}");
					object oSignal = Enum.Parse(typeof(SignalEnum), signal, true);
					
					SignalEnum eSignal = SignalEnum.UNKNOWN;
					
					if(oSignal != null)
						eSignal = (SignalEnum)oSignal;
					
					_gdbStopped(StopReasonEnum.Terminated, null, address == null?0:address.Value, (int)eSignal);					
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;			
		}
		
		#endregion
		public SignalRH (GDBConnector.GdbStopDelegate gdbStopped, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_gdbStopped = gdbStopped;
		}
	}
}
