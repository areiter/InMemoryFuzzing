// MultiTextReader.cs
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
using System.Threading;
using System.Text;
using System.Collections.Generic;
namespace Iaik.Utils.IO
{
	public class MultiTextReader : TextReader, IDisposable
	{
		
		
		private StreamReader _reader;
		
		/// <summary>
		/// Write from multiple sources and readout once, without consuming memory 
		/// for already read data anymore
		/// </summary>
		private OneTimeStream _src = new OneTimeStream();
		
		private List<Thread> _threads = new List<Thread>();
		private volatile bool _disposed = false;
		
		private int _val = 0;
		
		public MultiTextReader (params TextReader[] readers)
		{
			_reader = new StreamReader(_src);
			
			
			foreach(TextReader reader in readers)
			{
				Thread t = new Thread(ReadThread);
				t.Start(reader);
				_threads.Add(t);
			}
			
		}
		
		
		private void ReadThread(object oReader)
		{
			int id = _val++;
			
			TextReader reader = oReader as TextReader;
			
			char[] buffer = new char[4096];
			while(!_disposed)
			{			
				int read = reader.Read(buffer, 0, buffer.Length);
				lock(_src)
				{
					
					byte[] data = Encoding.UTF8.GetBytes(buffer, 0, read);
					_src.Write(data, 0, data.Length);
				}
			}
		}
		
		#region IDisposable implementation
		new public void Dispose ()
		{
			base.Dispose();
			
			_disposed = true;
			
			
			foreach(Thread t in _threads)
				t.Abort();
			
			_threads.Clear();
		}
		#endregion		
		
		public override int Peek ()
		{
			return _reader.Peek();
		}
		
		public override int Read ()
		{
			return _reader.Read();
		}
		
		public override int Read (char[] buffer, int index, int count)
		{
			return _reader.Read (buffer, index, count);
		}
		
		public override int ReadBlock (char[] buffer, int index, int count)
		{
			return _reader.ReadBlock (buffer, index, count);
		}
		
		public override string ReadLine ()
		{
			return _reader.ReadLine ();
		}
		
		public override string ReadToEnd ()
		{
			return _reader.ReadToEnd ();
		}
	}
}

