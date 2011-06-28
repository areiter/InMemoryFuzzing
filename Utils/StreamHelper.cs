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


//
//
// Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// Author: Georg Neubauer <georg.neubauer@student.tugraz.at>
using System;
using System.IO;
using System.Text;
using Iaik.Utils.Serialization;
using System.Reflection;

namespace Iaik.Utils
{
	/// <summary>
	/// Provides methods of writing/reading simple data types to/from streams
	/// </summary>
	public static class StreamHelper
	{
		public static void WriteUInt16(ushort value, Stream sink)
		{
			WriteBytes(BitConverter.GetBytes(value), sink);
		}
		
		public static ushort ReadUInt16(Stream src)
		{
			return BitConverter.ToUInt16(ReadBytes(2, src), 0);
		}
		
		public static void WriteInt32(Int32 value, Stream sink)
		{
			WriteBytes(BitConverter.GetBytes(value), sink);
		}
		
		public static int ReadInt32(Stream src)
		{
			return BitConverter.ToInt32(ReadBytes(4, src), 0);
		}
	
		public static void WriteUInt32 (UInt32 value, Stream sink)
		{
			WriteBytes (BitConverter.GetBytes (value), sink);
		}
		
		
		
		public static uint ReadUInt32(Stream src)
		{
			return BitConverter.ToUInt32(ReadBytes(4, src), 0);
		}
		
		public static void WriteInt64(Int64 value, Stream sink)
		{
			WriteBytes(BitConverter.GetBytes(value), sink);
		}
		
		public static Int64 ReadInt64 (Stream src)
		{
			return BitConverter.ToInt64 (ReadBytes (8, src), 0);
		}
		
		public static void WriteUInt64 (UInt64 value, Stream sink)
		{
			WriteBytes (BitConverter.GetBytes (value), sink);
		}

		public static UInt64 ReadUInt64 (Stream src)
		{
			return BitConverter.ToUInt64 (ReadBytes (8, src), 0);
		}
		
		public static void WriteNullableInt32(Int32? value, Stream sink)
		{
			if(value == null)
				WriteBool(false, sink);
			else
			{
				WriteBool(true, sink);
				WriteInt32(value.Value, sink);
			}
		}
		
		public static Int32? ReadNullableInt32(Stream src)
		{
			if(ReadBool(src))
				return ReadInt32(src);
			else
				return null;
		}
		
		public static void WriteString(String value, Stream sink)
		{
			if(value == null)
				WriteInt32(-1, sink);
			WriteInt32(Encoding.UTF8.GetByteCount(value), sink);
			WriteBytes(Encoding.UTF8.GetBytes(value), sink);
		}
		
		public static string ReadString(Stream src)
		{
			int length = ReadInt32(src);
			if(length == -1)
				return null;
			return Encoding.UTF8.GetString(ReadBytes(length, src));
		}

		public static void WriteBytesSafe(byte[] data, Stream sink)
		{
			if(data == null)
				WriteInt32(-1, sink);
			else
			{
				WriteInt32(data.Length, sink);
				WriteBytes(data, sink);
			}
		}
		
		public static byte[] ReadBytesSafe(Stream src)
		{
			int length = ReadInt32(src);
			if(length == -1)
				return null;
			else
				return ReadBytes(length, src);
		}
		
		private static void WriteBytes(byte[] buf, Stream sink)
		{
			sink.Write(buf, 0, buf.Length);
		}
			
		
		private static byte[] ReadBytes(int length, Stream src)
		{
			byte[] buf = new byte[length];
			int read = src.Read(buf, 0, length);
			
			if(read != length)
				throw new ArgumentException("Could not read enough bytes!");
			return buf;
		}
		

		public static void WriteBool(bool isResponse, Stream sink)
		{
			sink.WriteByte(isResponse?(byte)1:(byte)0);
		}
		
		public static bool ReadBool (Stream src)
		{
			return src.ReadByte () != 0;
		}
		
		public static void WriteStream(Stream src, Stream target)
		{
			WriteInt32((int)(src.Length - src.Position), target);
			
			byte[] buffer = new byte[4096];
			int read = 0;
			do
			{
				read = src.Read(buffer, 0, buffer.Length);
				target.Write(buffer, 0, read);
			}while(read > 0);
		}
		
		public static Stream ReadStream(Stream src)
		{
			byte[] buffer = new byte[4096];
			MemoryStream memSink = new MemoryStream();
			int dataLength = ReadInt32(src);
			int currentIndex = 0;
			int read = 0;
			
			do
			{
				int toRead = Math.Min(buffer.Length, dataLength - currentIndex);				
				read = src.Read(buffer, 0, toRead);
				
				memSink.Write(buffer, 0, read);
				currentIndex += read;
				
			}while(currentIndex < dataLength && read > 0);
			
			return memSink;
			
		}
		
		public static void WriteTypedStreamSerializable (ITypedStreamSerializable victim, Stream sink)
		{
			if (victim == null)
				WriteBool (false, sink);
			else
			{
				WriteBool (true, sink);
				TypedStreamSerializableAttribute attribute = TypedStreamSerializableHelper.FindAttribute (victim);
				
				
				WriteString (attribute.Identifier, sink);
				victim.Write (sink);
			}
		}
		
		public static T ReadTypedStreamSerializable<T> (Stream src) where T : ITypedStreamSerializable
		{
			return (T)ReadTypedStreamSerializable (src, typeof(T).Assembly);
		}
		
		public static ITypedStreamSerializable ReadTypedStreamSerializable (Stream src, params Assembly[] asms)
		{
			bool hasValue = ReadBool (src);
			if (hasValue == false)
				return null;
				
			string identifier = ReadString (src);
						
			foreach (Assembly asm in asms)
			{
				Type t = TypedStreamSerializableHelper.FindTypedStreamSerializableType (identifier, asm);
				
				if (t != null)
					return TypedStreamSerializableHelper.CreateTypedStreamSerializable (t, src);
			}
			
			throw new ArgumentException (string.Format ("Could not find TypedStreamSerializable-Type with identifier={0}", identifier));
		}
		
		
	}
}
