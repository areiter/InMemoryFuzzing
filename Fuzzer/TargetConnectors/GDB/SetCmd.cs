// SetCmd.cs
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
using System.Text;
namespace Fuzzer.TargetConnectors.GDB
{
	public class SetCmd : GDBCommand
	{
		private string _register;
		private string _value;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		
		
		public override string Command 
		{
			get{ return string.Format("set {0}={1}", _register, _value); }
		}
		
		#endregion
		public SetCmd (string register, string value)
		{
			_register = register;
			_value = value;
		}
	}
}

