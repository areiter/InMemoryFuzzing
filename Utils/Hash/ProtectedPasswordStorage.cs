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
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Globalization;

namespace Iaik.Utils.Hash
{


	/// <summary>
	/// Holds a SecureString structure where the plaintext password is appended to.
	/// Once the password is completly appended it can be hashed and stored in 
	/// protected memory, where only the running process has access to
	/// </summary>
	/// <remarks>
	/// The goal was to use ProtectedMemory for hash storage. Protected Memory encrypts data
	/// which is only accessible by this process, but this is not available on all platforms,
	/// so the workaround is to store the hash value in a secure string and convert it to byte[]
	/// once it is needed
	/// </remarks>
	public sealed class ProtectedPasswordStorage
	{
		/// <summary>
		/// Specifies the hashing algorithm in use
		/// </summary>
		private string _hashAlgo;
		
		/// <summary>
		/// Stores the plaintext password, once hashing is done,
		/// this is cleared
		/// </summary>
		private SecureString _plainPassword = new SecureString();	
		
		/// <summary>
		/// Populated once hashing is done
		/// </summary>
		private SecureString _protectedHash = null;
		
		/// <summary>
		/// Current size in bytes of the hash
		/// </summary>
		private int _currentHashSize = 0;
		
		/// <summary>
		/// contains the raw hash value
		/// </summary>
		private byte[] _hash = null;
		
		/// <summary>
		/// Hash injected from the outside
		/// </summary>
		private byte[] _injectedHash = null;
		
		/// <summary>
		/// Gets the protected hash structure which is populated once the Hash method is called
		/// </summary>
		public byte[] HashValue
		{
			get { return _injectedHash != null?_injectedHash:_hash; }	
		}
		
		public bool Hashed
		{
			get{ return _protectedHash != null; }
		}
		
		public ProtectedPasswordStorage ()
			: this("SHA1")
		{
		}
		
		public ProtectedPasswordStorage (string hashAlgo)
		{
			_hashAlgo = hashAlgo;
		}		
		
		/// <summary>
		/// Ads a password character to the plain password storage
		/// </summary>
		/// <param name="p"></param>
		public void AppendPasswordChar (char p)
		{
			if (_plainPassword != null)
				_plainPassword.AppendChar (p);
			else
				throw new NotSupportedException ("Cannot add password char once the password is locked");
		}
		
		/// <summary>
		/// Locks the password and calculates the password hash
		/// </summary>
		public void Hash ()
		{
			if (_protectedHash != null)
				throw new NotSupportedException ("Cannot hash twice");
			
			if(_injectedHash != null)
				return;
			
			_plainPassword.MakeReadOnly ();
				
			HashProvider hashProvider = new HashProvider (_hashAlgo);
			
			//The array fpr protected memory needs to have multiple size of 16
			//int byteArrayLength = 
			//	4  // 4 bytes size of the actual hash value
			//	+ hashProvider.HashBitSize/8;
			//byteArrayLength += byteArrayLength % 16;
			
			//byte[] myHashValue = new byte[byteArrayLength];
			
			_currentHashSize = hashProvider.HashBitSize / 8;
			byte[] myHashValue = new byte[_currentHashSize];
			hashProvider.Hash (myHashValue, 0, new HashSecureStringDataProvider (_plainPassword));
			
			//HACK: It's not sure that the converted strings get deleted by the 
			// garbage collector. Use ProtectedMemory as soon as possible
			_protectedHash = new SecureString ();
			foreach (byte b in myHashValue)
			{
				string hexString = string.Format ("{0:X2}", b);
				foreach (char c in hexString)
					_protectedHash.AppendChar (c);
			}
	
			
			//Writes the size of the ahsh value to the start of the hashvalue array
			//Array.Copy(BitConverter.GetBytes((int)hashProvider.HashBitSize/8), myHashValue, 4);
			
			//Hash value starts at index 4
			//hashProvider.Hash(myHashValue, 4, new HashSecureStringDataProvider(_plainPassword));
		
			//ProtectedData.Protect(hashProvider, null, DataProtectionScope.
			//ProtectedMemory.Protect(myHashValue, MemoryProtectionScope.SameProcess);
		}
		
		/// <summary>
		/// Decrypts the hash. don't forget to run ClearHash afterwards
		/// </summary>
		public void DecryptHash ()
		{
			
			if(_injectedHash != null)
				return;
			
			if(_protectedHash == null)
				Hash();
				
				
			IntPtr hashPtr = Marshal.SecureStringToBSTR (_protectedHash);
			try
			{
				if (_hash != null)
					ClearHash ();
				
				_hash = new byte[_currentHashSize];
				unsafe
				{
					char current1;
					char current2;
					int currentIndex = 0;
					
					
					do
					{
						current1 = ((char*)hashPtr)[currentIndex * 2];
						current2 = ((char*)hashPtr)[currentIndex * 2 + 1];
						if (current1 != 0 && current2 != 0)
						{
							_hash[currentIndex] = (byte)int.Parse (string.Format ("{0}{1}", current1, current2), NumberStyles.HexNumber);
							currentIndex++;
						}
					}
					while (current1 != 0 && current2 != 0);
				}
			}
			finally
			{
				Marshal.ZeroFreeBSTR (hashPtr);
			}
		}
		
		/// <summary>
		/// Clears the hash in memory
		/// </summary>
		public void ClearHash ()
		{
			if (_hash != null)
				ByteHelper.ClearBytes (_hash);
			
			_hash = null;
		}
		
		
		public void InjectHash(byte[] hash)
		{
			_injectedHash = new byte[hash.Length];
			Array.Copy(hash, _injectedHash, hash.Length);
			ByteHelper.ClearBytes(hash);
		}
		
		public void WellKnown()
		{
			InjectHash(new byte[20]);
		}

		public bool EqualPassword (ProtectedPasswordStorage obj)
		{
			if (obj == null)
				return false;
			
			IntPtr plain1 = Marshal.SecureStringToBSTR (_plainPassword);
			IntPtr plain2 = Marshal.SecureStringToBSTR (obj._plainPassword);
			try
			{
				unsafe
				{
					int currentIndex = 0;
					while (true)
					{
						char char1 = ((char*)plain1)[currentIndex];
						char char2 = ((char*)plain2)[currentIndex];
						
						if (char1 != char2)
							return false;
						else if (char1 == 0 || char2 == 0)
							return true;
						
						currentIndex++;
					}
					
				}
			}
			finally
			{
				Marshal.ZeroFreeBSTR (plain1);
				Marshal.ZeroFreeBSTR (plain2);
			}
		}
		
		public SecureString ExportSecureString()
		{
			_plainPassword.MakeReadOnly();
			return _plainPassword;
		}

		
		
	}
}
