// IDataGenerator.cs
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
using Fuzzer.DataLoggers;
using System.Collections.Generic;
namespace Fuzzer.DataGenerators
{
	/// <summary>
	/// Implemented by classes that generate data for the fuzzing process
	/// </summary>
	public interface IDataGenerator
	{
		/// <summary>
		/// Generate the next byte series.
		/// </summary>
		/// <remarks>
		/// The amount of bytes returned depends on the data generator implementation.
		/// E.g. a simple data generator which only generates data for int values may only return
		/// four bytes. Another one may return hundreds of bytes
		/// </remarks>
		/// <returns></returns>
		byte[] GenerateData();
		
		/// <summary>
		/// Sets the logger for the generated data to replay the generated data
		/// in another session
		/// </summary>
		/// <param name="logger"></param>
		void SetLogger(DataGeneratorLogger logger);
		
		/// <summary>
		/// Sets up the data generator. There may also be a ctor which makes this
		/// call obsolete. See implementation for configuration values
		/// </summary>
		/// <param name="config"></param>
		void Setup(IDictionary<string, string> config);
	}
	
	/// <summary>
	/// Implemented by classes that provide custom fuzz-data-length-schemes
	/// </summary>
	public interface IDataGeneratorLenType
	{
		/// <summary>
		/// Returns the length for the next data block
		/// </summary>
		/// <returns></returns>
		int NextLength();
	}
	
	public static class DataGeneratorLenTypeFactory
	{
		public static IDataGeneratorLenType Create (int minlen, int maxlen, string lentypeSpecifier)
		{
			if (lentypeSpecifier == null)
				lentypeSpecifier = "random";
			
			string[] lentypeParts = lentypeSpecifier.Split (new char[] { '|' }, 2);
			
			int arg1 = 0;
			if (lentypeParts[0].Equals ("random"))
				return new RandomDataGeneratorLenType (minlen, maxlen);
			else if (lentypeParts[0].Equals ("increase") && lentypeParts.Length == 2 && int.TryParse (lentypeParts[1], out arg1))
				return new IncreaseGeneratorLenType (minlen, maxlen, arg1);
			else if (lentypeParts[0].Equals ("decrease") && lentypeParts.Length == 2 && int.TryParse (lentypeParts[1], out arg1))
				return new DecreaseGeneratorLenType (minlen, maxlen, arg1);
			else
				throw new NotImplementedException (string.Format("Specified datagenerator len type '{0}' not supported", lentypeSpecifier));
		}
	}
	
	public class RandomDataGeneratorLenType : IDataGeneratorLenType
	{
		private int _minlen;
		private int _maxlen;
		private Random _r;
		
		public RandomDataGeneratorLenType (int minlen, int maxlen)
		{
			_minlen = minlen;
			_maxlen = maxlen;
			_r = new Random ();
		}
	

		#region IDataGeneratorLenType implementation
		public int NextLength ()
		{
			return _r.Next (_minlen, _maxlen);
		}
		#endregion
	}
	
	public class IncreaseGeneratorLenType : IDataGeneratorLenType
	{
		private int _minlen;
		private int _maxlen;
		private int _step;
		private int _current;
		
		public IncreaseGeneratorLenType (int minlen, int maxlen, int step)
		{
			_minlen = minlen;
			_maxlen = maxlen;
			_step = step;
			_current = -1;
		}
	

		#region IDataGeneratorLenType implementation
		public int NextLength ()
		{
			_current = (_current + _step) % (_maxlen);
			if(_current < _minlen)
				_current = _minlen;
			return _current;
		}
		#endregion
	}
	
	public class DecreaseGeneratorLenType : IDataGeneratorLenType
	{
		private int _minlen;
		private int _maxlen;
		private int _step;
		private int _current;

		public DecreaseGeneratorLenType (int minlen, int maxlen, int step)
		{
			_minlen = minlen;
			_maxlen = maxlen;
			_step = step;
			_current = maxlen;
		}


		#region IDataGeneratorLenType implementation
		public int NextLength ()
		{
			_current = (_current - _step) % (_maxlen - _minlen) + _minlen;
			return _current;
		}
		#endregion
	}
}

