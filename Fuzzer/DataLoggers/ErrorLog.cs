// ErrorLog.cs
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
using Fuzzer.TargetConnectors;
using Iaik.Utils;
namespace Fuzzer.DataLoggers
{
	public class ErrorLog : IDataLogger
	{
		private string _prefix;
		private string _path;
		private Stream _logStream = null;
		
		public ErrorLog (string path)
		{
			_path = path;
		}
	

		#region IDataLogger implementation
		public void FinishedFuzzRun ()
		{
			if (_logStream != null) 
			{
				_logStream.Close ();
				_logStream = null;
			}
		}

		public void StartingFuzzRun ()
		{
			
		}

		public string Prefix 
		{
			get { return _prefix; }
			set { _prefix = value; }
		}
		#endregion
		
		
		public void LogDebuggerStop (IDebuggerStop debuggerStop)
		{
			if(_logStream == null)
			{
				string logFilename = BuildLogFile();
				_logStream = File.OpenWrite(logFilename);
			}
			
			StreamHelper.WriteInt32((int)debuggerStop.StopReason, _logStream);
			StreamHelper.WriteInt64(debuggerStop.Status, _logStream);
			StreamHelper.WriteUInt64(debuggerStop.Address, _logStream);
		}
		
		/// <summary>
		/// Builds the next Pipe logfile
		/// </summary>
		/// <returns></returns>
		private string BuildLogFile ()
		{
			string filename = "";
			
			if (_prefix != null && _prefix != String.Empty)
				filename = _prefix + ".errorlog";
			else
				filename = "current" + ".errorlog";
			
			return Path.Combine (_path, filename);
			
		}
}
}

