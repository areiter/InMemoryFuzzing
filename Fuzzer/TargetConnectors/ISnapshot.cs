// ISnapshot.cs
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
	/// <summary>
	/// Represents a snapshot of a process on the target platform
	/// </summary>
	public interface ISnapshot : IDisposable
	{
		/// <summary>
		/// Restores the process to the saved state.
		/// It is up to the target connector to decide if multiple snapshots are possible, because restoring a 
		/// snapshot may revert data needed by another snapshot
		/// </summary>
		void Restore();
		
		/// <summary>
		/// Destroys the snapshot and discards its data
		/// </summary>
		void Destroy();
	}
}

