// Register.cs
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
using System.Collections.Generic;
using System.Collections;
using Iaik.Utils.Serialization;
using System.IO;
using Iaik.Utils;
namespace Fuzzer.TargetConnectors
{
	[TypedStreamSerializable("registers")]
	public class Registers : IEnumerable<Register>, ITypedStreamSerializable
	{
		public static Registers CreateFromFile (string registerFile)
		{
			using (FileStream src = File.OpenRead (registerFile))
			{
				return CreateFromStream (src);
			}
		}
		
		public static Registers CreateFromStream (Stream src)
		{
			return StreamHelper.ReadTypedStreamSerializable<Registers> (src);
		}
		
		
		
		private List<Register> _registers = new List<Register>();
		
	
		public Registers ()
		{
		}
		
		public Registers (Stream src)
		{
			Read (src);
		}
		
		public void Add (Register reg)
		{
			_registers.Add (reg);
		}
		
		public void Clear ()
		{
			_registers.Clear ();
		}
		
		public Register FindRegisterByName (string name)
		{
			foreach (Register reg in _registers)
			{
				if (reg.Name.Equals (name, StringComparison.InvariantCultureIgnoreCase))
					return reg;
			}
			
			return null;
		}
		
		public Register FindRegisterByNum (uint num)
		{
			foreach (Register reg in _registers)
			{
				if (reg.Num == num)
					return reg;
			}
			
			return null;
		}
	

		#region IEnumerable[Register] implementation
		public IEnumerator<Register> GetEnumerator ()
		{
			return _registers.GetEnumerator ();
		}
		#endregion
				
		#region IEnumerable implementation
		IEnumerator IEnumerable.GetEnumerator ()
		{
			return _registers.GetEnumerator ();
		}
		#endregion

		#region IStreamSerializable implementation
		public void Write (Stream sink)
		{
			StreamHelper.WriteInt32 (_registers.Count, sink);
			
			foreach (Register r in _registers)
				r.Write (sink);
		}

		public void Read (Stream src)
		{
			_registers = new List<Register> ();
			
			int count = StreamHelper.ReadInt32 (src);
			
			for (int i = 0; i < count; i++)
				_registers.Add (new Register(src));
		}
		#endregion

}
	
	/// <summary>
	/// Contains all informations of a single register
	/// </summary>
	[TypedStreamSerializable("register")]
	public class Register : AutoStreamSerializable, ITypedStreamSerializable
	{
		[SerializeMe(0)]
		private uint _num;
		
		/// <summary>
		/// Target platform number of the register
		/// </summary>
		public uint Num
		{
			get { return _num;}
		}
		
		[SerializeMe(1)]
		private string _name;
		
		/// <summary>
		/// Friendly name of the register
		/// </summary>
		public string Name
		{
			get { return _name;}
		}
		
		[SerializeMe(2)]
		private uint _size;
		
		/// <summary>
		/// Bytesize of the register
		/// </summary>
		public uint Size
		{
			get { return _size;}
		}
		
		public Register (uint num, string name, uint size)
		{
			_num = num;
			_name = name;
			_size = size;
		}
		
		public Register (Stream src)
		{
			Read (src);
		}
	}
}

