// GDBCommand.cs
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
using log4net;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Represents a generic commando to GDB
	/// </summary>
	public abstract class GDBCommand
	{
		/// <summary>
		/// Associated Response handler, if response handler is not set, command is sent without processing any response
		/// </summary>
		public abstract GDBResponseHandler ResponseHandler{ get; }
		
		protected ILog _logger = null;
		
		public abstract string Command { get; }
		
		protected abstract string LogIdentifier{get;}
		
		public GDBCommand ()
		{
			_logger = LogManager.GetLogger(LogIdentifier);
		}
		
		
	}
}

