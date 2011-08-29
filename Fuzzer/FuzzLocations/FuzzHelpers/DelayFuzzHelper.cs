// DelayFuzzHelper.cs
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
using Iaik.Utils.CommonAttributes;
using System.Xml;
using Fuzzer.TargetConnectors;
using Iaik.Utils;
using System.Collections.Generic;
using System.Threading;
namespace Fuzzer.FuzzLocations.FuzzHelpers
{
	
	[ClassIdentifier("fuzz_helper/delay")]
	public class DelayFuzzHelper : BaseFuzzLocation
	{
		private int _myDelay = 0;
		#region implemented abstract members of Fuzzer.FuzzLocations.BaseFuzzLocation
		protected override bool SupportsStopCondition 
		{
			get { return false; }
		}
		
		
		protected override bool SupportsDataGen 
		{
			get { return false; }
		}
		
		
		protected override bool SupportsTrigger 
		{
			get { return false; }
		}
		
		#endregion
		public override void Init (XmlElement fuzzLocationRoot, ITargetConnector connector, Dictionary<string, IFuzzLocation> predefinedFuzzers)
		{
			base.Init (fuzzLocationRoot, connector, predefinedFuzzers);
			
			_myDelay = XmlHelper.ReadInt (fuzzLocationRoot, "Delay", 0);
		}
		
		public override void Run (FuzzController ctrl)
		{
			base.Run (ctrl);
			
			Thread.Sleep (_myDelay);
		}
	}
}

