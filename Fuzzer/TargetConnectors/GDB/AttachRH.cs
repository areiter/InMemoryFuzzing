// AttachRH.cs
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
	public class AttachRH : GDBResponseHandler
	{
		private Action<string> _cb;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex r = new Regex (@"Attaching to program: (?<file>[\s*\S*]*), process (?<process_id>\S*)");
			
			foreach (string line in responseLines)
			{
				Match m = r.Match (line);
				
				if (m.Success)
				{
					_cb (m.Result ("${file}"));
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;
		}
		
		
		public override string LogIdentifier 
		{
			get { return "RH_attach"; }
		}
		
		#endregion
		public AttachRH (GDBSubProcess gdbProc, Action<string> cb)
			: base(gdbProc)
		{
			_cb = cb;
		}
	}
}

