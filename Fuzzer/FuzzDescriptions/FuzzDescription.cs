// FuzzDescription.cs
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
using Fuzzer.FuzzLocations;
using System.Collections.Generic;
using Fuzzer.TargetConnectors;
namespace Fuzzer.FuzzDescriptions
{
	public class FuzzDescription
	{
		private List<IFuzzLocation> _fuzzLocations = new List<IFuzzLocation>();
		
		public List<IFuzzLocation> FuzzLocation
		{
			get { return _fuzzLocations; }
		}
		
		private IBreakpoint _snapshotBreakpoint = null;
		
		public IBreakpoint SnapshotBreakpoint
		{
			get { return _snapshotBreakpoint; }
		}
		
		private IBreakpoint _restoreBreakpoint = null;
		
		public IBreakpoint RestoreBreakpoint
		{
			get { return _restoreBreakpoint; }
		}
		
		public FuzzDescription (IBreakpoint snapshotBreakpoint, IBreakpoint restoreBreakpoint)
		{
			_snapshotBreakpoint = snapshotBreakpoint;
			_restoreBreakpoint = restoreBreakpoint;
		}
		
		public void Init ()
		{
		}
		
		public void NextFuzzRun ()
		{
			foreach (IFuzzLocation fuzzLocation in _fuzzLocations)
				fuzzLocation.NextFuzzRun ();
		}
	}
}

