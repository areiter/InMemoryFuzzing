// GDBSymbolTableVariable.cs
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
using System.Threading;
using Iaik.Utils;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Represents a variable that gets resolved at runtime. Only use this implementation if 
	/// the variable is not resolveable with "info address ..." (GDBSymbolTableVariable)
	/// </summary>
	public class GDBCStyleReferenceSymbolTableVariable : GDBSymbolTableVariable
	{
	
		public GDBCStyleReferenceSymbolTableVariable (GDBConnector connector, string name, int size)
			: base(connector, name, size)
		{
		}
	

		#region ISymbolTableVariable implementation		
		public override UInt64? Address 
		{
			get 
			{
				GDBCStyleReferenceAddressSpecifier address = new GDBCStyleReferenceAddressSpecifier(_name, _connector);
				return address.ResolveAddress();
			}
		}
		
		#endregion
		
	}
}

