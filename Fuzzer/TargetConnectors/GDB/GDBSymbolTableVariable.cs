// SymbolTableVariable.cs
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
	/// Represents a variable that gets resolved at runtime
	/// </summary>
	public class GDBSymbolTableVariable : ISymbolTableVariable
	{
		private GDBConnector _connector;
		private GDBSymbolTable _symbolTable;
		private string _name;
		
		public GDBSymbolTableVariable (GDBConnector connector, GDBSymbolTable symbolTable, string name)
		{
			_connector = connector;
			_symbolTable = symbolTable;
			_name = name;
		}
	

		#region ISymbolTableVariable implementation
		public string Name 
		{
			get { return _name; }
		}

		public UInt64? Address 
		{
			get 
			{
				//Resolve the address
				
				throw new NotImplementedException();
			}
		}
		#endregion
}
}

