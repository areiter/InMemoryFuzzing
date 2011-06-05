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
		
		public delegate void SymbolResolvedDelegate(ISymbol symbol, IAddressSpecifier address);
		
		private ISymbol _symbol;
		private SymbolResolvedDelegate _resolvedCallback;
		
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override string LogIdentifier 
		{
			get { return "RH_info address"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex rStaticSymbol = new Regex("Symbol \"(?<symbol_name>\\S*)\"\\s*is\\s*(?<type>[\\S*\\s*]*) at address 0x(?<at>\\S*).", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rVariableSymbol = new Regex("Symbol \"(?<symbol_name>\\S*)\"\\s*is\\s*(?<type>[\\S*\\s*]*) at (?<reg_desc>[\\s*\\S*]*) $(?<reg_name>\\S*) offset (?<offset>[\\S*\\s*]*).", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rNoSymbol = new Regex("No\\s*symbol\\s*\"(?<symbol_name>\\S*)\"\\s*in\\s*current\\s*context.", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			for(int i = 0; i<responseLines.Length ; i++)
			{
				string line = responseLines[i];
				
				Match mStatic = rStaticSymbol.Match(line);
				if(mStatic.Success)
				{
					_resolvedCallback(_symbol, new StaticAddress(UInt64.Parse(mStatic.Result("${at}"), NumberStyles.HexNumber)));
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}	
				
				Match mVariable = rVariableSymbol.Match(line);
				if(mVariable.Success)
				{
					_resolvedCallback(_symbol,
					                  new GDBRegisterBasedAddressSpecifier(mVariable.Result("${reg_name}"),
					                                                       mVariable.Result("${offset}"),
					                                                       _gdbProc));
					return GDBResponseHandler.HandleResponseEnum.Handled;						
					                                                       
				}
				
				Match mNoSymbol = rNoSymbol.Match(line);
				
				if(mNoSymbol.Success)
				{
					_resolvedCallback(_symbol, null);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
			}
			return GDBResponseHandler.HandleResponseEnum.Handled;			
		}

		
		#endregion
		public InfoAddressRH (ISymbol symbol, SymbolResolvedDelegate resolvedCallback, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_symbol = symbol;
			_resolvedCallback = resolvedCallback;
		}
	}
}

