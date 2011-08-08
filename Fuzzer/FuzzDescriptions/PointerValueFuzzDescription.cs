// PointerValueFuzzDescription.cs
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
using Fuzzer.DataGenerators;
using Iaik.Utils.CommonAttributes;
using Fuzzer.FuzzLocations;

namespace Fuzzer.FuzzDescriptions
{
	[ClassIdentifier("fuzzdescription/pointer_value")]
	public class PointerValueFuzzDescription : IFuzzTech
	{
		
		/// <summary>
		/// Location to fuzz
		/// </summary>
		private InMemoryFuzzLocation _fuzzLocation;
		
		/// <summary>
		/// The current data written to the fuzzing target
		/// </summary>
		private byte[] _currentFuzzData = null;
		
		/// <summary>
		/// current memory allocated for the fuzz data
		/// </summary>
		private IAllocatedMemory _currentAllocatedMemory = null;
		
			
		public PointerValueFuzzDescription (IFuzzLocation fuzzLocation)
		{
			if (typeof(InMemoryFuzzLocation).IsAssignableFrom (fuzzLocation.GetType ()) == false)
				throw new ArgumentException ("PointerValueFuzzDescription needs an in memory fuzzer");
			
			_fuzzLocation = (InMemoryFuzzLocation)fuzzLocation;
		}
	

		#region IFuzzDescription implementation
		public void Init ()
		{
		}

		public void Run (FuzzController ctrl)
		{
			//It is assumed that currently the program is stopped at the snapshot
			
			ctrl.Snapshot.Destroy ();
			
			if (_currentAllocatedMemory != null)
			{
				ctrl.Connector.FreeMemory (_currentAllocatedMemory);
				_currentAllocatedMemory = null;
			}
			
			_currentFuzzData = _fuzzLocation.DataGenerator.GenerateData ();
			_currentAllocatedMemory = ctrl.Connector.AllocateMemory ((UInt64)_currentFuzzData.Length);
			ctrl.Connector.WriteMemory (_currentFuzzData, _currentAllocatedMemory.Address, (UInt64)_currentFuzzData.Length);
			
			//TODO: Big-Little endian handling
			byte[] addressBytes = BitConverter.GetBytes (_currentAllocatedMemory.Address);
			byte[] realAddressBytes;
			
			if (addressBytes.Length != _fuzzLocation.FuzzTarget.Size)
			{
				realAddressBytes = new byte[_fuzzLocation.FuzzTarget.Size];
				Array.Copy (addressBytes, realAddressBytes, Math.Min (addressBytes.Length, realAddressBytes.Length));
			}
			else
				realAddressBytes = addressBytes;
			
			ctrl.Connector.WriteMemory (realAddressBytes, _fuzzLocation.FuzzTarget.Address.Value, (UInt64)realAddressBytes.Length);
			ctrl.Snapshot = ctrl.Connector.CreateSnapshot ();
		}
		#endregion
}
}

