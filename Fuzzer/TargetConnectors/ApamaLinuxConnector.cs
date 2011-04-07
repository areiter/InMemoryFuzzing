// ApamaLinuxConnector.cs
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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

using Iaik.Utils;
using Iaik.Utils.CommonAttributes;
	

namespace Fuzzer.TargetConnectors
{
	/// <summary>
	/// Builds a connection to the target using libapama compiled for linux
	/// </summary>
	/// <remarks>
	/// Parameters:
	/// "protocol": protocol:target,parameter1=value1,parameter2=value2
	///  e.g. gdb:127.0.0.1:1234
	///       gdb:/dev/tty1,target=device.xml
	/// </remarks>
	[ClassIdentifier("linux/apama")]
	public class ApamaLinuxConnector : ITargetConnector
	{
		#region Native Imports
		[DllImport("libapama.so")]
		private static extern apama_session_t apama_session_create(StringBuilder protocol);
		#endregion
		public ApamaLinuxConnector ()
		{
		}
		
		#region ITargetConnector implementation
		public void Setup (IDictionary<string, string> config)
		{
			string protocol = DictionaryHelper.GetString("protocol", config, null);
			if(protocol == null)
				throw new KeyNotFoundException("Value for \"protocol\" not found");
			
			apama_session_create(
			  new StringBuilder(protocol)
			);
		}

		public void Connect ()
		{
			throw new NotImplementedException ();
		}

		public bool Connected {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion

		
	}
}

