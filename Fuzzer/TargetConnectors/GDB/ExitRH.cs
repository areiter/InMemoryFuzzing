// ExitRH.cs
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
using System.Globalization;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Handles Program exits.
	/// </summary>
	public class ExitRH : GDBResponseHandler
	{
		/// <summary>
		/// Is called if gdb receives a break
		/// </summary>
		private GDBConnector.GdbStopDelegate _gdbStopped;
		
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		protected override string LogIdentifier 
		{
			get { return "RH_exit"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBConnector connector, string[] responseLines, bool allowRequestLine)
		{
			Regex normal = new Regex(@"Program has exited normally.", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex withCode = new Regex(@"Program exited with code (?<exit_code>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			foreach(string line in responseLines)
			{
				Match mNormal = normal.Match(line);
				Match mWithCode = withCode.Match(line);
				if(mNormal.Success)
				{
					_gdbStopped(StopReasonEnum.Exit, null, 0, 0);					
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				else if(mWithCode.Success)
				{					
					_gdbStopped(StopReasonEnum.Exit, null, 0, Convert.ToInt64(mWithCode.Result("${exit_code}"), 8));
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;			
		}
		
		#endregion
		public ExitRH (GDBConnector.GdbStopDelegate gdbStopped)
		{
			_gdbStopped = gdbStopped;
		}
	}
}
