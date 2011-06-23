// EchoCommand.cs
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
namespace Fuzzer.RemoteControl
{
	public class EchoCommand : RemoteCommand
	{
		private string _message;
		
		public EchoCommand (string message)
		{
			_message = message;
		}
		
		#region implemented abstract members of Fuzzer.RemoteControl.RemoteCommand
		public override string Receiver 
		{
			get { return "ECHO"; }
		}
		
		
		public override byte[] Data 
		{
			get { return Encoding.ASCII.GetBytes (_message); }
		}
		
		#endregion
	}
}

