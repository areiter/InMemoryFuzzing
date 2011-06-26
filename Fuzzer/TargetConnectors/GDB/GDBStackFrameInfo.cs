// GDBStackFrameInfo.cs
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
using System.Collections;
using System.Collections.Generic;
namespace Fuzzer.TargetConnectors.GDB
{
	public class GDBStackFrameInfo : IStackFrameInfo
	{
		private IDictionary<string, IAddressSpecifier> _savedRegisters;
		
		public GDBStackFrameInfo (IDictionary<string, IAddressSpecifier> savedRegisters)
		{
			_savedRegisters = savedRegisters;
		}
	

		#region IStackFrameInfo implementation
		public IAddressSpecifier GetSavedRegisterAddress (string registerName)
		{
			if (_savedRegisters.ContainsKey (registerName))
				return _savedRegisters[registerName];
			
			return null;
		}
		#endregion
}
}

