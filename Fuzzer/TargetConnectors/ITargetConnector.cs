// ITargetConnector.cs
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
	/// Implemented by classes that provide fuzzer access to targets.
	/// </summary>
	/// <remarks>
	/// Target connectors can be architecture specific or cross platform
	/// 
	/// Always attach the <see cref="Iaik.Utils.CommonAttributes.ClassIdentifierAttribute"/> 
	/// to implementing classes.
	/// As a guideline use prefix "general/..." for cross platform target connectors
	/// and "win/...", "linux/...",.... for platform specific target connectors
	/// </remarks>
	public interface ITargetConnector : IDisposable
	{
		/// <summary>
		/// Gets the connection state
		/// </summary>
		bool Connected{ get; }
		
		/// <summary>
		/// Sets up the connector
		/// </summary>
		/// <param name="config">
		/// A <see cref="IDictrionary<System.String, System.Object>"/>
		/// </param>
		void Setup(IDictionary<string, string> config);
		
		/// <summary>
		/// Connects to the target
		/// </summary>
		void Connect();
		
		/// <summary>
		/// Closes the connection to the target
		/// </summary>
		void Close();
		
		/// <summary>
		/// Reads the memory at the specified address
		/// </summary>
		/// <param name="buffer">Buffer of at least size in length</param>
		/// <param name="address">Address to start reading from </param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns>Returns the number of actual read bytes</returns>
		UInt64 ReadMemory(byte[] buffer, UInt64 address, UInt64 size);
		
		/// <summary>
		/// Writes the memory at the specified address
		/// </summary>
		/// <param name="buffer">Buffer of at least size in length</param>
		/// <param name="address">Address to start writing to </param>
		/// <param name="size">Number of bytes to write</param>
		/// <returns>Returns the number of actual written bytes</returns>
		UInt64 WriteMemory(byte[] buffer, UInt64 address, UInt64 size);
		
		/// <summary>
		/// Sets a software breakpoint at the specified address
		/// </summary>
		/// <param name="address">Address of the breakpoint</param>
		/// <param name="size">Specify the size of the instruction at address to patch</param>
		void SetSoftwareBreakpoint(UInt64 address, UInt64 size);
	}
}

