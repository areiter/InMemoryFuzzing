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
			
	}
	
	/// <summary>
	/// Represents a single method with its parameters if available
	/// </summary>
	public interface ISymbolTableMethod
	{
		/// <summary>
		/// Gets the name or identifier of the mthod
		/// </summary>
		string Name{ get; }
		
		/// <summary>
		/// Gets the address of the method
		/// </summary>
		UInt64 Address{ get; }
		
		
		
	}
	
	public interface ISymbolTableVariable
	{
		string Name{ get; }
	}
}

