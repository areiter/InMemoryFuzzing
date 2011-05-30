// PrintCmd.cs
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
using System.Text;
namespace Fuzzer.TargetConnectors.GDB
{
	public class PrintCmd : GDBCommand
	{
		public enum Format
		{
			Hex,
			None
		}
		
		private Format _format;
		private string _expression;
		private PrintRH _rh;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}
		
		public override string Command 
		{
			get 
			{				
				StringBuilder cmd = new StringBuilder();
				cmd.Append("print");
				
				if(_format == PrintCmd.Format.Hex)
					cmd.Append("/x");
				
				cmd.Append(" ");
				cmd.Append(_expression);
				
				return cmd.ToString();
			}
		}
		
		#endregion
		public PrintCmd (Format format, string expression, Action<object> callback)
		{
			_format = format;
			_expression = expression;
			_rh = new PrintRH(format, callback);
		}
	}
}

