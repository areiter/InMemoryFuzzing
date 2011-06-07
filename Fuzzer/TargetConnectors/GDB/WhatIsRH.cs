// WhatIsRH.cs
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
	public class WhatIsRH : GDBResponseHandler
	{
		
		public delegate void WhatIsCallbackDelegate(ISymbol symbol, string returnType, string[] parameterTypes);

		private WhatIsCallbackDelegate _cb;
		private ISymbol _symbol;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override string LogIdentifier 
		{
			get { return "RH_whatis"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex r = new Regex (@"type = (?<return_type>\S*) \((?<parameters>[\s*\S*]*)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			Regex rNoSymbol = new Regex (@"No symbol [\s*\S*]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
			
			if (rNoSymbol.Match (responseLines[0]).Success)
			{
				_cb (_symbol, null, null);
				return GDBResponseHandler.HandleResponseEnum.Handled;
			}
			
			Match m = r.Match (responseLines[0]);
			if (m.Success)
			{
				string returnType = m.Result ("${return_type}");
				string[] parameters = m.Result ("${parameters}").Split (',');
				
				List<string> realParameters = new List<string> ();
				
				foreach (string param in parameters)
				{
					string paramName = param.Trim ();
					
					if(!paramName.Equals(String.Empty))
						realParameters.Add (paramName);
				}

				_cb(_symbol, returnType, realParameters.ToArray());
				
				
				return GDBResponseHandler.HandleResponseEnum.Handled;
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;
		}
		
		#endregion
		public WhatIsRH (GDBSubProcess gdbProc, ISymbol symbol, WhatIsCallbackDelegate cb)
			:base(gdbProc)
		{
			_cb = cb;
			_symbol = symbol;
		}
	}
}

