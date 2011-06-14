// PrintRH.cs
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
	public class PrintRH : GDBResponseHandler
	{
		private PrintCmd.Format _format;
		private Action<object> _callback;
			
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex r;
			//if(_format == PrintCmd.Format.Hex)
			//	r = new Regex(@"[\s*\S*]*=\s*0x(?<at>\S*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			//else
			r = new Regex (@"[\s*\S*]*=\s*(?<value>[\s*\S*]*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

			Regex rNoRegisters = new Regex (@"No registers", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rNoSymbol = new Regex ("No symbol \"(?<symbol_name>\\S*)\" in current context.", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			for (int i = 0; i < responseLines.Length; i++)
			{
				string line = responseLines[i];
				
				Match match = r.Match (line);

				if (match.Success)
				{
					string value = match.Result ("${value}");
					if (_format == PrintCmd.Format.Hex && value.Trim ().StartsWith ("0x"))
						_callback (UInt64.Parse (value.Trim ().Substring (2), NumberStyles.HexNumber));
					else
						_callback (match.Result ("${value}"));
					
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				
				if (rNoRegisters.Match (line).Success)
				{
					_callback (null);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				
				if (rNoSymbol.Match (line).Success)
				{
					_callback (null);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
			}
			
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;			
		}
		
		
		public override string LogIdentifier 
		{
			get { return "RH_print"; }
		}
		
		#endregion
		public PrintRH (PrintCmd.Format format, Action<object> callback, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_format = format;
			_callback = callback;
		}
	}
}

