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
using Fuzzer.FuzzLocations;
using System.Threading;
namespace Fuzzer
{
	/// <summary>
	/// Controls the fuzzing process and reports exceptions as they occur
	/// </summary>
	public class FuzzController
	{
		protected ITargetConnector _connector;
		protected ISnapshot _snapshot;
		protected IDataLogger _dataLogger;
		protected ErrorLog _errorLog = null;
		protected string _logDestination;
		protected FuzzDescription _fuzzDescription;
			
		protected ILog _log = LogManager.GetLogger("FuzzController");
		
		protected IFuzzLocation[] _preConditions;
		
		public ITargetConnector Connector
		{
			get { return _connector; }
		}
		
		/// <summary>
		/// Gets the current snapshot, for some fuzz descriptions it may be required to change/recreate the snapshot
		/// </summary>
		public ISnapshot Snapshot
		{
			get { return _snapshot; }
			set { _snapshot = value;}
		}
		
		/// <summary>
		/// Creates a new FuzzController. Once the snapshotBreakpoint is reached a snapshot is created.
		/// The snapshot gets restored once restore Breakpoint is reached
		/// </summary>
		/// <param name="connector">connector to use</param>
		/// <param name="snapshotBreakpoint">Location to create a snapshot</param>
		/// <param name="restoreBreakpoint">Location to restore the snapshot</param>
		public FuzzController (ITargetConnector connector, string logDestination,
			IDataLogger logger, FuzzDescription fuzzDescription, IFuzzLocation[] preConditions)
		{
			_connector = connector;
			_snapshot = null;
			_dataLogger = logger;
			_logDestination = logDestination;
			
			_errorLog = new ErrorLog (_logDestination);
			
			_fuzzDescription = fuzzDescription;
			_fuzzDescription.Init ();
			
			_preConditions = preConditions;
		}
		
		/// <summary>
		/// Creates a new FuzzController. 
		/// The snapshot already gets created outside and gets restored once the restore Breakpoint is reached
		/// </summary>
		/// <param name="connector">connector to use</param>
		/// <param name="snapshot">The snapshot to restore once restore Breakpoint is reachead</param>
		/// <param name="restoreBreakpoint">Location to restore the snapshot</param>
		public FuzzController (ITargetConnector connector, ISnapshot snapshot, string logDestination,
			IDataLogger logger, FuzzDescription fuzzDescription, IFuzzLocation[] preConditions)
		{
			_connector = connector;
			_snapshot = snapshot;
			_dataLogger = logger;
			_logDestination = logDestination;
			
			_errorLog = new ErrorLog (_logDestination);
			
			_fuzzDescription = fuzzDescription;
			_fuzzDescription.Init ();
			
			_preConditions = preConditions;
		}
		
		public void Fuzz ()
		{
			if (Directory.Exists (_logDestination) == false)
				Directory.CreateDirectory (_logDestination);
			
			
			//Invoke the pre conditions
			ThreadPool.QueueUserWorkItem (new WaitCallback (
				delegate(object state) 
				{
				if (_preConditions != null)
					{
						foreach (IFuzzLocation preCondition in _preConditions)
						{
							preCondition.Run (this);
						}
					}
				}), null);
			
			int loggerPrefix = 0;
			string morePrefix = DateTime.Now.ToString ("dd.MM.yyyy");
			IncrementLoggerPrefix (ref loggerPrefix, morePrefix);
			
			while (_fuzzDescription.FuzzLocation.Count > 0)
			{
				
				//Is the snapshot already reached and active? Create one if it does not exist
				if (_snapshot == null && _connector.LastDebuggerStop != null &&
					_fuzzDescription.SnapshotBreakpoint.Address == _connector.LastDebuggerStop.Address && 
					_connector.LastDebuggerStop.StopReason == StopReasonEnum.Breakpoint)
				{
					_dataLogger.StartingFuzzRun ();
					_snapshot = _connector.CreateSnapshot ();
				}
				
				//The restore breakpoint is reached.....do the restore
				if (_snapshot != null && _connector.LastDebuggerStop.StopReason == StopReasonEnum.Breakpoint && _fuzzDescription.RestoreBreakpoint.Address == _connector.LastDebuggerStop.Address)
				{
					_log.InfoFormat ("Restore snapshot for prefix #{0}, no error ", loggerPrefix);
					RestoreAndFuzz (ref loggerPrefix, morePrefix);
				}
				
				//Another breakpoint is reached, program exited or terminated, not for trigger breakpoints
				else if (_snapshot != null && _connector.LastDebuggerStop.StopReason != StopReasonEnum.Breakpoint)
				{
					//_log.InfoFormat ("Restore snapshot for prefix #{0}, error ", loggerPrefix);
					_errorLog.LogDebuggerStop (_connector.LastDebuggerStop);

					RestoreAndFuzz (ref loggerPrefix, morePrefix);
					//InvokeFuzzLocations(TriggerEnum.Location, _connector.LastDebuggerStop.Address);
				}
				
				_log.InfoFormat ("Starting fuzz with prefix #{0}", loggerPrefix);
				_connector.DebugContinue ();
			}
		}
		
		private void RestoreAndFuzz (ref int loggerPrefix, string morePrefix)
		{
			InvokeFuzzLocations (TriggerEnum.End, null);
			_dataLogger.FinishedFuzzRun ();
			_snapshot.Restore ();

			IncrementLoggerPrefix (ref loggerPrefix, morePrefix);
			_dataLogger.StartingFuzzRun ();
			
			_fuzzDescription.NextFuzzRun ();
			
			InvokeFuzzLocations (TriggerEnum.Start, null);			
		}
		
		private void InvokeFuzzLocations (TriggerEnum triggerType, object triggerData)
		{
			List<IFuzzLocation> fuzzLocationsToRemove = new List<IFuzzLocation> ();
			foreach (IFuzzLocation fuzzLocation in _fuzzDescription.FuzzLocation) {
				if (fuzzLocation.IsFinished)
					fuzzLocationsToRemove.Add (fuzzLocation); else if (fuzzLocation.IsTriggered (triggerType, triggerData)) {
					fuzzLocation.Run (this);
				}
			}
			
			foreach (IFuzzLocation toRemove in fuzzLocationsToRemove)
				_fuzzDescription.FuzzLocation.Remove (toRemove);
		}
		
		private void IncrementLoggerPrefix (ref int loggerPrefix, string morePrefix)
		{
			loggerPrefix++;
			_dataLogger.Prefix = string.Format ("{0}", loggerPrefix, morePrefix);
		}
	}
}

