// IDataGenerator.cs
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
using Fuzzer.DataLoggers;
using System.Collections.Generic;
namespace Fuzzer.DataGenerators
{
	/// <summary>
	/// Implemented by classes that generate data for the fuzzing process
	/// </summary>
	public interface IDataGenerator
	{
		/// <summary>
		/// Generate the next byte series.
		/// </summary>
		/// <remarks>
		/// The amount of bytes returned depends on the data generator implementation.
		/// E.g. a simple data generator which only generates data for int values may only return
		/// four bytes. Another one may return hundreds of bytes
		/// </remarks>
		/// <returns></returns>
		byte[] GenerateData();
		
		/// <summary>
		/// Sets the logger for the generated data to replay the generated data
		/// in another session
		/// </summary>
		/// <param name="logger"></param>
		void SetLogger(DataGeneratorLogger logger);
		
		/// <summary>
		/// Sets up the data generator. There may also be a ctor which makes this
		/// call obsolete. See implementation for configuration values
		/// </summary>
		/// <param name="config"></param>
		void Setup(IDictionary<string, string> config);
	}
}

