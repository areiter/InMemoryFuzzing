// ExamineRH.cs
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
	public class ExamineRH : GDBResponseHandler
	{
		private ISymbol _s;
		private InfoAddressRH.SymbolResolvedDelegate _symbolResolved;
		
		public ExamineRH (GDBSubProcess proc, ISymbol s, InfoAddressRH.SymbolResolvedDelegate symbolResolved)
			:base(proc)
		{
			_s = s;
			_symbolResolved = symbolResolved;
		}
	
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex r = new Regex(@"0x(?<address>\S*):[\s*\S*]*");
			
			Match m = r.Match(responseLines[0]);
			
			if(m.Success)
				_symbolResolved(_s, new StaticAddress(UInt64.Parse(m.Result("${address}"), NumberStyles.HexNumber)));
			else
				_symbolResolved(_s, null);
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}
		
		
		public override string LogIdentifier 
		{
			get { return "RH_x"; }
		}
		
		#endregion
	}
}

