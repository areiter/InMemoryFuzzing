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

namespace Iaik.Utils.Nonce
{

    /// <summary>
    /// Static class used to generate nonces of variable length
    /// </summary>
	public static class NonceGenerator
	{
        /// <summary>
        /// Generates a random nonce with the specified length
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
		public static byte[] GenerateByteNonce (int length)
		{
			byte[] randomData = new byte[length];
			GenerateByteNonce (randomData);
			return randomData;
		}
		
        /// <summary>
        /// Fills the specified byte array with random data
        /// </summary>
        /// <param name="nonce"></param>
		public static void GenerateByteNonce (byte[] nonce)
		{
			Random r = new Random ();
			r.NextBytes (nonce);
		}
		
	}
}
