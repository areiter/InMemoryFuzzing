// ConsoleProcess.cs
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
using System.Diagnostics;
using System.IO;
using Iaik.Utils.IO;
namespace Fuzzer.IO.ConsoleIO
{
	/// <summary>
	/// Baseclass for processes that get started and interact with its host process using 
	/// standard in/output
	/// </summary>
	public abstract class ConsoleProcess : IDisposable
	{
		/// <summary>
		/// The process 
		/// </summary>
		private Process _proc = null;
		
		
		/// <summary>
		/// Reads multiple Textreaders and provides the data in a single textreader
		/// </summary>
		private MultiTextReader _multiTextReader = null;
		
		/// <summary>
		/// Gets the executable file
		/// </summary>
		protected abstract string Execfile{ get; }
		
		protected abstract string Arguments{ get; }
		
		protected virtual TextReader Output
		{
			get{ return _multiTextReader; }
		}
		
		protected virtual TextWriter Input
		{
			get{ return _proc.StandardInput;}
		}
		
		/// <summary>
		/// Gets if the process is currently running
		/// </summary>
		protected virtual bool Running
		{
			get
			{ 
				if(_proc == null)
					return false;
				else
					return !_proc.HasExited;
			}
		}
		
		public ConsoleProcess ()
		{
		}
		
		
		/// <summary>
		/// Starts the process specified by the inherited class
		/// </summary>
		protected virtual void StartProcess()
		{
			ProcessStartInfo startInfo = new ProcessStartInfo("/bin/sh", string.Format("-c \"{0} {1} 2>&1\"", Execfile, Arguments));
			startInfo.UseShellExecute = false;
			startInfo.RedirectStandardInput = true;
			startInfo.RedirectStandardOutput = true;
			startInfo.RedirectStandardError = true;
			
			_proc = Process.Start(startInfo);
			
			_multiTextReader = new MultiTextReader(_proc.StandardOutput, _proc.StandardError);
		}
		
		/// <summary>
		/// Forces the process to exit
		/// </summary>
		protected virtual void KillProcess()
		{
			if(_proc != null)
				_proc.Kill();
		}

		
		/// <summary>
		/// Reads the next character from the stdout of the target process
		/// </summary>
		/// <returns></returns>
		protected virtual char ReadChar()
		{
			AssertProcess();
			
			return (char)Output.Read();
		}
		
		/// <summary>
		/// Read a single line from the target process
		/// </summary>
		/// <returns></returns>
		protected virtual string ReadLine()
		{
			AssertProcess();

			return Output.ReadLine();
		}
		
		/// <summary>
		/// Write the specified objects with the specified format to the process stdin
		/// </summary>
		/// <param name="format">Format of the string to write</param>
		/// <param name="args">Arguments</param>
		protected virtual void Write(string format, params object[] args)
		{
			AssertProcess();
			
			_proc.StandardInput.Write(string.Format(format, args));
		}
		
		/// <summary>
		/// See Write
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		protected virtual void WriteLine(string format, params object[] args) 
		{
			AssertProcess();
			
			_proc.StandardInput.WriteLine(string.Format(format, args));
		}
		
		private void AssertProcess()
		{
			if(!Running) throw new ArgumentException("Process not started");
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			KillProcess();
		}
		#endregion		
	}
}

