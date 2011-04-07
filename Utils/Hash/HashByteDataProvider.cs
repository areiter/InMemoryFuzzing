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



using System;
using Iaik.Utils.Serialization;
using System.IO;

namespace Iaik.Utils.Hash
{

	/// <summary>
	/// Provides data for hasher for byte arrays
	/// </summary>
	[TypedStreamSerializable("h_byte_dp")]
	public class HashByteDataProvider : HashDataProvider
	{
		
		/// <summary>
		/// Data to read from
		/// </summary>
		[SerializeMe(0)]
		private byte[] _data;
		
		/// <summary>
		/// CurrentIndex in the data bytes
		/// </summary>
		[SerializeMe(1)]
		private int _currentIndex;
		
		/// <summary>
		/// length to read
		/// </summary>
		[SerializeMe(2)]
		private int _length;
		
		public HashByteDataProvider (byte[] data)
			:this(data, null, null)
		{
		}
		
		public HashByteDataProvider (byte[] data, int? startIndex, int? length)
		{
			_data = data;
			
			if (startIndex == null)
				_currentIndex = 0;
			else
				_currentIndex = startIndex.Value;
			
			if (length == null)
				_length = data.Length - _currentIndex;
			else
				_length = length.Value;
			
			
		}
		
		public HashByteDataProvider(Stream src)
		{
			Read(src);
		}
		
		public override int NextBytes (byte[] buffer)
		{
			if (buffer == null)
				throw new ArgumentNullException ("buffer", "cannot be null");
			
			if (buffer.Length == 0)
				throw new ArgumentException ("Short buffer");
			
			if (_length <= 0)
				return 0;
			
			int toRead = Math.Min (buffer.Length, _length);
			
			Array.Copy (_data, _currentIndex, buffer, 0, toRead);
			
			_length -= toRead;
			
			return toRead;
		}

	}
}
