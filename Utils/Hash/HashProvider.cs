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
using System.Security.Cryptography;

namespace Iaik.Utils.Hash
{

	/// <summary>
	/// Hashes data with the specified algorithm and from the specified sources
	/// </summary>
	public class HashProvider
	{

		/// <summary>
		/// The hash algorithm instance to use
		/// </summary>
		protected HashAlgorithm _hashAlgorithm;

        /// <summary>
        /// Name of the hash algorithm
        /// </summary>
        protected string _sHashAlgorithm;

        public string HashAlgorithmName
        {
            get { return _sHashAlgorithm; }
        }

		public int HashBitSize
		{
			get { return _hashAlgorithm.HashSize; }
		}
		
		public HashProvider ()
			:this("SHA1")
		{
		}
		
		public HashProvider (string hashAlgorithm)
		{
            _sHashAlgorithm = hashAlgorithm;
			_hashAlgorithm = HashAlgorithm.Create (hashAlgorithm);
		}
		
		/// <summary>
		/// Performs the hashing operation. The data is applied to the hash algorithm as they come in dataProviders.
		/// </summary>
		/// <param name="dataProviders"></param>
		/// <returns> Returns the hashvalue of the specified (concatenated) dataProviders</returns>
		public byte[] Hash (params HashDataProvider[] dataProviders)
		{
			_hashAlgorithm.Initialize ();
			
			byte[] internalBuffer = new byte[1024 * 4];
			
			
			foreach (HashDataProvider dataProvider in dataProviders) {
				int bufferLength;
				do {
					bufferLength = dataProvider.NextBytes (internalBuffer);
					
					if (bufferLength > 0)
						_hashAlgorithm.TransformBlock (internalBuffer, 0, bufferLength, internalBuffer, 0);
				} while (bufferLength > 0);
			}
			
			_hashAlgorithm.TransformFinalBlock (internalBuffer, 0, 0);
			return _hashAlgorithm.Hash;
		}
		
		/// <summary>
		/// Performs the hashing operation. The data is applied to the hash algorithm as they come in dataProviders.
		/// </summary>
		/// <param name="dataProviders"></param>
		public void Hash (byte[] buffer, int index, params HashDataProvider[] dataProviders)
		{
			Array.Copy(Hash(dataProviders), 0, buffer, index, HashBitSize/8);
            ByteHelper.ClearBytes(_hashAlgorithm.Hash);
			_hashAlgorithm.Clear();
			
		}
	}
}
