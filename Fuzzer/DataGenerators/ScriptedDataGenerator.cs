// ScriptedDataGenerator.cs
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
using System.Collections.Generic;
using Fuzzer.DataLoggers;
using Iaik.Utils;
using Fuzzer.Scripting.Environments;
using Fuzzer.Scripting;
namespace Fuzzer.DataGenerators
{
	[ClassIdentifier("datagen/scripted")]
	public class ScriptedDataGenerator : IDataGenerator
	{
		private DataGeneratorLogger _logger;
		private ScriptEvaluator<ScriptedDataGeneratorEnvironment> _scriptEvaluator = null;
		private byte[] _data = null;
		
		
		public ScriptedDataGenerator ()
		{
		}
		
		
		#region IDataGenerator implementation
		public void Setup (IDictionary<string, string> config)
		{
			_scriptEvaluator = new ScriptEvaluator<ScriptedDataGeneratorEnvironment> (
				config, new Action<byte[]> (SetDataCallback), config);
		}
				
		private void SetDataCallback (byte[] data)
		{
			_data = data;
		}

		public void SetLogger (DataGeneratorLogger logger)
		{
			_logger = logger;
		}

		public byte[] GenerateData ()
		{
			_scriptEvaluator.Run ();
			_logger.LogData (_data);
			
			Console.WriteLine ("ScriptedDataGenerator: {0}", ByteHelper.ByteArrayToHexString (_data));
			return _data;
		}
		#endregion
	}
}

