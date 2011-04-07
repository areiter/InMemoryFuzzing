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
using System.IO;
using System.Text;
using System.Globalization;

namespace Iaik.Utils.IO
{

    /// <summary>
    /// Converts all written bytes to a hex-string stream and writes it to the underlying stream.
    /// On read 2 characters are read at once and converted back to a byte stream
    /// </summary>
	public class HexFilterStream : Stream
	{
		private Stream _baseStream;

		private StreamWriter _writer = null;
		private StreamReader _reader = null;

		public HexFilterStream (Stream baseStream)
		{
			_baseStream = baseStream;
			
			if(CanWrite)
				_writer = new StreamWriter(_baseStream);
				
			if(CanRead)
				_reader = new StreamReader(_baseStream);
		}
		
		
		public override bool CanRead 
		{
			get { return _baseStream.CanRead; }
		}
		
		public override bool CanWrite 
		{
			get { return _baseStream.CanWrite; }
		}
		
		public override bool CanSeek 
		{
			get { return _baseStream.CanSeek; }
		}
		
		public override long Length 
		{
			get { return _baseStream.Length / 2; }
		}

		public override long Position 
		{
			get { return _baseStream.Position / 2; }
			set { _baseStream.Position = value * 2; }
		}


		public override void Write (byte[] buffer, int offset, int count)
		{
			for(int i = offset; i<offset + count; i++)
			{
				_writer.Write(string.Format("{0:X2}", buffer[i]));
			}
			
			_writer.Flush();
		}

		public override int Read (byte[] buffer, int offset, int count)
		{
			int completeRead = 0;
		
			for(int i = 0; i<count; i++)
			{
				char[] chars = new char[2];
				int read = _reader.Read(chars,0,2);
				
				if(read < chars.Length)
					break;
				
				completeRead += read;

				buffer[i + offset] = byte.Parse(chars[0].ToString() + chars[1].ToString(), NumberStyles.AllowHexSpecifier);
			}
			
			return completeRead;
		}

		public override void Flush ()
		{
			_baseStream.Flush();
		}
	
		public override long Seek (long offset, SeekOrigin origin)
		{
			return _baseStream.Seek(offset*2, origin);
		}

		public override void SetLength (long value)
		{
			_baseStream.SetLength(value*2);
		}


		public override void Close ()
		{
			_baseStream.Close();
		}


        protected override void Dispose(bool disposing)
		{
			_baseStream.Dispose();
		}
	}
}
