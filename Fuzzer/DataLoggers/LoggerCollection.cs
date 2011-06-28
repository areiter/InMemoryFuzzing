// LoggerCollection.cs
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
using System.Collections.Generic;
namespace Fuzzer.DataLoggers
{
	/// <summary>
	/// Redirects the method calls to all added sub loggers
	/// </summary>
	public class LoggerCollection : IDataLogger
	{
		private List<IDataLogger> _loggers;
		
		private string _prefix = "";
		
		public LoggerCollection (params IDataLogger[] loggers)
		{
			_loggers = new List<IDataLogger> (loggers);
		}
	

		#region IDataLogger implementation
		public void FinishedFuzzRun ()
		{
			foreach (IDataLogger logger in _loggers)
				logger.FinishedFuzzRun ();
		}

		public void StartingFuzzRun ()
		{
			foreach (IDataLogger logger in _loggers)
				logger.StartingFuzzRun ();

		}

		public string Prefix 
		{
			get { return _prefix; }
			set 
			{
				_prefix = value;
				
				foreach (IDataLogger logger in _loggers)
					logger.Prefix = value;

			}
		}
		#endregion
}
}

