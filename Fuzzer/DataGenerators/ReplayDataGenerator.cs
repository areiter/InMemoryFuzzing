// ReplayDataGenerator.cs
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
using Iaik.Utils.IO;
namespace Fuzzer.DataGenerators
{
	/// <summary>
	/// Replays a previously recorded data log
	/// </summary>
	/// <remarks>
	/// Replaylog file looks as follows.
	/// 
	/// [DATETIME] command options...
	/// {
	///  [DATETIME] status 1 (success)
	///  [DATETIME] status 0 some error info
	/// }
	/// 
	/// After each command line a statusline follows
	/// The status line needs to be read outside
	/// </remarks>
	public class ReplayDataGenerator : IDataGenerator
	{
		private StreamReader _reader;
		
		public ReplayDataGenerator (StreamReader reader)
		{
			_reader = reader;
		}
	

		#region IDataGenerator implementation
		public byte[] GenerateData ()
		{
			string line = _reader.ReadLine ();
			
			if (line == null)
				throw new EndOfStreamException ("The end of the replay log has been reached");
			
		    string commandAndData = line.Substring (line.IndexOf (']') + 1).Trim ();
			string[] aCommandAndData = commandAndData.Split (' ', 2);
			
			using (Stream baseStream = new TextReaderStream (new StringReader (aCommandAndData[1])))
			{
				using (Stream hexStream = new HexFilterStream (baseStream))
				{
					byte[] buffer = new byte[hexStream.Length];
					hexStream.Read (buffer, 0, buffer.Length);
					return buffer;
				}
			}
		}
		#endregion
}
}

