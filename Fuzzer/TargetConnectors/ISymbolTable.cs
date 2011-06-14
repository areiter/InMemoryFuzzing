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
		/// Resolves the specified symbol to an address.
		/// It resolves to the address where the first 
		/// instruction of the method is located (BEFORE THE PROLOG of a method if one exists)
		/// </summary>
		/// <param name="symbol">
		/// A <see cref="ISymbol"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Nullable<UInt64>"/>
		/// </returns>
		IAddressSpecifier ResolveSymbol(ISymbol symbol);
		
		/// <summary>
		/// Resolves the specified symbol to an address.
		/// It resolves to the address where all parameters
		/// already got their values (AFTER METHOD PROLOG).
		/// This is needed to properly place breakpoints,
		/// and to be able to modify the method parameters.
		/// If no complete debuggoing information s available,
		/// always return the address after the parameters got assigned.
		/// Calling this method of course is only valid for ISymbolTableMethods
		/// </summary>
		/// <param name="symbol">
		/// A <see cref="ISymbol"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Nullable<UInt64>"/>
		/// </returns>
		IAddressSpecifier ResolveSymbolToBreakpointAddress(ISymbolTableMethod symbol);
		
		/// <summary>
		/// Gets the parameters of the specified method
		/// </summary>
		/// <param name="method">A <see cref="ISymbolTableMethod"/></param>
		/// <returns>A <see cref="ISymbolTableVariable[]"/></returns>
		ISymbolTableVariable[] GetParametersForMethod(ISymbolTableMethod method);
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
		IAddressSpecifier AddressSpecifier{ get; }
		
		/// <summary>
		/// Gets the address to set the breakpoint to. 
		/// Is greater than Address if a method prolog exists and
		/// is set to the address after all method parameters got
		/// ther initial values
		/// </summary>
		IAddressSpecifier BreakpointAddressSpecifier{ get; }
		
		/// <summary>
		/// Returns the parameters of the method if available
		/// </summary>
		ISymbolTableVariable[] Parameters{ get; }
		
		/// <summary>
		/// Resolves the symbol to an address, call with caution, it depends on the symbol table implementation
		/// if it is allowed to call this method
		/// </summary>
		void Resolve();
		
		
		
	}
	
	public interface ISymbolTableVariable : ISymbol
	{
		/// <summary>
		/// Name of the variable or any identifier
		/// </summary>
		string Name{ get; }
		
		/// <summary>
		/// Byte Size of the variable
		/// </summary>
		int Size { get; }
		
		/// <summary>
		/// Returns the address of the variable or null 
		/// if the variable is not valid in the current scope
		/// </summary>
		UInt64? Address { get; }
		
		/// <summary>
		/// Dereferences the current variable.
		/// This means that the value stored at the variables-address is interpreted as address
		/// </summary>
		/// <returns></returns>
		ISymbolTableVariable Dereference();
	}
}

