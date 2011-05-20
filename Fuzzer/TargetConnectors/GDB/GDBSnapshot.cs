// GDBSnapshot.cs
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
	/// ISnapshot implementation for the GDB connector,
	/// keep in mind that only a single snapshot is possible 
	/// using the GDB connector
	/// </summary>
	public class GDBSnapshot : ISnapshot
	{
		private GDBConnector _connector;
		private IBreakpoint _myBreakpoint;
				
		public GDBSnapshot (GDBConnector connector, IDebuggerStop lastDebuggerStop)
		{
			_connector = connector;
			
			if(lastDebuggerStop == null)
				throw new ArgumentException("No last stop address found");
			
			if(lastDebuggerStop.Address == 0 )
				throw new ArgumentException("Last stop does not provide address");
			
			_myBreakpoint = connector.SetSoftwareBreakpoint(lastDebuggerStop.Address, 0, "_snapshot");
			
			//Activate reverse debugging
			connector.QueueCommand(new RecordCmd());
		}
	

		#region ISnapshot implementation
		public void Restore ()
		{
			IDebuggerStop dbg = null;
			
			do
			{
				dbg = _connector.DebugContinue(true);
			}while(dbg.Breakpoint.Address != _myBreakpoint.Address);
				
		}

		public void Destroy ()
		{
			_myBreakpoint.Delete();
		}
		#endregion
		
		
		#region IDisposable implementation
		public void Dispose ()
		{
			Destroy();
		}
		#endregion

	}
}

