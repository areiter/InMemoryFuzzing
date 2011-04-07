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
	/// Abstract SwapManager, T specifies the type of objects to swap
	/// </summary>
	public abstract class SwapManager<T> where T: class
	{
		/// <summary>
		/// Specifies the replacement algorithm to use
		/// </summary>
		protected IReplacementAlgorithm _replacementAlgorithm;		
		
		/// <summary>
		/// Mapps identifiers to items
		/// </summary>
		private Dictionary<UInt64, T> _items = new Dictionary<UInt64, T>();
		
		public SwapManager (IReplacementAlgorithm replacementAlgorithm)
		{
			_replacementAlgorithm = replacementAlgorithm;	
		}
		
		/// <summary>
		/// Adds a new item to the internal cache
		/// </summary>
		/// <param name="item"></param>
		protected void AddNewItem(T item)
		{
			lock(_items)
			{
				UInt64 newId = _replacementAlgorithm.RegisterNew();
				_items.Add(newId, item);
			}
		}

        /// <summary>
        /// Adds a new item to the internal cache
        /// </summary>
        /// <param name="item"></param>
        protected void RemoveItem(T item)
        {
            lock (_items)
            {
                _replacementAlgorithm.Remove(ItemToId(item).Value);
                _items.Remove(ItemToId(item).Value);
            }
        }
		                          
		
		/// <summary>
		/// Swaps put a single handle
		/// </summary>
		protected virtual void SwapOut()
		{
			SwapOut(1);
		}
		
		/// <summary>
		/// Looks for a candidate to swap out and performs the swap operation
		/// </summary>
		protected virtual void SwapOut(int swapCount)
		{			
			lock(_items)
			{
				List<ulong> swapables = _replacementAlgorithm.Swapables;
				
				for(int i = 0; i<Math.Min(swapCount, swapables.Count); i++)
				{
					ulong swapCandidate = swapables[i];
					
					_replacementAlgorithm.SwapOut(swapCandidate);
					SwappedOut(IdToItem(swapCandidate));				
				}
			}
		}
		
		/// <summary>
		/// Swapps in the specified item
		/// </summary>
		/// <param name="item"></param>
		protected virtual void SwapIn(T item)
		{
			lock(_items)
			{
				UInt64? id = ItemToId(item);
				
				if(id == null)
					return;
				
				_replacementAlgorithm.SwapIn(id.Value);
				_replacementAlgorithm.RegisterUsed(id.Value);
				_replacementAlgorithm.Update();
				SwappedIn(item);
			}
		}
		
		/// <summary>
		/// Called on item swap in
		/// </summary>
		/// <param name="item"></param>
		protected virtual void SwappedIn(T item)
		{
		}
		
		/// <summary>
		/// Called on item swap out
		/// </summary>
		/// <param name="item"></param>
		protected virtual void SwappedOut(T item)
		{
		}
		
		private T IdToItem(UInt64 id)
		{
			if(_items.ContainsKey(id))
				return _items[id];
			
			return null;
		}
		
		public UInt64? ItemToId(T item)
		{
			foreach(KeyValuePair<UInt64, T> val in _items)
			{
				if(val.Value == val.Value)
					return val.Key;
			}
			
			return null;
		}
	}
}
