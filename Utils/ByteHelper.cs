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
using System.Text;
using System.IO;
using Iaik.Utils.Serialization;

namespace Iaik.Utils
{

    /// <summary>
    /// Static class which provides some byte helper functions
    /// </summary>
	public static class ByteHelper
	{

		/// <summary>
		/// Converts a byte array to a space seperated hex string
		/// </summary>
		/// <param name="data"></param>
		/// <param name="seperator">seperator which is appended to each hex value (except the last)</param>		
		/// <returns></returns>
		public static string ByteArrayToHexString(byte[] data)
		{
			return ByteArrayToHexString(data, " "); 
		}
		
		/// <summary>
		/// Converts a byte array to a hex string
		/// </summary>
		/// <param name="data"></param>
		/// <param name="seperator">seperator which is appended to each hex value (except the last)</param>		
		/// <returns></returns>
		public static string ByteArrayToHexString (byte[] data, string seperator)
		{
			if (data == null)
				return "<null>";
			StringBuilder returnVal = new StringBuilder ();
			
			for(int i = 0; i<data.Length; i++)
			{
				returnVal.AppendFormat ("{0:X2}", data[i]);
				
				if(i+1 < data.Length)
					returnVal.Append(seperator);
			}
			
			return returnVal.ToString ();
		}
		
		/// <summary>
		/// Compares the 2 byte arrays and returns if they contain the same data
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool CompareByteArrays (byte[] left, byte[] right)
		{
			if (left.Length != right.Length)
				return false;
			
			for (int i = 0; i < left.Length; i++)
			{
				if (left[i] != right[i])
					return false;
			}
			
			return true;
		}
		
		public static void ClearBytes (byte[] data)
		{
			Array.Clear (data, 0, data.Length);
		}
		
		
		/// <summary>
		/// XORs all bytes from data with key and saves them to data
		/// </summary>
		/// <param name="data"> </param>
		/// <param name="key"></param>
		public static void XORBytes(byte[] data, byte[] key)
		{
			if(data.Length != key.Length)
				throw new ArgumentException("Error XORBytes needs two arrays with same dimension");
		
			for(int i = 0; i<data.Length;i++)
			{
				data[i] = (byte)(data[i] ^ key[i]);
			}
		}
		
		public static byte[] SerializeToBytes(params IStreamSerializable[] victims)
		{
			using(MemoryStream sink = new MemoryStream())
			{
				foreach(IStreamSerializable victim in victims)
					victim.Write(sink);
					
				return sink.ToArray();
			}
		}
		
	}
}
