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
using Fuzzer.DataLoggers;
using Iaik.Utils;
using System.Collections.Generic;
using Iaik.Utils.CommonAttributes;

namespace Fuzzer.DataGenerators
{
	/// <summary>
	/// Replays a previously recorded data log
	/// </summary>
	/// <remarks>
	/// Replaylog file looks as follows.
	/// 
	/// {[<int32 data length>,<datalength bytes],...}
	/// 
	/// A single replay log file can contain multiple input data streams.
	/// e.g. if multiple memory locations are fuzzed. 
	/// They are read in the same order than they where assigned in the first run, 
	/// so it is up to the user that each variable receive the correct data stream
	/// </remarks>
	[ClassIdentifier("datagen/replay")]
	public class ReplayDataGenerator : IDataGenerator, IDataLogger
	{
		private Stream _logStream = null;
		private string _path;
		private string _prefix;
		
		public ReplayDataGenerator (string path)
		{
			_path = path;
		}
	
		public ReplayDataGenerator ()
		{
		}

		#region IDataGenerator implementation
		public void Setup(IDictionary<string, string> config)
		{
			_path = DictionaryHelper.GetString("path", config, "./");	
		}
		
		public void SetLogger(DataGeneratorLogger logger)
		{
		}
		
		public byte[] GenerateData ()
		{
			if (_logStream == null)
				return null;
			
			return StreamHelper.ReadBytesSafe (_logStream);
		}
		#endregion
		
		#region IDataLogger implementation
		public void FinishedFuzzRun ()
		{
			//Close the current Stream
			CloseLogStream ();
		}

		public void StartingFuzzRun ()
		{
			CloseLogStream ();
			OpenLogStream ();
		}

		public string Prefix 
		{
			get { return _prefix; }
			set { _prefix = value; }
		}
		#endregion
		
		private void OpenLogStream ()
		{
			string logFile = BuildLogFile ();
			if (File.Exists (logFile))
				_logStream = File.OpenRead (logFile);
			else
				_logStream = null;
		}
		
		private void CloseLogStream ()
		{
			if (_logStream != null)
			{
				_logStream.Close ();
				_logStream = null;
			}
		}
		
		/// <summary>
		/// Builds the next stack frame log file
		/// </summary>
		/// <returns></returns>
		private string BuildLogFile ()
		{
			string filename = "";
			
			if (_prefix != null && _prefix != String.Empty)
				filename = _prefix + ".fuzzdata";
			else
				filename = "current.fuzzdata";
			
			return Path.Combine (_path, filename);
			
		}
	}
}

