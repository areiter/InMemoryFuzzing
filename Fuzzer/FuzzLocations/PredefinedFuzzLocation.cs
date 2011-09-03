// PredefinedFuzzLocation.cs
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
using System.Xml;
using Fuzzer.TargetConnectors;
using Iaik.Utils.CommonAttributes;
using Fuzzer.DataLoggers;
using Fuzzer.FuzzDescriptions;
using System.Collections.Generic;
using Iaik.Utils;
namespace Fuzzer.FuzzLocations
{
	[ClassIdentifier("fuzzer/predefined")]
	public class PredefinedFuzzLocation : IFuzzLocation
	{
		private IFuzzLocation _childLocation = null;
		private object _changeableId = null;	

		#region IFuzzLocation implementation
		
		public void ApplyChangeableId (object id)
		{
			_childLocation.ApplyChangeableId (id);
		}
		
		public bool IsTriggered (TriggerEnum trigger, object data)
		{
			_childLocation.ApplyChangeableId (_changeableId);
			return _childLocation.IsTriggered (trigger, data);
		}

		public void SetLogger (LoggerDestinationEnum loggerDestination, IDataLogger logger)
		{
			_childLocation.ApplyChangeableId (_changeableId);
			_childLocation.SetLogger (loggerDestination, logger);
		}

		public void NextFuzzRun ()
		{
			_childLocation.ApplyChangeableId (_changeableId);
			_childLocation.NextFuzzRun ();
		}

		public void Init (XmlElement fuzzLocationRoot, ITargetConnector connector, Dictionary<string, IFuzzLocation> predefinedFuzzers)
		{
			IDictionary<string, string> config = DictionaryHelper.ReadDictionaryXml (fuzzLocationRoot, "FuzzerArg");
			
			string id = DictionaryHelper.GetString ("id", config, null);
			if (!predefinedFuzzers.ContainsKey (id))
				throw new ArgumentException (string.Format ("Could not find fuzzer with id '{0}'", id));
			
			_childLocation = predefinedFuzzers[id];
		}

		public object InitChanges (XmlElement fuzzLocationRoot)
		{
			_changeableId = _childLocation.InitChanges (fuzzLocationRoot);
			return _changeableId;
		}

		public void Run (FuzzController ctrl)
		{
			_childLocation.ApplyChangeableId (_changeableId);
			_childLocation.Run (ctrl);			
		}

		public bool IsFinished 
		{
			get 
			{
				_childLocation.ApplyChangeableId (_changeableId);
				return _childLocation.IsFinished;
			}
		}
		#endregion

		#region IDisposable implementation
		public void Dispose ()
		{
			_childLocation.Dispose ();
		}
		#endregion
}
}

