// InfoAddressRH.cs
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
using System.Globalization;
namespace Fuzzer.TargetConnectors.GDB
{
	public class InfoAddressRH : GDBResponseHandler
	{
		
		public delegate void SymbolResolvedDelegate(ISymbol symbol, UInt64? address);
		
		private ISymbol _symbol;
		private SymbolResolvedDelegate _resolvedCallback;
		
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		protected override string LogIdentifier 
		{
			get { return "RH_info address"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex r = new Regex("Symbol \"(?<symbol_name>\\S*)\"\\s*is\\s*a\\s*(?<type>\\S*)[\\s*\\S*]*0x(?<at>\\S*).", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rNoSymbol = new Regex("No\\s*symbol\\s*\"(?<symbol_name>\\S*)\"\\s*in\\s*current\\s*context.", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			for(int i = 0; i<responseLines.Length ; i++)
			{
				string line = responseLines[i];
				
				Match m = r.Match(line);
				
				if(m.Success)
				{
					_resolvedCallback(_symbol, UInt64.Parse(m.Result("${at}"), NumberStyles.HexNumber));
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}	
				
				m = rNoSymbol.Match(line);
				
				if(m.Success)
				{
					_resolvedCallback(_symbol, null);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
			}
			return GDBResponseHandler.HandleResponseEnum.Handled;			
		}

		
		#endregion
		public InfoAddressRH (ISymbol symbol, SymbolResolvedDelegate resolvedCallback)
		{
			_symbol = symbol;
			_resolvedCallback = resolvedCallback;
		}
	}
}

