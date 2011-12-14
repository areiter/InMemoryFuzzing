// GDBCStyleReferenceAddressSpecifier.cs
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
using System.Threading;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Expects a variable that can be resolved with gdb like
	/// print/x &(${var})
	/// </summary>
	public class GDBCStyleReferenceAddressSpecifier : IAddressSpecifier
	{
		private string _varSpecifier;
		private GDBSubProcess _gdbProc;
		
		public GDBCStyleReferenceAddressSpecifier (string varSpecifier, GDBSubProcess gdbProc)
		{
			_varSpecifier = varSpecifier;
			_gdbProc = gdbProc;
		}
	

		#region IAddressSpecifier implementation
		/// <summary>
		/// Writes print/x &(${var}) to gdb
		/// </summary>
		/// <returns>
		/// A <see cref="System.Nullable<UInt64>"/>
		/// </returns>
		public UInt64? ResolveAddress() 
		{
			UInt64? myAddress = null;
			ManualResetEvent evt = new ManualResetEvent(false);
			_gdbProc.QueueCommand(
				new PrintCmd(PrintCmd.Format.Hex,
			                 string.Format("&({0})", _varSpecifier),
			    delegate(object value){
					if(value is UInt64)
						myAddress = (UInt64)value;
					evt.Set();
				}, _gdbProc));
			evt.WaitOne();
			return myAddress;
		}
		#endregion
}
}

