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
using Iaik.Utils.Serialization;

namespace Iaik.Utils.Hash
{

    /// <summary>
    /// Baseclass for all HashDataProviders, which provides the serialized data (byte stream) of some kind of 
    /// entity (byte array, stream, primitive type, more complex types,...)
    /// </summary>
	public abstract class HashDataProvider : AutoStreamSerializable, IDisposable, ITypedStreamSerializable
	{

		/// <summary>
		/// Writes the next databytes into buffer, buffer declares the count of bytes
		/// </summary>
		/// <param name="buffer"></param>
		/// <returns>Returns the actual written bytes</returns>
		public abstract int NextBytes(byte[] buffer);
		
		#region IDisposable implementation
		public virtual void Dispose ()
		{
		}
		
		#endregion
	}
}
