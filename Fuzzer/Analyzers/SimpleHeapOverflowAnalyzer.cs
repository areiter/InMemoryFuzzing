// SimpleHeapOverflowAnalyzer.cs
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
using Iaik.Utils.CommonAttributes;
namespace Fuzzer.Analyzers
{
	/// <summary>
	/// Detects simple heap overflows e.g.:
	/// 
	/// Memory is allocated for 300 bytes. But program may also write to 301, 302,... without segmentation fault or similar because
	/// malloc allocates a whole page.
	/// </summary>
	/// <remarks>
	/// Uses the files: *.pipes
	/// 
	/// This analyzer builts a "map" of allocated memory and checks the bounds for each memory write.
	/// If a program allocates 2 times 300 bytes and writes to the 301 byte of the first allocation, the error
	/// may not be detected if the memory chunks are allocated without a gap.
	/// </remarks>
	[ClassIdentifier("analyzers/simple_heap_overflow")]
	public class SimpleHeapOverflowAnalyzer : BaseDataAnalyzer
	{
		public SimpleHeapOverflowAnalyzer ()
		{
		}
		
		#region implemented abstract members of Fuzzer.Analyzers.BaseDataAnalyzer
		public override string LogIdentifier 
		{
			get { return "SimpleHeapOverflowAnalyzer";}
		}


		public override void Analyze (AnalyzeController ctrl)
		{
			//GenerateFile(		
		}
		
		#endregion

	}
}

