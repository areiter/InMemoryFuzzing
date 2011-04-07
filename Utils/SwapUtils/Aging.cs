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
	/// This class implements the aging replacement algorithm
	/// </summary>
	public sealed class Aging : IReplacementAlgorithm
	{
		/// <summary>
		/// Local locking
		/// </summary>
		private object _syncLock = new object();
		
		// TODO: maybe add limits???
		const UInt64 UPDATE_MASK = 0x8000000000000000;
		List<KeyValuePair<UInt64, UInt64>> _used = new List<KeyValuePair<UInt64, UInt64>>();
		List<UInt64> _swaped = new List<UInt64>();
		List<UInt64> _recent = new List<UInt64>();
		UInt64 _nextID = 0;
		
		bool _isSorted = false;
		
		public Aging ()
		{
		}
		
		/// <summary>
		/// Internal helper for registering one id per time.
		/// </summary>
		/// <param name="id">
		/// A <see cref="UInt64"/>
		/// </param>
		private void InsertUsed(UInt64 id)
		{
			KeyValuePair<UInt64, UInt64> pair = new KeyValuePair<UInt64, UInt64>(id, UPDATE_MASK);
			_used.Add(pair);
			_isSorted = false;
		}
		
		#region IReplacementAlgorithm implementation
		public void SwapIn (List<ulong> ids)
		{
			lock(_syncLock)
			{
				foreach(UInt64 id in ids)
				{
					_swaped.Remove(id);
					InsertUsed(id);
				}
			}
		}
		
		
		public void SwapIn (ulong id)
		{
			lock(_syncLock)
			{
				_swaped.Remove(id);
				InsertUsed(id);
			}
		}
		
		
		public void SwapOut (List<ulong> ids)
		{
			lock(_syncLock)
			{
				// search for each id
				foreach(UInt64 id in ids)
				{
					// in each pair
					foreach(KeyValuePair<UInt64, UInt64> pair in _used)
					{
						if(pair.Key == id)
						{
							_used.Remove(pair);
							_swaped.Add(id);
							return;
						}
					}
				}
				throw new KeyNotFoundException();
			}
		}
		
		public void SwapOut (ulong id)
		{
			lock(_syncLock)
			{
				foreach(KeyValuePair<UInt64, UInt64> pair in _used)
				{
					if(pair.Key == id)
					{
						_used.Remove(pair);
						_swaped.Add(id);
						return;
					}
				}
				throw new KeyNotFoundException();
			}
		}
		
		public bool IsSwaped (UInt64 id)
		{
			lock(_syncLock)
			{
				return _swaped.Contains(id);
			}
		}
		
		public void Update ()
		{
			lock(_syncLock)
			{
				_isSorted = false;
				// create a new list, because KeyValuePairs can't be changed in place
				List<KeyValuePair<UInt64, UInt64>> helper = new List<KeyValuePair<UInt64, UInt64>>();
				// step through all items
				foreach(KeyValuePair<UInt64, UInt64> pair in _used)
				{
					KeyValuePair<UInt64, UInt64> newpair;
					// update if it was used
					if(_recent.Contains(pair.Key))
					{
						newpair =  new KeyValuePair<UInt64,UInt64>(pair.Key, (pair.Value >> 1) | UPDATE_MASK);
						_recent.Remove(pair.Key);
					}
					// also if not
					else
					{
						newpair =  new KeyValuePair<UInt64,UInt64>(pair.Key, pair.Value >> 1);
					}
					helper.Add(newpair);
				}
				_used = helper;
				// be sure nothing is registered
				_recent.Clear();
			}
		}
				
		public void RegisterUsed (ulong id)
		{
			lock(_syncLock)
			{
				_recent.Add(id);
			}
		}
		
		
		public void RegisterUsed (List<ulong> ids)
		{
			lock(_syncLock)
			{
				foreach(ulong id in ids)
				{
					_recent.Add(id);
				}
			}
		}
		
		
		public ulong RegisterNew ()
		{
			lock(_syncLock)
			{
				InsertUsed(_nextID);
				++_nextID;
				return _nextID - 1;
			}
		}
		
		
		public void Remove (ulong id)
		{
			lock(_syncLock)
			{
				// search in used
				foreach(KeyValuePair<UInt64, UInt64> pair in _used)
				{
					if(pair.Key == id)
					{
						_used.Remove(pair);
						return;
					}
				}
				// and in swaped
				foreach(UInt64 item in _swaped)
				{
					if(id == item)
					{
						_swaped.Remove(id);
						return;
					}
				}
			}
		}
		
		
		public List<ulong> Swapables {
			get {
				lock(_syncLock)
				{
					if(!_isSorted)
						_used.Sort(delegate(KeyValuePair<UInt64, UInt64> firstpair,
						                    KeyValuePair<UInt64, UInt64> nextpair){
						return firstpair.Value.CompareTo(nextpair.Value);
					});
					_isSorted = true;
					List<UInt64> ret = new List<UInt64>();
					foreach(KeyValuePair<UInt64, UInt64> pair in _used)
						ret.Add(pair.Key);
					return ret;
				}
			}
		}
		
		#endregion
	}
}
