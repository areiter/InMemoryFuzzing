// ContinueRH.cs
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
	/// <summary>
	/// Parses the continue response 
	/// "Continue." on success and "The program is not being run." on failure
	/// </summary>
	public class ContinueRH : GDBResponseHandler
	{
		
		private Action<bool> _cb;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		protected override string LogIdentifier 
		{
			get { return "RH_continue"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess connector, string[] responseLines, bool allowRequestLine)
		{
			Regex success = new Regex(@"\s*Continuing.\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex failure = new Regex(@"\s*The program is not being run.\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			foreach(string line in responseLines)
			{
				if(success.Match(line).Success)
				{
					_cb(true);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				else if(failure.Match(line).Success)
				{
					connector.QueueCommand(new RunCmd(_cb));
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}				
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;
		}
		
		#endregion
		
		
		public ContinueRH (Action<bool> cb)
		{
			_cb = cb;
		}
	}
}

