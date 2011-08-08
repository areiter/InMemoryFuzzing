// IFuzzDescription.cs
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
using Fuzzer.TargetConnectors;
using Fuzzer.DataGenerators;
namespace Fuzzer.FuzzDescriptions
{
	/// <summary>
	/// Describes a single "fuzzin technique" e.g.
	/// InMemoryPointerValue, InMemorySingleValue, UnixSocket, NetSocket,....
	/// </summary>
	public interface IFuzzTech
	{
		
		/// <summary>
		/// Is called before the first fuzzing round
		/// Do all initializations here
		/// </summary>
		void Init();
		
		/// <summary>
		/// Insert the modified values here
		/// </summary>
		/// <remarks>
		/// Changing variables may require the snapshot to be recreated (this implies that the program counter
		/// is located at the beginning of the snapshot)
		/// </remarks>
		void Run(FuzzController ctrl);
		
		
	}
}

