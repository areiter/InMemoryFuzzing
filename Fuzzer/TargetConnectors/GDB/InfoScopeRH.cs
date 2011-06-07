// InfoScopeRH.cs
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
using System.Collections.Generic;
namespace Fuzzer.TargetConnectors.GDB
{
	public class InfoScopeRH : GDBResponseHandler
	{
		public delegate void InfoScopeResponseDelegate(ISymbol[] discoveredVariables);
		
		public InfoScopeResponseDelegate _cb;
		
		public InfoScopeRH (GDBSubProcess gdbProc, InfoScopeResponseDelegate cb)
			:base(gdbProc)
		{
			_cb = cb;
		}

		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override string LogIdentifier 
		{
			get { return "RH_info scope"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			if (allowRequestLine)
				return GDBResponseHandler.HandleResponseEnum.RequestLine;
			
			Regex rNoLocals = new Regex (@"Scope for (?<symbol>\S*) contains no locals or arguments.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			Regex rUndefinedSymbol = new Regex (@"[\s*\S*]* not defined.", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			Regex rHeader = new Regex (@"Scope for (?<symbol>\S*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			Regex rVar = new Regex (@"Symbol (?<symbol>\S*) [\s*\S*]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			
			List<ISymbol> discoveredSymbols = new List<ISymbol>();
			foreach(string line in responseLines)
			{
				Match m = rNoLocals.Match(line);
				if(m.Success)
					break;

				m = rUndefinedSymbol.Match(line);
				if(m.Success)
					break;					
					
			    m = rHeader.Match(line);
				
				if(m.Success)
				{
					continue;
				}
				
				m = rVar.Match(line);
				if(m.Success)
				{
					discoveredSymbols.Add(new SimpleSymbol(m.Result("${symbol}")));
				}					
			}
			
			_cb(discoveredSymbols.ToArray());
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}
		
		#endregion

	}
}

