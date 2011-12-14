// SingleValueFuzzDescription.cs
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
using Iaik.Utils.CommonAttributes;
using Fuzzer.DataGenerators;
using Fuzzer.FuzzLocations;
namespace Fuzzer.FuzzDescriptions
{
	/// <summary>
	/// Fuzzes a single variable with a simple type (no pointer). e.g. int, int64, char array ...
	/// </summary>
	[ClassIdentifier("fuzzdescription/single_value")]
	public class SingleValueFuzzDescription : IFuzzTech
	{
		
		/// <summary>
		/// The associated fuzz location
		/// </summary>
		private InMemoryFuzzLocation _fuzzLocation;
		
		/// <summary>
		/// The current data written to the fuzzing target
		/// </summary>
		private byte[] _currentFuzzData = null;
		
		
		public SingleValueFuzzDescription (IFuzzLocation fuzzLocation)
		{
			if (typeof(InMemoryFuzzLocation).IsAssignableFrom (fuzzLocation.GetType ()) == false)
				throw new ArgumentException ("PointerValueFuzzDescription needs an in memory fuzzer");
			
			_fuzzLocation = (InMemoryFuzzLocation)fuzzLocation;
		}
	

		#region IFuzzDescription implementation
		public void Init ()
		{
		}

		public void Run (FuzzController fuzzController)
		{
			_currentFuzzData = _fuzzLocation.DataGenerator.GenerateData ();
			ISnapshot snapshot = fuzzController.Snapshot;
			fuzzController.Connector.WriteMemory (_currentFuzzData, _fuzzLocation.FuzzTarget.Address.Value, (UInt64)_currentFuzzData.Length, ref snapshot);
			fuzzController.Snapshot = snapshot;
		}
		#endregion
}
}

