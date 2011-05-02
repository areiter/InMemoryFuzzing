// SetBreakpointRH.cs
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
using System.Text.RegularExpressions;
namespace Fuzzer.TargetConnectors.GDB
{
	public class SetBreakpointRH : GDBResponseHandler
	{
		
		private Action<int> _cb;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		protected override string LogIdentifier 
		{
			get { return "RH_break"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBConnector connector, string[] responseLines, bool allowRequestLine)
		{
			Regex r = new Regex(@"Breakpoint\s*(?<num>\d+)\s*at\s*0x(?<at>\S*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			foreach(string line in responseLines)
			{
				Match m = r.Match(line);
				_cb(int.Parse(m.Result("${num}")));
				break;
			}
			
			return GDBResponseHandler.HandleResponseEnum.Handled;
		}
		
		#endregion
		
		
		public SetBreakpointRH (Action<int> cb)
		{
			_cb = cb;
		}
	}
}

