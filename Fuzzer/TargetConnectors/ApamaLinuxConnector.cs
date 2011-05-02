// ApamaLinuxConnector.cs
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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

using Iaik.Utils;
using Iaik.Utils.CommonAttributes;
	

namespace Fuzzer.TargetConnectors
{
	/// <summary>
	/// Builds a connection to the target using libapama compiled for linux
	/// </summary>
	/// <remarks>
	/// Parameters:
	/// "protocol": protocol:target,parameter1=value1,parameter2=value2
	///  e.g. gdb:127.0.0.1:1234
	///       gdb:/dev/tty1,target=device.xml
	/// </remarks>
	[ClassIdentifier("linux/apama")]
	public class ApamaLinuxConnector : ITargetConnector
	{
		
		/// <summary>
		/// Current apama session
		/// </summary>
		private apama_session _currentSession = null;
		
		/// <summary>
		/// Protocol definition of libapama
		/// </summary>
		private string _protocol = null;
		
		#region Native Imports
		
		[DllImport("libapama.so")]
		private static extern IntPtr apama_session_create(StringBuilder protocol);
		
		[DllImport("libapama.so")]
		private static extern ApamaReturnValue apama_session_destroy(IntPtr session);
		
		[DllImport("libapama.so")]
		private static extern ApamaReturnValue apama_memory_read(IntPtr session, UInt64 address, ref byte[] buffer, ref UInt64 read_bytes, UInt64 n);
		
		[DllImport("libapama.so")]
		private static extern ApamaReturnValue apama_memory_write(IntPtr session, UInt64 address, ref byte[] buffer, ref UInt64 written_bytes, UInt64 n);
		
		[DllImport("libapama.so")]
		private static extern ApamaReturnValue apama_breakpoint_set(IntPtr session, ApamaBreakpointType type, UInt64 address, UInt64 kind);
		
		[DllImport("libapama.so")]
		private static extern ApamaReturnValue apama_breakpoint_remove(IntPtr session, ApamaBreakpointType type, UInt64 address, UInt64 kind);
		
		[DllImport("libapama.so")]
		private static extern ApamaReturnValue apama_continue(IntPtr session, UInt64 address);
		
		#endregion
		public ApamaLinuxConnector ()
		{
		}
		
		#region ITargetConnector implementation
		public bool Connected {
			get {
				return _currentSession != null;
			}
		}
		
		public void Setup (IDictionary<string, string> config)
		{
			_protocol = DictionaryHelper.GetString("protocol", config, null);
			if(_protocol == null)
				throw new KeyNotFoundException("Value for \"protocol\" not found");
			
			
		}

		public void Connect ()
		{
			IntPtr returnValue = apama_session_create(
			  new StringBuilder(_protocol)
			);
			
			apama_session_internal internalSession = (apama_session_internal)Marshal.PtrToStructure(returnValue, typeof(apama_session_internal));
			
			if(internalSession == null)
				throw new ApamaException("Could not connect");
			
			apama_session session = new apama_session();
			session.ReadFromIntPtr(returnValue, internalSession);
			_currentSession = session;
		}

		public void Close()
		{
			if(_currentSession != null)
			{
				ApamaReturnValue returnVal = (ApamaReturnValue)apama_session_destroy(_currentSession.apama_session_ptr);
				if(returnVal != ApamaReturnValue.APAMA_ERROR_OK)
					throw new ApamaException("Not a valid session");
				
				_currentSession = null;
			}
		}		

		public ulong ReadMemory (byte[] buffer, ulong address, ulong size)
		{
			AssertSession();
			
			ulong readBytes = 0;
			apama_memory_read(_currentSession.apama_session_ptr, address, ref buffer, ref readBytes, size).Assert();
			return readBytes;
		}

		public ulong WriteMemory (byte[] buffer, ulong address, ulong size)
		{
			AssertSession();
			
			ulong writtenBytes = 0;
			apama_memory_write(_currentSession.apama_session_ptr, address, ref buffer, ref writtenBytes, size);
			return writtenBytes;
		}
		
		public IBreakpoint SetSoftwareBreakpoint(UInt64 address, UInt64 size, string identifier)
		{
			AssertSession();
			apama_breakpoint_set(_currentSession.apama_session_ptr, ApamaBreakpointType.APAMA_MEMORY_BREAKPOINT, address, size).Assert();
			
			return new ApamaBreakpoint(this, ApamaBreakpointType.APAMA_MEMORY_BREAKPOINT, address, size);
		}
		
		public void RemoveSoftwareBreakpoint(UInt64 address, UInt64 size)
		{
			AssertSession();
			apama_breakpoint_remove(_currentSession.apama_session_ptr, ApamaBreakpointType.APAMA_MEMORY_BREAKPOINT, address, size).Assert();
		}
		
		public void DebugContinue()
		{
			AssertSession();
			apama_continue(_currentSession.apama_session_ptr, 0).Assert();
		}
		
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			if(_currentSession != null)
				Close();
		}
		#endregion

		
		/// <summary>
		/// Throws an exception if the connector is not connected
		/// </summary>
		private void AssertSession()
		{
			if(!Connected)
				throw new ApamaException("Not connected to target");
		}
		
		
	}
}

