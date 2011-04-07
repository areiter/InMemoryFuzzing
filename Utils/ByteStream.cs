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


// //
// //
// // Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// // Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;
using System.IO;

namespace Iaik.Utils
{
    /// <summary>
    /// Immutable Stream implementation for byte arrays
    /// </summary>
	public class ByteStream : Stream
	{
		/// <summary>
		/// Data to read from
		/// </summary>
		private byte[] _data;
		
		/// <summary>
		/// Current position in the stream(array)
		/// </summary>
		private int _currentPosition;
		
		public ByteStream (byte[] data)
		{
			_data = data;
		}
		
		#region Stream overrides
		public override bool CanRead 
		{
			get { return true;	}
		}

		public override bool CanWrite 
		{
			get {return false;}
		}
		
		public override bool CanSeek 
		{
			get { return true; }
		}

		public override long Length 
		{
			get{ return _data.Length;}
		}

		public override long Position 
		{			
			get { return _currentPosition; }
			set { Seek(value, SeekOrigin.Begin); }
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			int realReadLength = Math.Min(count, _data.Length - _currentPosition);			
			
			Array.Copy(_data, _currentPosition, buffer, offset, realReadLength);

			_currentPosition += realReadLength;
			return realReadLength;
		}
		
		
		public override void SetLength (long value)
		{
			throw new NotSupportedException();
		}

		public override void Flush ()
		{
			throw new NotImplementedException ();
		}

		public override void Write (byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException ();
		}

		public override int ReadByte ()
		{
			if(_currentPosition < _data.Length)
			{
				byte val = _data[_currentPosition];
				_currentPosition++;
				return val;
			}
			else
				return -1;
		}


		public override long Seek (long offset, SeekOrigin origin)
		{
			long newOffset;
			if(origin == SeekOrigin.Begin)
				newOffset = offset;
			else if(origin == SeekOrigin.Current)
				newOffset = _currentPosition + offset;
			else if(origin == SeekOrigin.End)
				newOffset = _data.Length - offset;
			else
				throw new ArgumentException("Unknown origin");
			
			if(newOffset >= _data.Length || newOffset < 0)
				throw new IOException("You can not seek out of the array!");
			
			_currentPosition = (int)newOffset;
			return _currentPosition;
		}
 
 
		#endregion
	}
}
