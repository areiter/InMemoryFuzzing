// Trigger.cs
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
using Fuzzer.XmlFactory;
using Fuzzer.TargetConnectors;
namespace Fuzzer.FuzzDescriptions
{
	public class Trigger : ITrigger
	{
		private TriggerEnum _trigger;
		private bool _triggerFirst;
		private int _triggerCount = 0;
		private object _data;
		
		private int _currentCount = 0;
		
		public Trigger (string trigger, string triggerArgs, ITargetConnector connector)
		{
			_trigger = (TriggerEnum)Enum.Parse (typeof(TriggerEnum), trigger, true);
			
			if (_trigger == TriggerEnum.Location)
			{
				_data = FuzzDescriptionInfo.ParseRegionAddress (triggerArgs, false, connector);
				/*IBreakpoint myBreakpoint =*/ connector.SetSoftwareBreakpoint (
					((IAddressSpecifier)_data).ResolveAddress ().Value, 0, "Trigger at '" + triggerArgs + "'");
			}
			else if (triggerArgs != null && triggerArgs.StartsWith ("!"))
			{
				_triggerFirst = true;
				_triggerCount = Int32.Parse (triggerArgs.Substring (1));
			}
			else if(triggerArgs != null)
				_triggerCount = Int32.Parse (triggerArgs);
		}
	

		#region ITrigger implementation
		public void NextFuzzRun ()
		{
			_currentCount++;
			if (_currentCount > _triggerCount)
				_currentCount = 1;
		}

		public bool IsTriggered (TriggerEnum trigger, object data)
		{
			if (_triggerFirst && trigger == _trigger && (trigger == TriggerEnum.Start || trigger == TriggerEnum.End))
			{
				_triggerFirst = false;
				_currentCount = 0;
				return true;
			}
			
			if (trigger == _trigger && 
				(trigger == TriggerEnum.Start || trigger == TriggerEnum.End) &&
				(_currentCount == _triggerCount || _triggerCount == 0))
			{
				return true;
			}
			
			if (trigger == _trigger &&
			    trigger == TriggerEnum.Location &&
				data is UInt64)
			{
				UInt64? address = ((IAddressSpecifier)_data).ResolveAddress ();
				
				return (address != null && address.Value == (UInt64)data);
			}
			
			return false;
		}
		
		#endregion
}
}

