/* Copyright 2010 Andreas Reiter <andreas.reiter@student.tugraz.at>, 
 *                Georg Neubauer <georg.neubauer@student.tugraz.at>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */



using System;
using System.IO;

namespace Iaik.Utils.Serialization
{

	/// <summary>
	/// Represents a typed primitive-type on the stream
	/// (bool, string, int, byte, byte[])
	/// </summary>
	[TypedStreamSerializable("p")]
	public class TypedPrimitive : ITypedStreamSerializable
	{
		private enum PrimitiveTypeEnum : byte
		{
			Bool = 0,
			String,
			Int,
			UInt,
			Byte,
			ByteA,
			UShort
		}
		
		public delegate void StreamWriteDelegate(object value, Stream sink);
	
		
		private object _value;
		
		public object Value
		{
			get{ return _value;}
		}
		
		public TypedPrimitive (Stream src)
		{
			Read (src);
		}
		
		public TypedPrimitive (object value)
		{
			_value = value;
		}
		
		#region IStreamSerializable implementation
		public void Write (Stream sink)
		{
			Type myType = _value.GetType ();
			
			if (myType.IsEnum)
				myType = Enum.GetUnderlyingType (myType);
			
			if (myType == typeof(int))
			{
				sink.WriteByte ((byte)PrimitiveTypeEnum.Int);
				StreamHelper.WriteInt32 ((int)_value, sink);
			}
			else if(myType == typeof(UInt32))
			{
				sink.WriteByte((byte)PrimitiveTypeEnum.UInt);
				StreamHelper.WriteUInt32((uint)_value, sink);
			}       
			else if (myType == typeof(bool))
			{
				sink.WriteByte ((byte)PrimitiveTypeEnum.Bool);
				StreamHelper.WriteBool ((bool)_value, sink);
			}
			else if (myType == typeof(string))
			{
				sink.WriteByte ((byte)PrimitiveTypeEnum.String);
				StreamHelper.WriteString ((string)_value, sink);
			}
			else if (myType == typeof(byte))
			{
				sink.WriteByte ((byte)PrimitiveTypeEnum.Byte);
				sink.WriteByte ((byte)_value);
			}
			else if (myType == typeof(byte[]))
			{
				sink.WriteByte ((byte)PrimitiveTypeEnum.ByteA);
				StreamHelper.WriteBytesSafe ((byte[])_value, sink);
			}
			else if (myType == typeof(ushort))
			{
				sink.WriteByte ((byte)PrimitiveTypeEnum.UShort);
				StreamHelper.WriteUInt16 ((ushort)_value, sink);
			}
			else
				throw new NotSupportedException(string.Format("The type '{0}' is not supported by TypedPrimitive", 
						myType));
		}


		public void Read (Stream src)
		{
			PrimitiveTypeEnum primitiveType = (PrimitiveTypeEnum)src.ReadByte ();
			
			
			if (primitiveType == PrimitiveTypeEnum.Int)
				_value = StreamHelper.ReadInt32 (src);
			else if(primitiveType == PrimitiveTypeEnum.UInt)
				_value = StreamHelper.ReadUInt32(src);
			else if (primitiveType == PrimitiveTypeEnum.Bool)
				_value = StreamHelper.ReadBool (src);
			else if (primitiveType == PrimitiveTypeEnum.String)
				_value = StreamHelper.ReadString (src);
			else if (primitiveType == PrimitiveTypeEnum.Byte)
				_value = src.ReadByte ();
			else if (primitiveType == PrimitiveTypeEnum.ByteA)
				_value = StreamHelper.ReadBytesSafe (src);
			else if (primitiveType == PrimitiveTypeEnum.UShort)
				_value = StreamHelper.ReadUInt16 (src);
			else
				throw new NotSupportedException (string.Format ("The type '{0}' is not supported by TypedPrimitive", primitiveType));
			
		}
		
		#endregion
		
		public override string ToString ()
		{
			return string.Format("[{0}] '{1}'", _value.GetType(), _value);
		}


	}
}
