// MaintArchitectureRH.cs
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
using System.Text.RegularExpressions;
namespace Fuzzer.TargetConnectors.GDB
{
	public class MaintArchitectureRH : GDBResponseHandler
	{
		public delegate void DiscoveredVariablesDelegate(IDictionary<string, string> discoveredVariables);
		
		private DiscoveredVariablesDelegate _cb;
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			if (allowRequestLine)
				return GDBResponseHandler.HandleResponseEnum.RequestLine;
			
			Regex r = new Regex (@"\S*:\s*(?<varname>\S*)\s*=\s*(?<value>\S*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			
			Dictionary<string, string> discoveredVariables = new Dictionary<string, string> ();
			
			foreach (string line in responseLines)
			{
				Match m = r.Match (line);
				
				if (m.Success)
				{
					string varName = m.Result ("${varname}");
					string value = m.Result ("${value}");
					
					if (discoveredVariables.ContainsKey (varName))
						discoveredVariables[varName] = value;
					else
						discoveredVariables.Add (varName, value);
				}
			}
			
			_cb (discoveredVariables);
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}
		
		
		public override string LogIdentifier 
		{
			get { return "RH_maint print architecture"; }
		}
		
		#endregion
		public MaintArchitectureRH (GDBSubProcess gdbProc, DiscoveredVariablesDelegate discoveredVariablesCB)
			:base(gdbProc)
		{
			_cb = discoveredVariablesCB;
		}
	}
}

