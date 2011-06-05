// InfoAddressCmd.cs
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
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Looks for all available function symbols (debugging symbols or linker symbols)
	/// </summary>
	public class InfoAddressCmd : GDBCommand
	{

		private InfoAddressRH _rh;
		private ISymbol _symbol;
		
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		public override string Command 
		{
			get { return "info address " + _symbol.Symbol; }
		}
		
		protected override string LogIdentifier 
		{
			get { return "info address"; }
		}
		
		#endregion
		public InfoAddressCmd (ISymbol symbol, InfoAddressRH.SymbolResolvedDelegate resolvedCallback, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_symbol = symbol;
			_rh = new InfoAddressRH(symbol, resolvedCallback, _gdbProc);
		}
	}
}

