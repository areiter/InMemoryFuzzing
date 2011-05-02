// UnhandledRH.cs
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
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// This should always be the last permanent response handler, it accepts all responses and writes them to the log file
	/// </summary>
	public class UnhandledRH : GDBResponseHandler
	{
		
		protected override string LogIdentifier
		{
			get{ return "Unhandled GDB Response"; }
		}
		
		public UnhandledRH ()
		{
		}
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBConnector connector, string[] responseLines, bool allowRequestLine)
		{
			_logger.WarnFormat("Got {0} unhandled response lines:", responseLines.Length);
			
			foreach(string responseLine in responseLines)
				_logger.Warn(responseLine);
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}
		
		#endregion
		
	}
}

