// InfoScopeCmd.cs
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
	/// Lists the variables valid in a specified scope.
	/// If a method has parameters and local variables, the parameters are always listed first
	/// </summary>
	public class InfoScopeCmd : GDBCommand
	{
		private string _scope;
		private InfoScopeRH _rh;
		
		public InfoScopeCmd (GDBSubProcess gdbProc, string scope, InfoScopeRH.InfoScopeResponseDelegate callback)
			:base(gdbProc)
		{
			_scope = scope;
			_rh = new InfoScopeRH(_gdbProc, callback);
		}
		
		public override string Command 
		{
			get { return "info scope " + _scope; }
		}
		
		protected override string LogIdentifier 
		{
			get { return "info scope";}
		}
		
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}
	}
}

