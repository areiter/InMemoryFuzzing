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
using Iaik.Utils.Serialization;
using System.IO;

namespace Iaik.Utils
{

	/// <summary>
	/// Provides the ability to access individual bits
	/// and export them as a byte array.
	/// </summary>
	public class BitMap:AutoStreamSerializable
	{
		/// <summary>
		/// Contains the current state of the bitmap
		/// </summary>
		[SerializeMe(0)]
		private byte[] _data;

		/// <summary>
		/// Returns the raw data
		/// </summary>
		public byte[] Data
		{
			get{ return _data; }
		}
		
		/// <summary>
		/// Returns the number of usable bits in this bit map
		/// </summary>
		public int BitCount
		{
			get{ return _data.Length * 8;}
		}
		
		/// <summary>
		/// The bit size ceiled to a multiple of 8
		/// </summary>
		/// <param name="bitsize"></param>
		public BitMap (int bitsize)
		{
			if((bitsize % 8) > 0)
				bitsize = ((bitsize % 8) + 1) * 8;
			
			//Initialization automatically occurs to zero
			_data = new byte[bitsize / 8];			
		}
		
		public BitMap (byte[] selectionBits)
		{
			_data = selectionBits;
		}
		
		public BitMap(Stream src)
		{
			Read(src);
		}
		
		/// <summary>
		/// Sets the whole bitmap to the specified value
		/// </summary>
		/// <param name="value"></param>
		public void SetBitmap(bool value)
		{
			for(int i = 0; i<_data.Length; i++)
				_data[i] = value?(byte)0xff:(byte)0;
		}
		
		/// <summary>
		/// Sets the specified bit to the specified value
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public void SetBit(int index, bool value)
		{
			int byteIndex = index / 8;
			int bitIndex = index % 8;
			
		
			if(value)
				_data[byteIndex] = (byte)(_data[byteIndex] | (1<<bitIndex));
			else
				_data[byteIndex] = (byte)(_data[byteIndex] & (0xfe<<bitIndex));
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public bool GetBit(int index)
		{
			int byteIndex = index / 8;
			int bitIndex = index % 8;
			
			if(((_data[byteIndex] >> bitIndex) & 0x01) != 0)
				return true;
			else
				return false;
		}
		
		public bool this[int index]
		{
			get
			{
				return GetBit(index);
			}
			set
			{
				SetBit(index, value);
			}
		}
	}
}
