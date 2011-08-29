// FixedByteGenerator.cs
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
using Iaik.Utils.IO;
using System.IO;
namespace Fuzzer.DataGenerators
{
	[ClassIdentifier("datagen/fixed_bytes")]
	public class FixedByteGenerator : IDataGenerator
	{
		private string _data;
		private DataGeneratorLogger _logger;
		
		public FixedByteGenerator ()
		{
		}
	

		#region IDataGenerator implementation
		public byte[] GenerateData ()
		{
			
			using (HexFilterStream src = new HexFilterStream (new TextReaderStream (new StringReader (_data))))
			{
				byte[] data = new byte[src.Length];
				src.Read (data, 0, data.Length);
				_logger.LogData (data);
				return data;
			}
		}

		public void SetLogger (DataGeneratorLogger logger)
		{
			_logger = logger;
		}

		public void Setup (IDictionary<string, string> config)
		{
			_data = DictionaryHelper.GetString ("data", config, "");
		}
		#endregion
}
}

