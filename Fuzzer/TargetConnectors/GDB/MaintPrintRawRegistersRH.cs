// MaintPrintRawRegistersRH.cs
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
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Handles a register dump response
	/// </summary>
	public class MaintPrintRawRegistersRH : GDBResponseHandler
	{
		public delegate void RegisterDiscoveredDelegate(string name, uint num, uint size);
		
		private RegisterDiscoveredDelegate _registerDiscovery;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			if (allowRequestLine)
				return GDBResponseHandler.HandleResponseEnum.RequestLine;
			
			Regex rHeader = new Regex (@"Name\s*Nr\s*Rel\s*Offset\s*Size\s*Type\s*\Raw value\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			Regex rRegisterLine = new Regex (@"\s*(?<regname>\S*)\s*(?<regnum>\S*)\s*(?<regrel>\S*)\s*(?<regoffset>\S*)\s*(?<regsize>\S*)\s*(?<regtype>\S*)\s*(?<regvalue>\S*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			
			foreach (string line in responseLines)
			{
				Match m = rHeader.Match (line);
				
				if (m.Success)
					continue;
				
				m = rRegisterLine.Match (line);
				
				if (m.Success)
				{
					uint regnum;
					uint regsize;
					if (uint.TryParse (m.Result ("${regnum}"), out regnum) == false ||
						uint.TryParse (m.Result ("${regsize}"), out regsize) == false)
						continue;
					
					
					
					_registerDiscovery (m.Result ("${regname}"), regnum, regsize);
				}
			}
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}
		
		
		public override string LogIdentifier 
		{
			get { return "RH_maint print raw-registers"; }
		}
		
		#endregion
		public MaintPrintRawRegistersRH (GDBSubProcess gdbProc, RegisterDiscoveredDelegate registerDiscovery)
			: base(gdbProc)
		{
			_registerDiscovery = registerDiscovery;
		}
	}
}

