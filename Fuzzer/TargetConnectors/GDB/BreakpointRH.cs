// BreakpointRH.cs
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
	/// Handles breakpoint responses.
	/// </summary>
	public class BreakpointRH : GDBResponseHandler
	{
		private GDBConnector _connector;
		
		/// <summary>
		/// Is called if gdb receivesd a break
		/// </summary>
		private GDBConnector.GdbStopDelegate _gdbStopped;
		
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override string LogIdentifier 
		{
			get { return "RH_breakpoint"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess connector, string[] responseLines, bool allowRequestLine)
		{
			Regex rAddress = new Regex(@"Breakpoint\s*(?<num>\d+)\s*,\s*0x(?<at>\S*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			//Is there an option to tell GDB to always output the current pc on break?
			Regex rSymbol = new Regex(@"Breakpoint\s*(?<num>\d+)\s*,\s*(?<symbol>\S*)\s*\((?<args>[\s*\S*]*)\)\s*at\s*(?<file>[\S*\s*]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			foreach(string line in responseLines)
			{
				Match m = rAddress.Match(line);
				if(m.Success)
				{
					int breakpointNum = int.Parse(m.Result("${num}"));
					GDBBreakpoint breakpoint = _connector.LookupBreakpoint(breakpointNum);
					
					if(breakpoint != null)
					{
						_gdbStopped(StopReasonEnum.Breakpoint, breakpoint,
						            UInt64.Parse(m.Result("${at}"), NumberStyles.HexNumber), 0);
						
					}

					
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				
				m = rSymbol.Match(line);
				if(m.Success)
				{
					int breakpointNum = int.Parse(m.Result("${num}"));
					GDBBreakpoint breakpoint = _connector.LookupBreakpoint(breakpointNum);
					
					if(breakpoint != null)
					{
						_gdbStopped(StopReasonEnum.Breakpoint, breakpoint, 0, 0);
					}
					
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;			
		}
		
		#endregion
		public BreakpointRH (GDBConnector connector, GDBConnector.GdbStopDelegate gdbStopped)
			:base(connector)
		{
			_connector = connector;
			_gdbStopped = gdbStopped;
		}
	}
}
