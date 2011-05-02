// OneTimeStream.cs
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
using System.IO;
using System.Collections.Generic;
using System.Threading;
namespace Iaik.Utils.IO
{
	/// <summary>
	/// Implements a stream where you write to like it's a memory stream
	/// and can read the stream once. The data that has been read is removed from memory
	/// </summary>
	public class OneTimeStream : Stream
	{
		/// <summary>
		/// Chunk size
		/// </summary>
		private int _bufferSize = 4096;
		
		private int _readPosition = 0;
		private int _readIndex = 0;
		private int _writePosition = 0;
		private AutoResetEvent _evt = new AutoResetEvent(false);
		
		
		private List<byte[]> _buffers = new List<byte[]>();
		
		
		#region implemented abstract members of System.IO.Stream
				
		
		
		public override int Read (byte[] buffer, int offset, int count)
		{
			try
			{
				Monitor.Enter(_buffers);
				if(_buffers.Count == 0 || 
				   (_readIndex + 1 == _buffers.Count && _readPosition >= _writePosition))
				{
					_evt.Reset();
					Monitor.Exit(_buffers);
					_evt.WaitOne();
					Monitor.Enter(_buffers);
				}
				
				int read = 0;
				
				try
				{
					do
					{
						if(_readPosition >= _buffers[_readIndex].Length)
						{
							_readIndex++;
							_readPosition = 0;
						}
						
						if(_readIndex < _buffers.Count )
						{
							Array.Copy(_buffers[_readIndex], _readPosition, buffer, offset + read, 
						           Math.Min(count-read, _buffers[_readIndex].Length - _readPosition));
							
							int currentBufferLength = _buffers[_readIndex].Length;
							
							if(_readIndex == _buffers.Count - 1)
								currentBufferLength = _writePosition;
							
							int currentread = Math.Min(count-read, currentBufferLength - _readPosition);
							read += currentread;
							_readPosition += currentread;
						}
						else
							return read;
						
						if(_readIndex == _buffers.Count - 1 && _readPosition == _writePosition)
							return read;
							
					}while(read < count && _readIndex < _buffers.Count);
				}
				finally
				{
					//Delete Buffers;
					if(_readIndex > 0)
					{
						_buffers.RemoveRange(0,_readIndex);
						_readIndex = 0;
					}
				}
				
				return read;
					
					
			}
			finally
			{
				Monitor.Exit(_buffers);
			}
		}
		
		public override void Write (byte[] buffer, int offset, int count)
		{
			lock(_buffers)
			{
				int written = 0;
				
				do
				{
					if(_buffers.Count == 0 || _writePosition >= _buffers[0].Length)
					{
						CreateNewBuffer();
						_writePosition = 0;
					}
					
					Array.Copy(buffer, offset+written, _buffers[_buffers.Count -1], _writePosition,
					           Math.Min(count - written, _buffers[_buffers.Count - 1].Length - _writePosition));
					int currentwritten = Math.Min(count - written, _buffers[_buffers.Count - 1].Length - _writePosition);
					written += currentwritten;
					_writePosition += currentwritten;
					
				} while(written < count);
				
				_evt.Set();
				
			}
		}
		
		
		public override void Flush ()
		{
			
		}
		
		
		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new System.NotImplementedException();
		}
		
		
		public override void SetLength (long value)
		{
			throw new System.NotImplementedException();
		}
		
		
		
		
		
		public override bool CanRead 
		{
			get { return true; }
		}
		
		
		public override bool CanSeek 
		{
			get {return false;}
		}
		
		
		public override bool CanWrite 
		{
			get { return true; }
		}
		
		
		public override long Length {
			get {
				throw new System.NotImplementedException();
			}
		}
		
		
		public override long Position {
			get {
				throw new System.NotImplementedException();
			}
			set {
				throw new System.NotImplementedException();
			}
		}		
		#endregion
		
		private void CreateNewBuffer()
		{
			byte[] newBuffer = new byte[this._bufferSize];
			
			_buffers.Add(newBuffer);
		}
		
		public OneTimeStream ()
		{
		}
	}
}

