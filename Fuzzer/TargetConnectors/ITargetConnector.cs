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
	public interface ITargetConnector
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
	}
}

