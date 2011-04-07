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
using System.Security;
using System.Runtime.InteropServices;
using System.Text;

namespace Iaik.Utils.Hash
{

	/// <summary>
	/// Does not support stream serialization
	/// </summary>
	public class HashSecureStringDataProvider : HashDataProvider
	{
		
		/// <summary>
		/// Provides the secure string to hash
		/// </summary>
		private SecureString _src;
		
		/// <summary>
		/// Current location in _src
		/// </summary>
		private int _currentIndex = 0;
		
		public HashSecureStringDataProvider (SecureString src)
		{
			_src = src;
		}
		

		public override int NextBytes (byte[] buffer)
		{
			IntPtr secureStringPtr = Marshal.SecureStringToBSTR (_src);
			
			try
			{
				unsafe 
				{
					char* secureStringP = (char*)secureStringPtr;
					int read = 0;
					int charsRead = 0;
					
					for (int i = _currentIndex; i < buffer.Length; i++)
					{
						if (secureStringP[i] == 0)
							break;
						else
						{
							int byteCount = Encoding.UTF8.GetByteCount (new char[] { secureStringP[i] });
							
							//Check if the buffer is large enough to hold the bytes of the current car
							if (read + byteCount > buffer.Length)
								break;
							
							int charBytes = Encoding.UTF8.GetBytes (new char[] { secureStringP[i] }, 0, 1, buffer, read);
							read += charBytes;
							charsRead++;
						}
					}
					
					_currentIndex += charsRead;
					return read;
				
				}
			}
			finally
			{
				Marshal.ZeroFreeBSTR (secureStringPtr);
			}
		}

	}
}
