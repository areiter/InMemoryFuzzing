// SymbolTableMethod.cs
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
namespace Fuzzer.TargetConnectors
{
	public class SymbolTableMethod : ISymbolTableMethod
	{
		private string _name;
		private UInt64? _address;
		private ISymbolTable _symbolTable;
		
		public SymbolTableMethod (ISymbolTable symbolTable, string name, UInt64 address)
		{
			_symbolTable = symbolTable;
			_name = name;
			_address = address;
		}
	

		#region ISymbolTableMethod implementation
		public string Name 
		{
			get { return _name; }
		}

		public ulong? Address 
		{
			get { return _address; }
		}
	
		public void Resolve()
		{
			_address = _symbolTable.ResolveSymbol(this);
		}
		#endregion
		
		#region ISymbol implementation
		public string Symbol 
		{
			get { return Name; }
		}
		#endregion
}
}

