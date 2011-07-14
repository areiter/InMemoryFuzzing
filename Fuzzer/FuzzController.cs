// FuzzController.cs
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
using Fuzzer.FuzzDescriptions;
using System.Collections.Generic;
using Fuzzer.DataLoggers;
using System.IO;
using log4net;
namespace Fuzzer
{
	/// <summary>
	/// Controls the fuzzing process and reports exceptions as they occur
	/// </summary>
	public class FuzzController
	{
		protected ITargetConnector _connector;
		protected IBreakpoint _snapshotBreakpoint;
		protected ISnapshot _snapshot;
		protected IBreakpoint _restoreBreakpoint;
		protected IDataLogger _dataLogger;
		protected ErrorLog _errorLog = null;
		protected string _logDestination;
		protected Queue<IFuzzDescription> _fuzzDescriptions = new Queue<IFuzzDescription>(); 
			
		protected ILog _log = LogManager.GetLogger("FuzzController");
		
		public ITargetConnector Connector
		{
			get { return _connector; }
		}
		
		/// <summary>
		/// Creates a new FuzzController. Once the snapshotBreakpoint is reached a snapshot is created.
		/// The snapshot gets restored once restore Breakpoint is reached
		/// </summary>
		/// <param name="connector">connector to use</param>
		/// <param name="snapshotBreakpoint">Location to create a snapshot</param>
		/// <param name="restoreBreakpoint">Location to restore the snapshot</param>
		public FuzzController (ITargetConnector connector, IBreakpoint snapshotBreakpoint, IBreakpoint restoreBreakpoint, string logDestination,
			IDataLogger logger, params IFuzzDescription[] fuzzDescriptions)
		{
			_connector = connector;
			_snapshotBreakpoint = snapshotBreakpoint;
			_snapshot = null;
			_restoreBreakpoint = restoreBreakpoint;
			_dataLogger = logger;
			_logDestination = logDestination;
			
			InitFuzzDescriptions (fuzzDescriptions);
		}
		
		/// <summary>
		/// Creates a new FuzzController. 
		/// The snapshot already gets created outside and gets restored once the restore Breakpoint is reached
		/// </summary>
		/// <param name="connector">connector to use</param>
		/// <param name="snapshot">The snapshot to restore once restore Breakpoint is reachead</param>
		/// <param name="restoreBreakpoint">Location to restore the snapshot</param>
		public FuzzController (ITargetConnector connector, ISnapshot snapshot, IBreakpoint restoreBreakpoint, string logDestination,
			IDataLogger logger, params IFuzzDescription[] fuzzDescriptions)
		{
			_connector = connector;
			_snapshotBreakpoint = null;
			_snapshot = snapshot;
			_restoreBreakpoint = restoreBreakpoint;
			_dataLogger = logger;
			_logDestination = logDestination;
			
			_errorLog = new ErrorLog (_logDestination);
			InitFuzzDescriptions (fuzzDescriptions);
		}
		
		public void Fuzz ()
		{
			if (Directory.Exists (_logDestination) == false)
				Directory.CreateDirectory (_logDestination);
			
			int loggerPrefix = 0;
			string morePrefix = DateTime.Now.ToString ("dd.MM.yyyy");
			IncrementLoggerPrefix (ref loggerPrefix, morePrefix);
			
			while (true)
			{
				
				//Is the snapshot already reached and active? Create one if it does not exist
				if (_snapshot == null && _connector.LastDebuggerStop != null &&
					_snapshotBreakpoint.Address == _connector.LastDebuggerStop.Address && 
					_connector.LastDebuggerStop.StopReason == StopReasonEnum.Breakpoint)
				{
					_dataLogger.StartingFuzzRun ();
					_snapshot = _connector.CreateSnapshot ();
				}
				
				//The restore breakpoint is reached.....do the restore
				if (_snapshot != null && _connector.LastDebuggerStop.StopReason == StopReasonEnum.Breakpoint && _restoreBreakpoint.Address == _connector.LastDebuggerStop.Address)
				{
					_log.InfoFormat ("Restore snapshot for prefix #{0}, no error ", loggerPrefix);
					RestoreAndFuzz (ref loggerPrefix, morePrefix);
				}
				else if (_snapshot != null && _connector.LastDebuggerStop.StopReason != StopReasonEnum.Breakpoint)
				{
					_log.InfoFormat ("Restore snapshot for prefix #{0}, error ", loggerPrefix);
					_errorLog.LogDebuggerStop (_connector.LastDebuggerStop);

					RestoreAndFuzz (ref loggerPrefix, morePrefix);
				}
				
				_log.InfoFormat ("Starting fuzz with prefix #{0}", loggerPrefix);
				_connector.DebugContinue ();
			}
		}
		
		private void RestoreAndFuzz (ref int loggerPrefix, string morePrefix)
		{
			_dataLogger.FinishedFuzzRun ();
			_snapshot.Restore ();

			IncrementLoggerPrefix (ref loggerPrefix, morePrefix);			
			_dataLogger.StartingFuzzRun ();
			
			IFuzzDescription currentDescription = _fuzzDescriptions.Dequeue ();
			currentDescription.Run (ref _snapshot);
			_fuzzDescriptions.Enqueue (currentDescription);
		}
		
		private void IncrementLoggerPrefix (ref int loggerPrefix, string morePrefix)
		{
			loggerPrefix++;
			_dataLogger.Prefix = string.Format ("{0}", loggerPrefix, morePrefix);
		}
		
		private void InitFuzzDescriptions (IFuzzDescription[] fuzzDescriptions)
		{
			foreach (IFuzzDescription desc in fuzzDescriptions)
			{
				desc.Init (this);
				_fuzzDescriptions.Enqueue (desc);
			}
		}
	}
}

