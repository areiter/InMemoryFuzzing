// ApamaBreakpoint.cs
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
namespace Fuzzer.TargetConnectors
{
	public class ApamaBreakpoint : IBreakpoint
	{
		/// <summary>
		/// Already destroyed?
		/// </summary>
		private bool _disposed = false;
		
		/// <summary>
		/// Associated connector
		/// </summary>
		private ApamaLinuxConnector _connector;
		
		/// <summary>
		/// Type of the breakpoint (hw/sw(
		/// </summary>
		private ApamaBreakpointType _breakpointType;
		
		/// <summary>
		/// Address of the breakpoint
		/// </summary>
		private UInt64 _address;
		
		/// <summary>
		/// Size of the target command to patch
		/// </summary>
		private UInt64 _size;
		
		internal ApamaBreakpoint (ApamaLinuxConnector connector, ApamaBreakpointType breakpointType,
		                          UInt64 address, UInt64 size)
		{
			_connector = connector;
			_breakpointType = breakpointType;
			_address = address;
			_size = size;
		}	

		private void AssertBreakpoint()
		{
			if(_disposed)
				throw new ObjectDisposedException("Breakpoint already removed and cannot be used any more");
		}
		
		#region IBreakpoint implementation
		public void RemoveBreakpoint ()
		{
			if(_breakpointType == ApamaBreakpointType.APAMA_MEMORY_BREAKPOINT)
			{
				_connector.RemoveSoftwareBreakpoint(_address, _size);
				_disposed = true;
			}
		}

		public bool Enabled 
		{
			get { return true; }
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			RemoveBreakpoint();
		}
		#endregion
}
}

