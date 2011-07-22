// ExamineCmd.cs
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
	public class ExamineCmd : GDBCommand
	{
		private ISymbol _symbol;
		private ExamineRH _rh;
		
		public ExamineCmd (GDBSubProcess proc, ISymbol symbol, InfoAddressRH.SymbolResolvedDelegate symbolResolved)
			:base(proc)
		{
			_symbol = symbol;
			_rh = new ExamineRH(proc, symbol, symbolResolved);
		}
		
    	#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		
		protected override string LogIdentifier 
		{
			get { return "CMD_x"; }
		}
		
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}
		
		public override string Command 
		{
			get { return string.Format("x {0}", _symbol.Symbol); }
		}
		
		#endregion

	}
}

