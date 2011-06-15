// GDBSymbolTableVariableAddress.cs
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
namespace Fuzzer.TargetConnectors.GDB
{
	public class GDBSymbolTableVariableAddress : ISymbolTableVariable
	{
		private IAddressSpecifier _address;
		private int _size;
		private GDBConnector _connector;
		
		public GDBSymbolTableVariableAddress (GDBConnector connector, IAddressSpecifier address, int size)
		{
			_connector = connector;
			_address = address;
			_size = size;
		}
	

		#region ISymbolTableVariable implementation
		

		public string Name 
		{
			get { return Symbol; }
		}

		public int Size 
		{
			get { return _size; }
		}

		public Nullable<ulong> Address 
		{
			get { return _address.ResolveAddress (); }
		}
		
		public string Symbol 
		{
			get 
			{
				UInt64? address = Address;
				
				if (address != null)
					return string.Format ("0x{0:X}", address.Value);
				else
					return null;
			}
		}
		
		public ISymbolTableVariable Dereference ()
		{
			return GDBSymbolTableVariable.InternalDereference (_connector, Address, Size);
		}
		
		public ISymbolTableVariable Dereference (int index)
		{
			return GDBSymbolTableVariable.InternalDereference (_connector, Address.Value + (UInt64)(index * Size), Size);
		}
		#endregion

	}
}

