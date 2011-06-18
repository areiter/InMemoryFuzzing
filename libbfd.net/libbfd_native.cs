// libbfd_native.cs
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
using System.Runtime.InteropServices;
using System.Collections.Generic;
namespace Iaik.Utils.libbfd
{

	/// <summary>
	/// Contains the native pinvokes for the libbfd
	/// </summary>
	public static class libbfd_native
	{
		
		public enum FILEFORMAT : int
		{
			bfd_unknown = 0,
			bfd_object = 1,
			bfd_archive = 2,
			bfd_core = 3
		}
		
		[StructLayout(LayoutKind.Sequential)]
		public struct BfdSection
		{
			public string name;
			public int id;
			public int index;
			public IntPtr next_section;
			public IntPtr prev_section;
			public ushort flags;
			public IntPtr vma;
			public IntPtr lma;
			public UInt64 size;
			public UInt64 rawsize;
			public IntPtr relax;
			public int relax_count;
			public IntPtr output_offset;
			public IntPtr output_section;
			public UInt32 alignment_power;
			public IntPtr relocation;
			public IntPtr orelocation;
			public UInt32 reloc_count;
			//... not completed, currently only the size is of interest
		}
		
		/// <summary>
		/// Opens a BFD and returns a pointer to a BFD struct (also called abfd)
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="target">Specifies the format of the target file (hint GNUTARGET)</param>
		/// <returns></returns>
		[DllImport("libbfd")]
		public static extern IntPtr bfd_openr(string filename, string target);
	
		/// <summary>
		/// Closes a previous opened BFD
		/// </summary>
		/// <param name="abfd"></param>
		/// <returns></returns>
		[DllImport("libbfd")]
		public static extern int bfd_close(IntPtr abfd);
		
		/// <summary>
		/// Resturns the section pointer to the section with the specified name or null
		/// </summary>
		/// <param name="abfd">Pointer to a opened BFD</param>
		/// <param name="sectionName">Name of the section</param>
		/// <returns></returns>
		[DllImport("libbfd")]
		public static extern IntPtr bfd_get_section_by_name(IntPtr abfd, string sectionName);
		
		/// <summary>
		/// Sets the fileformat of the specified bfd
		/// </summary>
		/// <param name="abfd"></param>
		/// <param name="fileformat"></param>
		/// <returns> </returns>
		[DllImport("libbfd")]
		public static extern int bfd_set_format(IntPtr abfd, FILEFORMAT fileformat);
		
		/// <summary>
		/// Checks if the specified file format is compatible with the specified bfd
		/// </summary>
		/// <param name="abfd"></param>
		/// <param name="format"></param>
		/// <returns></returns>
		[DllImport("libbfd")]
		public static extern int bfd_check_format (IntPtr abfd, FILEFORMAT format);
		
		[DllImport("libbfd")]
		public static extern int bfd_check_format_matches(IntPtr abfd, FILEFORMAT format, out IntPtr matching);
		
		/// <summary>
		/// Returns a pointer to a list of available targets
		/// </summary>
		/// <returns>
		/// A <see cref="IntPtr"/>
		/// </returns>
		[DllImport("libbfd")]
		public static extern IntPtr bfd_target_list();	
		
		/// <summary>
		/// Writes the requested content oh the specified section to buffer and return true on success
		/// or false otherwise
		/// </summary>
		/// <param name="abfd"></param>
		/// <param name="section"></param>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		/// <returns></returns>
		[DllImport("libbfd")]
		public static extern bool bfd_get_section_contents 
			(IntPtr abfd, IntPtr section, byte[] buffer, UInt64 offset, UInt64 length);

		
		/// <summary>
		/// Returns the last error code
		/// </summary>
		/// <returns></returns>
		[DllImport("libbfd")]
		public static extern int bfd_get_error();
		
		/// <summary>
		/// Returns the error message with the specified error code
		/// </summary>
		/// <param name="error"></param>
		/// <returns></returns>
		[DllImport("libbfd")]
		public static extern IntPtr bfd_errmsg(int error);
		
		/// <summary>
		/// Creates a temporary BfdSection struct and returns the size of the section
		/// </summary>
		/// <param name="bfdSection"></param>
		/// <returns></returns>
		public static UInt64 GetSectionSize (IntPtr bfdSection)
		{
			BfdSection sectionStruct = (BfdSection)Marshal.PtrToStructure (bfdSection, typeof(BfdSection));
			return sectionStruct.size;
		}
		
		
		public static string GetFormattedLastError ()
		{
			int errorCode = bfd_get_error ();
			IntPtr errorMsgPtr = bfd_errmsg (errorCode);
			
			return string.Format ("{0} ({1})", Marshal.PtrToStringAuto (errorMsgPtr), errorCode);
		}
		
		/// <summary>
		/// Gets a list of all supported targets
		/// </summary>
		/// <returns>
		/// A <see cref="System.String[]"/>
		/// </returns>
		public static string[] GetTargetList ()
		{
			IntPtr myTargetList = libbfd_native.bfd_target_list ();
			IntPtr strPointer = IntPtr.Zero;
			List<string> list = new List<string> ();
			do {
				if (strPointer != IntPtr.Zero) {
					list.Add (Marshal.PtrToStringAnsi (strPointer));
				}
				
				if (myTargetList == IntPtr.Zero)
					break;
				
				byte[] buffer = new byte[IntPtr.Size];
				Marshal.Copy (myTargetList, buffer, 0, buffer.Length);
				
				strPointer = new IntPtr ((Int64)ByteArrayToUInt64 (buffer, 0, buffer.Length));
				
				myTargetList = new IntPtr (myTargetList.ToInt64 () + IntPtr.Size);
			} while (strPointer != IntPtr.Zero);
			
			return list.ToArray ();
		}
		
		/// <summary>
		/// Decodes the number encoded in data as a little endian number
		/// </summary>
		/// <param name="data">A <see cref="System.Byte[]"/></param>
		/// <param name="offset">A <see cref="System.Int32"/></param>
		/// <param name="length">A <see cref="System.Int32"/></param>
		/// <returns>A <see cref="UInt64"/></returns>
		private static UInt64 ByteArrayToUInt64 (byte[] data, int offset, int length)
		{
			UInt64 myNumber = 0;
			
			for (int i = offset; i < offset + length; i++) {
				myNumber += ((UInt64)data[i]) << 8 * (i - offset);
			}
			
			return myNumber;
		}
	}
}

