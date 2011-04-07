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
	
	/// <summary>
	/// Apama session that is used to connect and interact with a specific device
	/// </summary>
	public class apama_session
	{
		public apama_snapshot[] snapshots;
		
		/// <summary>
		/// Unmarshal structure from Pointer. 
		/// </summary>
		/// <remarks>
		/// Cannot be done with a simple unmarshall, because
		/// structure contains a linked list
		/// </remarks>
		/// <param name="ptr">Pointer to a apama_session_t structure</param>
		public void ReadFromIntPtr(IntPtr ptr)
		{
			if(ptr.Equals(IntPtr.Zero))
				throw new ArgumentException("Cannot read from pointer to zero");
			
			
			List<apama_snapshot> snapshots = new List<apama_snapshot>();
			
			apama_snapshot_internal currentSnapshot = null;
			currentSnapshot = apama_snapshot_internal.ReadFromPointer(ptr);
			
			do
			{
				snapshots.Add(currentSnapshot.CreateSnapshot());	
			}while((currentSnapshot = currentSnapshot.GetNextSnapshot()) != null);
		}
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
			          nextSnapshot, 
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
}

