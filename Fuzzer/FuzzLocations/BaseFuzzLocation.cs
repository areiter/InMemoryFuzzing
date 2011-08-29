// BaseFuzzLocation.cs
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
using Fuzzer.FuzzDescriptions;
using System.Xml;
using Fuzzer.DataGenerators;
using Iaik.Utils;
using Iaik.Utils.CommonFactories;
using System.Collections.Generic;
using Fuzzer.TargetConnectors;
using Fuzzer.DataLoggers;
namespace Fuzzer.FuzzLocations
{
	/// <summary>
	/// Provides basic functionality for derived classes. It is not required to derive from this class.
	/// </summary>
	public abstract class BaseFuzzLocation : IFuzzLocation
	{
		public class ChangeableComponents
		{
			public IFuzzStopCondition StopCondition;
			public IDataGenerator DataGenerator;
			public ITrigger[] Triggers;
			public bool IsPreCondition;
			
			public ChangeableComponents (IFuzzStopCondition stopCondition,
				IDataGenerator dataGenerator,
				ITrigger[] triggers,
				bool isPreCondition)
			{
				StopCondition = stopCondition;
				DataGenerator = dataGenerator;
				Triggers = triggers;
				IsPreCondition = isPreCondition;
			}
			
		}
		
		protected List<ChangeableComponents> _changeableComponents = new List<ChangeableComponents>();
		
		/// <summary>
		/// Associated connector
		/// </summary>
		protected ITargetConnector _connector = null;
		
		/// <summary>
		/// Specifies the stop condition of this fuzz location
		/// </summary>
		protected IFuzzStopCondition _stopCondition = null;
		
		/// <summary>
		/// Datagenerator of the fuzz location if supported
		/// </summary>
		protected IDataGenerator _dataGenerator = null;
		
		/// <summary>
		/// Contains all location triggers
		/// </summary>
		protected ITrigger[] _triggers = null;
		
		/// <summary>
		/// Determines if the current fuzz location is a precondition (no logging or snapshots)
		/// </summary>
		protected bool _isPreCondition = false;
		
		/// <summary>
		/// Returns if the implementing class expects an [StopCondition] tag
		/// </summary>
		protected abstract bool SupportsStopCondition { get; }
		
		/// <summary>
		/// Returns if the implementing class expects an [DataGenerator] Tag and its arguments
		/// </summary>
		protected abstract bool SupportsDataGen { get; }
		
		/// <summary>
		/// Returns if the implementing class expects one or more [Trigger] Tags
		/// </summary>
		protected abstract bool SupportsTrigger { get; }
		
		#region IFuzzLocation implementation
		public virtual void Init (XmlElement fuzzLocationRoot, ITargetConnector connector, Dictionary<string, IFuzzLocation> predefinedFuzzers)
		{
			_connector = connector;			
		}
		
		public object InitChanges (XmlElement fuzzLocationRoot)
		{
			_isPreCondition = fuzzLocationRoot.Name == "PreCondition";
			
			if (SupportsStopCondition)
				ReadStopCondition (fuzzLocationRoot);
			else
				_stopCondition = null;
			
			if (SupportsDataGen)
				ReadDataGen (fuzzLocationRoot);
			else
				_dataGenerator = null;
			
			if (SupportsTrigger)
				ReadTriggers (fuzzLocationRoot, _connector);
			else
				_triggers = null;
			
			ChangeableComponents changeableComponent = new ChangeableComponents (
				_stopCondition, _dataGenerator, _triggers, _isPreCondition);
			_changeableComponents.Add (changeableComponent);
			
			InternInitChanges (changeableComponent, fuzzLocationRoot);
			
			return changeableComponent;
		}
		
		protected virtual void InternInitChanges (object changeableId, XmlElement fuzzLocationRoot)
		{
			
		}
		
		private void ReadStopCondition (XmlElement root)
		{
			string stopCondition = XmlHelper.ReadString (root, "StopCondition");
			if (stopCondition == null || stopCondition == string.Empty || stopCondition.Equals ("none"))
				_stopCondition = null;
			else {
				string[] stopConditionParts = stopCondition.Split (new char[] { '|' }, 2);
				if (stopConditionParts[0].Equals ("count", StringComparison.InvariantCultureIgnoreCase) && stopConditionParts.Length == 2)
					_stopCondition = new CountFuzzStopCondition (int.Parse (stopConditionParts[1]));
				else
					throw new NotImplementedException (string.Format ("Invalid stop condition identifier '{0}'", stopCondition));
			}
		}

		private void ReadDataGen (XmlElement root)
		{
			IDataGenerator dataGen = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<IDataGenerator> (XmlHelper.ReadString (root, "DataGenerator"));
			IDictionary<string, string> arguments = new Dictionary<string, string> ();
			foreach (XmlElement datagenArgNode in root.SelectNodes ("DataGenArg"))
				arguments.Add (datagenArgNode.GetAttribute ("name"), datagenArgNode.InnerText);
			dataGen.Setup (arguments);
			
			_dataGenerator = dataGen;
		}
		
		private void ReadTriggers (XmlElement root, ITargetConnector connector)
		{
			List<ITrigger> triggers = new List<ITrigger> ();
			foreach (XmlElement triggerNode in root.SelectNodes ("Trigger"))
			{
				string[] splittedTrigger = triggerNode.InnerText.Split (new char[] { '|' }, 2);
				
				string triggerType = splittedTrigger[0];
				string triggerArgs = null;
				
				if (splittedTrigger.Length > 1)
					triggerArgs = splittedTrigger[1];
				triggers.Add (new Trigger (triggerType, triggerArgs, connector));
			}
			
			_triggers = triggers.ToArray ();
		}
		
		public virtual void SetLogger (LoggerDestinationEnum loggerDestination, IDataLogger logger)
		{
			if (loggerDestination == LoggerDestinationEnum.DataGenLogger && _dataGenerator != null)
				_dataGenerator.SetLogger ((DataGeneratorLogger)logger);
		}
		
		public virtual void NextFuzzRun ()
		{
			if (_triggers != null)
			{
				foreach (ITrigger trigger in _triggers)
					trigger.NextFuzzRun ();
			}
		}
		
		public virtual void Run (FuzzController ctrl)
		{
			
			
			if (_stopCondition != null)
				_stopCondition.StartFuzzRound ();
			
			if (_triggers != null)
			{
				foreach (ITrigger trigger in _triggers)
					trigger.NextFuzzRun ();
			}
		}
		
		public void ApplyChangeableId (object changeableId)
		{
			if (changeableId != null && changeableId is ChangeableComponents && _changeableComponents.Contains ((ChangeableComponents)changeableId)) {
				_stopCondition = ((ChangeableComponents)changeableId).StopCondition;
				_dataGenerator = ((ChangeableComponents)changeableId).DataGenerator;
				_triggers = ((ChangeableComponents)changeableId).Triggers;
				_isPreCondition = ((ChangeableComponents)changeableId).IsPreCondition;
			} else if (changeableId != null)
				throw new ArgumentException ("Unexpected changeable id");
		}

		public virtual bool IsFinished 
		{
			get { return _stopCondition == null ? false : _stopCondition.Finished; }
		}
		
		public virtual bool IsTriggered (TriggerEnum triggerType, object data)
		{
			if (_triggers == null)
				return false;
			foreach (ITrigger trigger in _triggers)
			{
				if (trigger.IsTriggered (triggerType, data))
					return true;
			}
			
			return false;
		}
		#endregion		
		
		protected virtual void Disposing ()
		{
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			Disposing ();	
		}
		#endregion

	}
}

