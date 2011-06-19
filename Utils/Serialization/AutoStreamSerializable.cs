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
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace Iaik.Utils.Serialization
{

	/// <summary>
	/// Finally implemented by classes which support automatic stream (reflective supported) serialization.
	/// All members to serialize have attached an SerializeMeAttribute
	/// </summary>
	public abstract class AutoStreamSerializable : IStreamSerializable
	{
		
		/// <summary>
		/// Returns the member information for all Members to de-/serialize
		/// </summary>
		/// <returns>
		/// A <see cref="FieldInfo[]"/>
		/// </returns>
		private FieldInfo[] GetMembersToSerializeInOrder ()
		{
			IDictionary<int, FieldInfo> dictMemberInfos = new SortedDictionary<int, FieldInfo> ();
			
			foreach (FieldInfo memberInfo in this.GetType ().GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				object[] attachedAttributes = memberInfo.GetCustomAttributes (typeof(SerializeMeAttribute), false);
			
				if (attachedAttributes != null && attachedAttributes.Length > 0)
					dictMemberInfos.Add (((SerializeMeAttribute)attachedAttributes[0]).Ordinal, memberInfo);
			}
			
			
			List<FieldInfo> memberInfos = new List<FieldInfo> ();
			
			foreach (FieldInfo memberInfo in dictMemberInfos.Values)
				memberInfos.Add (memberInfo);
			
			return memberInfos.ToArray ();
		}
		
		
		#region IStreamSerializable implementation
		public virtual void Write (Stream sink)
		{
			foreach (FieldInfo memberInfo in GetMembersToSerializeInOrder ())
			{
				Type curType = memberInfo.FieldType;
				
				if (curType.IsEnum)
					curType = Enum.GetUnderlyingType (curType);
				
				if (typeof(ITypedStreamSerializable).IsAssignableFrom (curType)) 
				{
					StreamHelper.WriteTypedStreamSerializable ((ITypedStreamSerializable)memberInfo.GetValue (this), sink);
				}
				else if (typeof(IStreamSerializable).IsAssignableFrom (curType))
				{
					IStreamSerializable toSerialize = (IStreamSerializable)memberInfo.GetValue (this);
					
					if (toSerialize == null)
						StreamHelper.WriteBool (false, sink);
					else
					{
						StreamHelper.WriteBool (true, sink);
						toSerialize.Write (sink);
					}
				}
				else if (curType == typeof(byte))
					sink.WriteByte ((byte)memberInfo.GetValue (this));
				else if (curType == typeof(byte[]))
					StreamHelper.WriteBytesSafe ((byte[])memberInfo.GetValue (this), sink);
                else if (curType == typeof(byte[][]))
                {
					StreamHelper.WriteInt32 (((byte[][])memberInfo.GetValue (this)).Length, sink);

                    foreach (byte[] data in (byte[][])memberInfo.GetValue (this))
						StreamHelper.WriteBytesSafe (data, sink);
				}
                else if (curType == typeof(int))
					StreamHelper.WriteInt32 ((int)memberInfo.GetValue (this), sink);
                else if (curType == typeof(uint))
					StreamHelper.WriteUInt32 ((uint)memberInfo.GetValue (this), sink);
                else if (curType == typeof(ushort))
					StreamHelper.WriteUInt16 ((ushort)memberInfo.GetValue (this), sink);
				else if (curType == typeof(string))
					StreamHelper.WriteString ((string)memberInfo.GetValue (this), sink);
                else if (curType == typeof(Stream))
                    StreamHelper.WriteStream((Stream)memberInfo.GetValue(this), sink);
                else
                    throw new ArgumentException(string.Format("Type '{0}' is not supported by AutoStreamSerializable", curType));
			}
		}
		

		public virtual void Read (Stream src)
		{
			foreach (FieldInfo memberInfo in GetMembersToSerializeInOrder ())
			{
				Type curType = memberInfo.FieldType;
				
				if (curType.IsEnum)
					curType = Enum.GetUnderlyingType (curType);
				
				if (typeof(ITypedStreamSerializable).IsAssignableFrom (curType))
				{
					memberInfo.SetValue (this, StreamHelper.ReadTypedStreamSerializable (src, this.GetType ().Assembly));
				}
				else if (typeof(IStreamSerializable).IsAssignableFrom (curType))
				{
					ConstructorInfo defaultCtorInfo = curType.GetConstructor (new Type[] {  });
					ConstructorInfo ctorInfo = curType.GetConstructor (new Type[] { typeof(Stream) });
					
					if (defaultCtorInfo == null && ctorInfo == null)
						throw new ArgumentException (string.Format ("'{0}' is not compatible with AutoStreamSerializable, no ctor(Stream) found!", curType));
					
					bool hasValue = StreamHelper.ReadBool (src);
					
					if (hasValue)
					{
						if (ctorInfo != null)
						{
							memberInfo.SetValue (this, ctorInfo.Invoke (new object[] { src }));
						}
						else
						{
							memberInfo.SetValue (this, defaultCtorInfo.Invoke (new object[] {  }));
							((IStreamSerializable)memberInfo.GetValue (this)).Read (src);
						}
					}
					else
						memberInfo.SetValue (this, null);
				
				}
				else if (curType == typeof(byte))
					memberInfo.SetValue (this, (byte)src.ReadByte ());
				else if (curType == typeof(byte[]))
					memberInfo.SetValue (this, StreamHelper.ReadBytesSafe (src));
                else if (curType == typeof(byte[][]))
                {
					int length = StreamHelper.ReadInt32 (src);
					byte[][] val = new byte[length][];

                    for (int i = 0; i < length; i++)
						val[i] = StreamHelper.ReadBytesSafe (src);

                    memberInfo.SetValue (this, val);
				}
                else if (curType == typeof(int))
					memberInfo.SetValue (this, StreamHelper.ReadInt32 (src));
                else if (curType == typeof(uint))
					memberInfo.SetValue (this, StreamHelper.ReadUInt32 (src));
                else if (curType == typeof(ushort))
					memberInfo.SetValue (this, StreamHelper.ReadUInt16 (src));
                else if (curType == typeof(Stream))
					memberInfo.SetValue (this, StreamHelper.ReadStream (src));
				else if (curType == typeof(string))
					memberInfo.SetValue (this, StreamHelper.ReadString (src));
                else
                    throw new ArgumentException(string.Format("Type '{0}' is not supported by AutoStreamSerializable", curType));
			}
		}
		
		#endregion
	}
}
