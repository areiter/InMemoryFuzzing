// ISymbolTable.cs
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
using System.Collections.Generic;
namespace Fuzzer.TargetConnectors
{
	/// <summary>
	/// Converts Symbols to addresses and vice versa
	/// </summary>
	public interface ISymbolTable : IDisposable
	{
		/// <summary>
		/// Sets up the symbol table connector
		/// </summary>
		/// <param name="config">
		/// A <see cref="IDictrionary<System.String, System.Object>"/>
		/// </param>
		void Setup(IDictionary<string, string> config);


		/// <summary>
		/// Lists all available methods (debugging or linking symbols)
		/// </summary>
		ISymbolTableMethod[] ListMethods{ get; }
		
		/// <summary>
		/// Looks for a method with the specified name
		/// </summary>
		/// <param name="methodName">A <see cref="System.String"/></param>
		/// <returns>A <see cref="ISymbolTableMethod"/></returns>
		ISymbolTableMethod FindMethod(string methodName);
		
		/// <summary>
		/// Resolves the specified symbol to an address
		/// </summary>
		/// <param name="symbol">
		/// A <see cref="ISymbol"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Nullable<UInt64>"/>
		/// </returns>
		UInt64? ResolveSymbol(ISymbol symbol);
	}
	
	/// <summary>
	/// Implemented by symbols that can be resolved to addresses
	/// </summary>
	public interface ISymbol
	{
		/// <summary>
		/// Returns the symbol of this instance
		/// </summary>
		string Symbol{get;}
	}
	
	/// <summary>
	/// Represents a single method with its parameters if available
	/// </summary>
	public interface ISymbolTableMethod : ISymbol
	{
		/// <summary>
		/// Gets the name or identifier of the mthod
		/// </summary>
		string Name{ get; }
		
		/// <summary>
		/// Gets the address of the method
		/// </summary>
		UInt64? Address{ get; }
		
		/// <summary>
		/// Resolves the symbol to an address, call with caution, it depends on the symbol table implementation
		/// if it is allowed to call this method
		/// </summary>
		void Resolve();
		
	}
	
	public interface ISymbolTableVariable
	{
		string Name{ get; }
	}
}

