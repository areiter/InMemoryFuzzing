// GDBStackFrameInfo.cs
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
using System.Collections;
using System.Collections.Generic;
using Iaik.Utils.Serialization;
using Iaik.Utils;
using System.IO;
namespace Fuzzer.TargetConnectors.GDB
{
	[TypedStreamSerializable("gdb_stack_frame_info")]
	public class GDBStackFrameInfo : IStackFrameInfo
	{
		private IDictionary<string, IAddressSpecifier> _savedRegisters;
		
		public GDBStackFrameInfo (IDictionary<string, IAddressSpecifier> savedRegisters)
		{
			_savedRegisters = savedRegisters;
		}
		
		public GDBStackFrameInfo (Stream src)
		{
			_savedRegisters = new Dictionary<string, IAddressSpecifier> ();
			Read (src);
		}
	

		#region IStackFrameInfo implementation
		public IAddressSpecifier GetSavedRegisterAddress (string registerName)
		{
			if (_savedRegisters.ContainsKey (registerName))
				return _savedRegisters[registerName];
			
			return null;
		}
		#endregion
		
		#region IStreamSerializable implementation
		public void Write (Stream sink)
		{
			StreamHelper.WriteInt32 (_savedRegisters.Count, sink);
			
			foreach (KeyValuePair<string, IAddressSpecifier> pair in _savedRegisters)
			{
				StreamHelper.WriteString (pair.Key, sink);
				
				UInt64? address = pair.Value.ResolveAddress ();
				if (address == null)
					StreamHelper.WriteBool (false, sink);
				else
				{
					StreamHelper.WriteBool (true, sink);
					StreamHelper.WriteUInt64 (address.Value, sink);
				}	
			}
		}

		public void Read (Stream src)
		{
			int count = StreamHelper.ReadInt32 (src);
			
			for (int i = 0; i < count; i++)
			{
				string id = StreamHelper.ReadString (src);
				
				bool hasValue = StreamHelper.ReadBool (src);
				UInt64? address = null;
				
				if (hasValue)
					address = StreamHelper.ReadUInt64 (src);
				
				_savedRegisters.Add (id, new StaticAddress (address));
			}
		}
		#endregion
	}
}

