// StackFrameLogger.cs
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
using Fuzzer.TargetConnectors;
using System.IO;
using Iaik.Utils;
namespace Fuzzer.DataLoggers
{
	/// <summary>
	/// Retrieves the current stackframe info from the connector
	/// and writes it to disk
	/// </summary>
	public class StackFrameLogger : IDataLogger
	{
		private string _prefix;
		private string _path;
		private ITargetConnector _connector;
		
		public StackFrameLogger (ITargetConnector connector, string path)
		{
			_connector = connector;
			_path = path;
		}
	

		#region IDataLogger implementation
		public void FinishedFuzzRun ()
		{
		}

		public void StartingFuzzRun ()
		{
			IStackFrameInfo stackFrameInfo = _connector.GetStackFrameInfo ();
			
			string logFile = BuildLogFile ();
			
			if (File.Exists (logFile))
				File.Delete (logFile);
			
			using (Stream logStream = File.OpenWrite (logFile))
			{
				if (stackFrameInfo != null)
					StreamHelper.WriteTypedStreamSerializable (stackFrameInfo, logStream);
			}
					
		}

		public string Prefix {
			get { return _prefix; }
			set { _prefix = value; }
		}
		#endregion
		
		/// <summary>
		/// Builds the next stack frame log file
		/// </summary>
		/// <returns></returns>
		private string BuildLogFile ()
		{
			string filename = "";
			
			if (_prefix != null && _prefix != String.Empty)
				filename = _prefix + ".stackframeinfo";
			else
				filename = "current.stackframeinfo";
			
			return Path.Combine (_path, filename);
			
		}
}
}

