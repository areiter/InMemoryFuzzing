// TargetRH.cs
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
	public class TargetRH : GDBResponseHandler
	{
		private Action<bool> _connectionStatusCb;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override string LogIdentifier 
		{
			get { return "RH_target"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess connector, string[] responseLines, bool allowRequestLine)
		{
			foreach(string line in responseLines)
			{
				if(line.Trim().StartsWith("remote debugging using", StringComparison.InvariantCultureIgnoreCase))
				{
					_connectionStatusCb(true);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				else if(line.Trim().EndsWith("unknown host", StringComparison.InvariantCultureIgnoreCase))
				{
					_connectionStatusCb(false);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;
		}
		
		#endregion
		public TargetRH (Action<bool> connectionStatusCb, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_connectionStatusCb = connectionStatusCb;
		}
	}
}

