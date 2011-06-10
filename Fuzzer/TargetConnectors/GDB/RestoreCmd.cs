// RestoreCmd.cs
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
using System.IO;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Sends a restore command to write to target memory
	/// restore FILE [OFFSET] [START] [END]
	/// </summary>
	public class RestoreCmd:GDBCommand
	{

		private string _file;
		private UInt64 _address;
		private RestoreRH _rh;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBCommand
		public override GDBResponseHandler ResponseHandler 
		{
			get { return _rh; }
		}		
		
		public override string Command 
		{
			get{ return string.Format("restore {0} binary 0x{1}", _file.Replace(" ", "\\ "), _address.ToString("X")); }				
		}
		
		
		protected override string LogIdentifier 
		{
			get { return "CMD_restore"; }
		}
		
		
		
		#endregion
		public RestoreCmd (string filename, UInt64 address, GDBSubProcess gdbProc)
			: base(gdbProc)
		{
			_file = filename;
			_address = address;
			_rh = new RestoreRH (gdbProc);
			
		}
	}
}

