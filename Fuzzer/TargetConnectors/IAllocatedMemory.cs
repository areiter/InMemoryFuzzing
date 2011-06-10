// IAllocatedMemory.cs
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
	/// <summary>
	/// Represents a chunk of allocated memory
	/// </summary>
	/// <remarks>
	/// It is nearly the same than an ISymbolTableVariable, but it marks
	/// memory chunks that can be freed by the connector.
	/// 
	/// Ordinary ISymbolTableVariables cannot be freed
	/// </remarks>
	public interface IAllocatedMemory
	{
		/// <summary>
		/// Returns the address of the allocated memory
		/// </summary>
		UInt64 Address { get; }
	}
}

