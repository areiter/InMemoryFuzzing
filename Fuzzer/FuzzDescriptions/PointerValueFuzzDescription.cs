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

namespace Fuzzer.FuzzDescriptions
{
	[ClassIdentifier("fuzzdescription/pointer_value")]
	public class PointerValueFuzzDescription : IFuzzDescription
	{
		/// <summary>
		/// The target variable to fuzz
		/// </summary>
		private ISymbolTableVariable _fuzzTarget;

		/// <summary>
		/// The generator
		/// </summary>
		private IDataGenerator _dataGenerator;

		/// <summary>
		/// The current fuzz controller
		/// </summary>
		private FuzzController _fuzzController;

		/// <summary>
		/// The current data written to the fuzzing target
		/// </summary>
		private byte[] _currentFuzzData = null;
		
		/// <summary>
		/// current memory allocated for the fuzz data
		/// </summary>
		private IAllocatedMemory _currentAllocatedMemory = null;
		
		
		private IFuzzStopCondition _stopCondition = null;
		
		
		public IFuzzStopCondition StopCondition
		{
			get{ return _stopCondition; }
			set{ _stopCondition = value;}
		}
		
		public PointerValueFuzzDescription ()
		{
			
		}
	

		#region IFuzzDescription implementation
		public void Init (FuzzController fuzzController)
		{
			_fuzzController = fuzzController;
		}
		
		public void SetFuzzTarget (ISymbolTableVariable fuzzTarget)
		{
			_fuzzTarget = fuzzTarget;
		}

		public void SetDataGenerator (IDataGenerator dataGenerator)
		{
			_dataGenerator = dataGenerator;
		}

		public void Run (ref ISnapshot snapshot)
		{
			//It is assumed that currently the program is stopped at the snapshot
			
			snapshot.Destroy ();
			
			if (_currentAllocatedMemory != null)
			{
				_fuzzController.Connector.FreeMemory (_currentAllocatedMemory);
				_currentAllocatedMemory = null;
			}
			
			_currentFuzzData = _dataGenerator.GenerateData ();
			_currentAllocatedMemory = _fuzzController.Connector.AllocateMemory ((UInt64)_currentFuzzData.Length);
			_fuzzController.Connector.WriteMemory (_currentFuzzData, _currentAllocatedMemory.Address, (UInt64)_currentFuzzData.Length);
			
			//TODO: Big-Little endian handling
			byte[] addressBytes = BitConverter.GetBytes (_currentAllocatedMemory.Address);
			byte[] realAddressBytes;
			
			if (addressBytes.Length != _fuzzTarget.Size)
			{
				realAddressBytes = new byte[_fuzzTarget.Size];
				Array.Copy (addressBytes, realAddressBytes, Math.Min (addressBytes.Length, realAddressBytes.Length));
			}
			else
				realAddressBytes = addressBytes;
			
			_fuzzController.Connector.WriteMemory (realAddressBytes, _fuzzTarget.Address.Value, (UInt64)realAddressBytes.Length);
			snapshot = _fuzzController.Connector.CreateSnapshot ();
		}
		#endregion
}
}

