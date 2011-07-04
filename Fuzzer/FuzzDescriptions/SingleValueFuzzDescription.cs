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
namespace Fuzzer.FuzzDescriptions
{
	/// <summary>
	/// Fuzzes a single variable with a simple type (no pointer). e.g. int, int64, ...
	/// </summary>
	[ClassIdentifier("fuzzdescription/single_value")]
	public class SingleValueFuzzDescription : IFuzzDescription
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
		
		public SingleValueFuzzDescription ()
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
			_currentFuzzData = _dataGenerator.GenerateData ();
			_fuzzController.Connector.WriteMemory (_currentFuzzData, _fuzzTarget.Address.Value, (UInt64)_currentFuzzData.Length, ref snapshot);
		}
		#endregion
}
}

