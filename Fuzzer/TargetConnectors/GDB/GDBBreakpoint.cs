// GDBBreakpoint.cs
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
	public class GDBBreakpoint : IBreakpoint
	{
		/// <summary>
		/// Already destroyed?
		/// </summary>
		private bool _disposed = false;
		
		/// <summary>
		/// Associated connector
		/// </summary>
		private GDBConnector _connector;

		/// <summary>
		/// The number of the breakpoint
		/// </summary>
		private int _breakpointNum;
		
		/// <summary>
		/// Address of the breakpoint
		/// </summary>
		private UInt64 _address;
		
		/// <summary>
		/// Identifier of the Breakpoint
		/// </summary>
		private string _identifier;
		
		/// <summary>
		/// Called on breakpoint removal
		/// </summary>
		private Action<int> _removeMe;
		
		internal GDBBreakpoint (GDBConnector connector,
		                        int breakpointNum,
		                        UInt64 address,
		                        string identifier,
		                        Action<int> removeMe)
		{
			_connector = connector;
			_breakpointNum = breakpointNum;
			_address = address;
			_identifier = identifier;
			_removeMe = removeMe;
		}	

		private void AssertBreakpoint()
		{
			if(_disposed)
				throw new ObjectDisposedException("Breakpoint already removed and cannot be used any more");
		}
		
		#region IBreakpoint implementation
		public void Delete ()
		{
			_connector.QueueCommand(new DeleteBreakpointCmd(_breakpointNum));
			_disposed = true;
			_removeMe(_breakpointNum);
		}

		public bool Enabled 
		{
			get { return true; }
		}
		
		public UInt64 Address
		{
			get{ return _address; }
		}
		
		public string Identifier
		{
			get{ return _identifier; }
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			Delete();
		}
		#endregion
	}
}

