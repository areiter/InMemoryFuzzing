/* Copyright 2010 Andreas Reiter <andreas.reiter@student.tugraz.at>, 
 *                Georg Neubauer <georg.neubauer@student.tugraz.at>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


// Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;
using System.Collections.Generic;

namespace Iaik.Utils.SwapUtils
{

	/// <summary>
	/// This is the common interface for different replacement algorithms. A replacement algorithm
	/// has to implement this interface. The algorithmen has to register unique ids, for each element registered
	/// via the RegisterNew() method. The mapping from id to object, has to be done outside. 
	/// </summary>
	public interface IReplacementAlgorithm
	{
		/// <summary>
		/// Each id in ids walks back into used state.
		/// </summary>
		/// <param name="ids">
		/// A <see cref="List<UInt64>"/>
		/// </param>
		void SwapIn(List<UInt64> ids);
		
		/// <summary>
		/// The given id walks back into used state.
		/// </summary>
		/// <param name="id">
		/// A <see cref="UInt64"/>
		/// </param>
		void SwapIn(UInt64 id);
		
		/// <summary>
		/// Each id in ids walks into swapped state.
		/// </summary>
		/// <param name="ids">
		/// A <see cref="List<UInt64>"/>
		/// </param>
		void SwapOut(List<UInt64> ids);
		
		/// <summary>
		/// The given id walks into swapped state.
		/// </summary>
		/// <param name="ids">
		/// A <see cref="List<UInt64>"/>
		/// </param>
		void SwapOut(UInt64 id);
		
		/// <summary>
		/// Is the requested id currently marked as swapped?
		/// </summary>
		/// <param name="id">
		/// A <see cref="UInt64"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		bool IsSwaped(UInt64 id);
		
		/// <summary>
		/// Update the current state, to this at each usage cycle.
		/// </summary>
		void Update();
		
		/// <summary>
		/// Register an id as used.
		/// </summary>
		/// <param name="id">
		/// A <see cref="UInt64"/>
		/// </param>
		void RegisterUsed(UInt64 id);
		
		/// <summary>
		/// Register each id in ids as used.
		/// </summary>
		/// <param name="ids">
		/// A <see cref="List<UInt64>"/>
		/// </param>
		void RegisterUsed(List<UInt64> ids);
		
		/// <summary>
		/// Get an id for an new object.
		/// </summary>
		/// <returns>
		/// A <see cref="UInt64"/>
		/// </returns>
		UInt64 RegisterNew();
		
		/// <summary>
		/// Remove the tracking for an object.
		/// </summary>
		/// <param name="id">
		/// A <see cref="UInt64"/>
		/// </param>
		void Remove(UInt64 id);
		
		/// <summary>
		/// Get a list of swapable ids, ascending in last time of usage.
		/// </summary>
		List<UInt64> Swapables{get;}
	}
}
