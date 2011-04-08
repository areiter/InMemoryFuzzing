// ApamaHelpers.cs
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

namespace Fuzzer.TargetConnectors
{
	
	public class ApamaException:Exception
	{
		public ApamaException(string message):
			base(message)
		{
		}
		
	}
	
	public enum ApamaBreakpointType : int
	{
  		APAMA_MEMORY_BREAKPOINT   = 0, /**< A memory breakpoint */
		APAMA_HARDWARE_BREAKPOINT = 1  /**< A hardware breakpoint */
	}
	
	public enum ApamaReturnValue
	{
  		APAMA_ERROR_OK, /**< No error occured */
  		APAMA_ERROR_UNKNOWN, /**< An unknown error occured */
  		APAMA_ERROR_UNKNOWN_PROTOCOL, /**< The given protocol is not known */
  		APAMA_ERROR_CONNECTION, /**< Problem with the connection */
  		APAMA_ERROR_PROTOCOL, /**< Protocol error */
  		APAMA_ERROR_OUT_OF_MEMORY, /**< Out of memory */
  		APAMA_ERROR_NOT_IMPLEMENTED, /**< This function is not implemented by the given protocol */
  		APAMA_ERROR_BAD_SESSION /**< Caller provided an invalid session */
	};
	
	/// <summary>
	/// Apama session that is used to connect and interact with a specific device
	/// </summary>
	public class apama_session
	{
		/// <summary>
		/// Pointer to the original session struct
		/// </summary>
		public IntPtr apama_session_ptr;
		
		public apama_snapshot[] snapshots;
		
		/// <summary>
		/// Unmarshal structure from Pointer. 
		/// </summary>
		/// <remarks>
		/// Cannot be done with a simple unmarshall, because
		/// structure contains a linked list
		/// </remarks>
		public void ReadFromIntPtr(IntPtr apama_session_ptr, apama_session_internal internalSession)
		{
			this.apama_session_ptr = apama_session_ptr;
			
			List<apama_snapshot> snapshots = new List<apama_snapshot>();
			IntPtr nextSnapshot = internalSession.ptr_apama_snapshot_internal;	
			apama_snapshot_internal currentSnapshot = null;			
			
			while(!nextSnapshot.Equals(IntPtr.Zero))
			{			
				currentSnapshot = apama_snapshot_internal.ReadFromPointer(nextSnapshot);
				snapshots.Add(currentSnapshot.CreateSnapshot());
				nextSnapshot = currentSnapshot.nextSnapshot;
			}
			
			this.snapshots = snapshots.ToArray();
		}
		
		
	}
	
	
	[StructLayout(LayoutKind.Sequential)]
	public class apama_session_internal
	{
		public IntPtr ptr_apama_snapshot_internal;
	}
	
	
	[StructLayout(LayoutKind.Sequential)]
	public class apama_snapshot_internal
	{
		/// <summary>
		/// Id of the snapshot
		/// </summary>
		public int id;
		
		/// <summary>
		/// Pointer to the next snapshot
		/// </summary>
		public IntPtr nextSnapshot;
		
		public apama_snapshot CreateSnapshot()
		{
			return new apama_snapshot(id);
		}
		
		
		public apama_snapshot_internal GetNextSnapshot()
		{
			if(nextSnapshot.Equals(IntPtr.Zero))
				return null;
			
			return ReadFromPointer(nextSnapshot);
		}
		
		public static apama_snapshot_internal ReadFromPointer(IntPtr ptr)
		{
			return (apama_snapshot_internal)Marshal.PtrToStructure(
			          ptr, 
			          typeof(apama_snapshot_internal)
			        );
		}
		
	}
	
	/// <summary>
	/// 
	/// </summary>
	public class apama_snapshot
	{
		/// <summary>
		/// Id of the snapshot
		/// </summary>
		public int id;
		
		internal apama_snapshot(int id)
		{
			this.id = id;
		}
	}
	
	public static class ApamaExtensions
	{
		public static void Assert(this ApamaReturnValue returnValue)
		{
			if(returnValue != ApamaReturnValue.APAMA_ERROR_OK)
				throw new ApamaException(returnValue.ToString());
		}
	}
}

