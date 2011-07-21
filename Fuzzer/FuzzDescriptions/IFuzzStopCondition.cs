// IFuzzStopCondition.cs
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
namespace Fuzzer.FuzzDescriptions
{
	public interface IFuzzStopCondition
	{
		/// <summary>
		/// Returns if the Fuzzong process for the current fuzz location is already finished
		/// </summary>
		bool Finished { get; }
		
		/// <summary>
		/// Tells the stop condition that another round gets started
		/// </summary>
		void StartFuzzRound();
	}
	
	public class CountFuzzStopCondition : IFuzzStopCondition
	{
		private int _maxRuns;
		private int _current = 0;
		
		public CountFuzzStopCondition (int maxRuns)
		{
			_maxRuns = maxRuns;
		}
		
		#region IFuzzStopCondition implementation
		public void StartFuzzRound ()
		{
			_current++;
		}

		public bool Finished 
		{
			get { return _current > _maxRuns; }
		}
		#endregion
	}
}

