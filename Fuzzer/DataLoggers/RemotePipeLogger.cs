// RemotePipeLogger.cs
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
using Fuzzer.RemoteControl;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace Fuzzer.DataLoggers
{
	/// <summary>
	/// Logs all via RemoteControlProtocol received messages for the specified pipe names
	/// </summary>
	public class RemotePipeLogger : IDataLogger
	{
		/// <summary>
		/// Pipenames to log
		/// </summary>
		private List<string> _logPipeNames;
		
		private string _path;
		
		private string _prefix;
		
		private StreamWriter _sink = null;
		
		public RemotePipeLogger (RemoteControlProtocol p, string path, params string[] logPipeNames)
		{
			_logPipeNames = new List<string> (logPipeNames);
			_path = path;
			p.PipeData += HandlerPipeData;
		}

		private void HandlerPipeData (int pipeId, string pipeName, Byte[] data, int index, int offset)
		{
			if (_logPipeNames.Count == 0 || _logPipeNames.Contains (pipeName))
			{
				if (_sink != null)
				{
					_sink.Write (Encoding.ASCII.GetString (data, index, offset));
				}
			}			
		}
	

		#region IDataLogger implementation
		public void FinishedFuzzRun ()
		{
			_sink.Close ();
			_sink = null;
		}

		public void StartingFuzzRun ()
		{
			if (_sink != null)
				_sink.Close ();
			
			_sink = new StreamWriter (BuildLogFile ());
		}

		public string Prefix 
		{
			get { return _prefix; }
			set { _prefix = value; }
		}
		#endregion
		
		/// <summary>
		/// Builds the next Pipe logfile
		/// </summary>
		/// <returns></returns>
		private string BuildLogFile ()
		{
			string filename = "";
			
			StringBuilder concatPipes = new StringBuilder ();
			foreach (string pipeName in _logPipeNames)
			{
				if (concatPipes.Length > 0)
					concatPipes.Append ("_");
				concatPipes.Append (pipeName);
			}
			
			string sConcatPipes = "";
			if (concatPipes.Length > 0)
				sConcatPipes = "." + concatPipes.ToString ();
			
			if (_prefix != null && _prefix != String.Empty)
				filename = _prefix + sConcatPipes + ".pipes";
			else
				filename = "current" + sConcatPipes + ".pipes";
			
			return Path.Combine (_path, filename);
			
		}
}
}

