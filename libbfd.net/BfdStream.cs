// BfdStream.cs
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
using System.IO;
namespace Iaik.Utils.libbfd
{
	/// <summary>
	/// Wraps a readonly Stream around a bfd file and its sections
	/// </summary>
	public class BfdStream : Stream
	{
		/// <summary>
		/// Creates a BfdStream from a given Core dump file
		/// </summary>
		/// <param name="filename">Path to the cire file</param>
		/// <returns></returns>
		public static BfdStream CreateFromCoreFile (string filename, FileAccess access, string target)
		{
			if (access != FileAccess.Read)
				throw new NotImplementedException ("BfdStream currently only supports reading");
			
			IntPtr abfd = libbfd_native.bfd_openr (filename, target);
			
			if (abfd == IntPtr.Zero)
				throw new ArgumentException (string.Format ("Could not load file '{0}' ({1})", filename, libbfd_native.GetFormattedLastError ()));

			int result = libbfd_native.bfd_check_format (abfd, libbfd_native.FILEFORMAT.bfd_core);
			
			if (result == 0)
				throw new ArgumentException (string.Format ("File '{0}' has the wrong format or wrong target has been specified ({1})", filename, libbfd_native.GetFormattedLastError ()));
			
			return new BfdStream (abfd);
		}
		
		/// <summary>
		/// Creates a BfdStream from a given Core dump file and selects the specified section
		/// </summary>
		/// <param name="filename">Path to the cire file</param>
		/// <returns></returns>
		public static BfdStream CreateFromCoreFileSelectSection (string filename, FileAccess access, string target, string sectionName)
		{
			BfdStream stream = CreateFromCoreFile (filename, access, target);
			stream.SelectSection (sectionName);
			return stream;
		}
		
		/// <summary>
		/// Pointer to the opened BFD
		/// </summary>
		private IntPtr _abfd;
		
		/// <summary>
		/// The current offset in the section
		/// </summary>
		private UInt64 _currentOffset = 0;
		
		/// <summary>
		/// The current section pointer
		/// </summary>
		private IntPtr _currentSection = IntPtr.Zero;
		
		/// <summary>
		/// Length of the selected section, or null
		/// </summary>
		private UInt64 _sectionLength = 0;
		
		private BfdStream (IntPtr abfd)
		{
			_abfd = abfd;
		}
		
		/// <summary>
		/// Selects a section in the bfd file
		/// </summary>
		/// <param name="name">
		/// A <see cref="System.String"/>
		/// </param>
		public void SelectSection (string name)
		{
			_currentSection = libbfd_native.bfd_get_section_by_name (_abfd, name);
			
			if (_currentSection == IntPtr.Zero)
				throw new ArgumentException (string.Format ("Section '{0}' could not be found ({1})", name, libbfd_native.GetFormattedLastError ()));
			
			_sectionLength = libbfd_native.GetSectionSize (_currentSection);
			_currentOffset = 0;
			
		}
		
		
		protected override void Dispose (bool disposing)
		{
			libbfd_native.bfd_close (_abfd);
			_abfd = IntPtr.Zero;
			
			base.Dispose (disposing);	
		}
		#region implemented abstract members of System.IO.Stream

		
		public override void Flush ()
		{
		}
		
		
		public override int Read (byte[] buffer, int offset, int count)
		{
			AssertSection ();
			
			//end of stream
			if (_currentOffset + 1 == _sectionLength)
				return 0;
			
			if (buffer.Length < count)
				throw new ArgumentException ("Buffer too small");
			
			byte[] myBuffer = buffer;
			
			//If the buffer got an offset, we need to create a temporary one
			if (offset != 0)
				myBuffer = new byte[count];

			int realCount = (int)Math.Min (_sectionLength - _currentOffset, (ulong)count);
			
			if (libbfd_native.bfd_get_section_contents (_abfd, _currentSection, myBuffer, (ulong)_currentOffset, (ulong)realCount))
			{
				if (offset != 0)
					Array.Copy (myBuffer, 0, buffer, offset, count);
				
				_currentOffset += (ulong)realCount;
				return realCount;
			}
			else
				throw new ArgumentException (string.Format ("Could not read from BFD ({0})", libbfd_native.GetFormattedLastError ()));
		}
		
		
		public override long Seek (long offset, SeekOrigin origin)
		{
			AssertSection ();
			if (origin == SeekOrigin.Begin && (ulong)offset < _sectionLength)
				_currentOffset = (ulong)offset;
			else if (origin == SeekOrigin.Current && _currentOffset + (ulong)offset < _sectionLength)
				_currentOffset += (ulong)offset;
			else if (origin == SeekOrigin.End && (ulong)offset < _sectionLength)
				_currentOffset = _sectionLength - (ulong)offset;
			else
				throw new ArgumentException("Illegal offset/origin combination");
				
			
			return (long)_currentOffset;
		}
		
		
		public override void SetLength (long value)
		{
			throw new System.NotSupportedException ();
		}
		
		
		public override void Write (byte[] buffer, int offset, int count)
		{
			throw new System.NotSupportedException();
		}
		
		
		public override bool CanRead 
		{
			get { return true; }
		}
		
		
		public override bool CanSeek 
		{
			get { return true; }
		}
		
		
		public override bool CanWrite 
		{
			get { return false; }
		}
		
		
		public override long Length 
		{
			get 
			{
				AssertSection ();
				return (long)_sectionLength;
			}
		}
		
		
		public override long Position 
		{
			get 
			{
				AssertSection ();
				return (long)_currentOffset;
			}
			set 
			{
				if(value > (long)_sectionLength || value < 0)
					throw new ArgumentException("Illegal position");
				
				_currentOffset = (ulong)value;
			}
		}
		
		#endregion
		
		private void AssertSection ()
		{
			if (_currentSection == IntPtr.Zero)
				throw new ArgumentException ("No section selected");
		}
		
	}
}

