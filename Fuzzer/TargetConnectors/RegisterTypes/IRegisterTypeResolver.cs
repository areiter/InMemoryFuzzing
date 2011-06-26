// IRegisterTypeResolver.cs
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
namespace Fuzzer.TargetConnectors.RegisterTypes
{
	/// <summary>
	/// Defines the different register types
	/// </summary>
	public enum RegisterTypeEnum
	{
		/// <summary>
		/// Defines the register witch contains the next instrction to execute (eip, rip,...)
		/// </summary>
		ProgramCounter
	}
	
	/// <summary>
	/// Finds registers based on their purpose
	/// </summary>
	public interface IRegisterTypeResolver
	{
		/// <summary>
		/// Resolves the specified register type to target specific register name
		/// </summary>
		/// <param name="registerType">Register type to resolve</param>
		/// <returns>Returns the resolved register name or null if register is not available on the target system</returns>
		string GetRegisterName(RegisterTypeEnum registerType);
	}
}

