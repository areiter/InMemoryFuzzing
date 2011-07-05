// ITargetConnector.cs
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
using System.Collections.Generic;
using Fuzzer.TargetConnectors.RegisterTypes;
using Fuzzer.DataLoggers;

namespace Fuzzer.TargetConnectors
{
	/// <summary>
	/// Implemented by classes that provide fuzzer access to targets.
	/// </summary>
	/// <remarks>
	/// Target connectors can be architecture specific or cross platform
	/// 
	/// Always attach the <see cref="Iaik.Utils.CommonAttributes.ClassIdentifierAttribute"/> 
	/// to implementing classes.
	/// As a guideline use prefix "general/..." for cross platform target connectors
	/// and "win/...", "linux/...",.... for platform specific target connectors
	/// </remarks>
	public interface ITargetConnector : IDisposable
	{
		/// <summary>
		/// Gets the connection state
		/// </summary>
		bool Connected{ get; }
	
		/// <summary>
		/// Returns the symbol table implementation for this
		/// connector
		/// </summary>
		ISymbolTable SymbolTable{ get; }
		
		/// <summary>
		/// Returns the last stop reason
		/// </summary>
		IDebuggerStop LastDebuggerStop{ get; }
		
		/// <summary>
		/// Returns an object which resolves RegisterType->RegisterName
		/// </summary>
		IRegisterTypeResolver RegisterTypeResolver { get; }
		
		/// <summary>
		/// Sets up the connector
		/// </summary>
		/// <param name="config">
		/// A <see cref="IDictrionary<System.String, System.Object>"/>
		/// </param>
		void Setup(IDictionary<string, string> config);
		
		/// <summary>
		/// Connects to the target
		/// </summary>
		void Connect();
		
		/// <summary>
		/// Closes the connection to the target
		/// </summary>
		void Close();
		
		/// <summary>
		/// Reads the memory at the specified address
		/// </summary>
		/// <param name="buffer">Buffer of at least size in length</param>
		/// <param name="address">Address to start reading from </param>
		/// <param name="size">Number of bytes to read</param>
		/// <returns>Returns the number of actual read bytes</returns>
		UInt64 ReadMemory(byte[] buffer, UInt64 address, UInt64 size);
		
		/// <summary>
		/// Writes the memory at the specified address
		/// </summary>
		/// <param name="buffer">Buffer of at least size in length</param>
		/// <param name="address">Address to start writing to </param>
		/// <param name="size">Number of bytes to write</param>
		/// <returns>Returns the number of actual written bytes</returns>
		UInt64 WriteMemory(byte[] buffer, UInt64 address, UInt64 size, ref ISnapshot aSnapshot);
		
		UInt64 WriteMemory (byte[] buffer, UInt64 address, UInt64 size);
			
		/// <summary>
		/// Sets a software breakpoint at the specified address
		/// </summary>
		/// <param name="address">Address of the breakpoint</param>
		/// <param name="size">Specify the size of the instruction at address to patch</param>
		/// <param name="identifier">Readable identifier of the breakpoint</param>
		IBreakpoint SetSoftwareBreakpoint(UInt64 address, UInt64 size, string identifier);
		
		/// <summary>
		/// Sets a software breakpoint at the specified method
		/// </summary>
		/// <param name="method">A <see cref="ISymbolTableMethod"/></param>
		/// <param name="size">A <see cref="UInt64"/></param>
		/// <param name="identifier">A <see cref="System.String"/></param>
		/// <returns>A <see cref="IBreakpoint"/></returns>
		IBreakpoint SetSoftwareBreakpoint(ISymbolTableMethod method, UInt64 size, string identifier);
		
		/// <summary>
		/// Continues from the current position, to the next break
		/// </summary>
		IDebuggerStop DebugContinue();
		
		/// <summary>
		/// Snapshots the target process at its current state
		/// </summary>
		/// <returns>A <see cref="ISnapshot"/></returns>
		ISnapshot CreateSnapshot();
		
		/// <summary>
		/// Retrieves the value of the specified register
		/// </summary>
		/// <param name="register">A <see cref="System.String"/></param>
		/// <returns>A <see cref="UInt64"/></returns>
		UInt64? GetRegisterValue(string register);
		
		/// <summary>
		/// Sets the specified register to the specified value
		/// </summary>
		void SetRegisterValue(string name, string value);
		
		/// <summary>
		/// Retrieves informations about the current stack frame
		/// </summary>
		/// <returns></returns>
		IStackFrameInfo GetStackFrameInfo();
		
		/// <summary>
		/// Allocates memory of the given size on the target
		/// </summary>
		/// <param name="size">size to allocate</param>
		/// <returns></returns>
		IAllocatedMemory AllocateMemory(UInt64 size);
		
		/// <summary>
		/// Frees a previously allocated chunk of memory
		/// </summary>
		/// <param name="memory">Memory to free</param>
		void FreeMemory(IAllocatedMemory memory);
		
		/// <summary>
		/// Logs all relevant data generated by the connector to the specified path
		/// </summary>
		/// <param name="destination"></param>
		/// <returns></returns>
		IDataLogger CreateLogger(string destination);
	}
	
	public enum StopReasonEnum
	{
		/// <summary>
		/// A Breakpoint has been hit
		/// </summary>
		Breakpoint = 0,
		
		/// <summary>
		/// The program exited
		/// </summary>
		Exit,
		
		/// <summary>
		/// The program unexpectedly terminated
		/// </summary>
		Terminated	
		
	}
	
	public enum SignalEnum : int
	{
		UNKNOWN		= 0,
		SIGHUP 		= 1,	/* Hangup (POSIX).  */
		SIGINT 		= 2,	/* Interrupt (ANSI).  */
		SIGQUIT 	= 3,	/* Quit (POSIX).  */
		SIGILL 		= 4,	/* Illegal instruction (ANSI).  */
		SIGTRAP 	= 5,	/* Trace trap (POSIX).  */
		SIGABRT 	= 6,	/* Abort (ANSI).  */
		SIGIOT 		= 6,	/* IOT trap (4.2 BSD).  */
		SIGBUS 		= 7,	/* BUS error (4.2 BSD).  */
		SIGFPE 		= 8,	/* Floating-point exception (ANSI).  */
		SIGKILL 	= 9,	/* Kill, unblockable (POSIX).  */
		SIGUSR1 	= 10,	/* User-defined signal 1 (POSIX).  */
		SIGSEGV 	= 11,	/* Segmentation violation (ANSI).  */
		SIGUSR2 	= 12,	/* User-defined signal 2 (POSIX).  */
		SIGPIPE 	= 13,	/* Broken pipe (POSIX).  */
		SIGALRM 	= 14,	/* Alarm clock (POSIX).  */
		SIGTERM 	= 15,	/* Termination (ANSI).  */
		SIGSTKFLT 	= 16,	/* Stack fault.  */
		SIGCHLD 	= 17,	/* Child status has changed (POSIX).  */
		SIGCONT 	= 18,	/* Continue (POSIX).  */
		SIGSTOP 	= 19,	/* Stop, unblockable (POSIX).  */
		SIGTSTP 	= 20,	/* Keyboard stop (POSIX).  */
		SIGTTIN 	= 21,	/* Background read from tty (POSIX).  */
		SIGTTOU 	= 22,	/* Background write to tty (POSIX).  */
		SIGURG 		= 23,	/* Urgent condition on socket (4.2 BSD).  */
		SIGXCPU 	= 24,	/* CPU limit exceeded (4.2 BSD).  */
		SIGXFSZ 	= 25,	/* File size limit exceeded (4.2 BSD).  */
		SIGVTALRM 	= 26,	/* Virtual alarm clock (4.2 BSD).  */
		SIGPROF 	= 27,	/* Profiling alarm clock (4.2 BSD).  */
		SIGWINCH 	= 28,	/* Window size change (4.3 BSD, Sun).  */
		SIGIO 		= 29,	/* I/O now possible (4.2 BSD).  */
		SIGPWR 		= 30,	/* Power failure restart (System V).  */
		SIGUNUSED 	= 31,
		_NSIG 		= 64	/* Biggest signal number + 1
				   (including real-time signals).  */
	}
	
	/// <summary>
	/// Contains information why the debugger has stopped
	/// </summary>
	public interface IDebuggerStop
	{
		/// <summary>
		/// Contains the Debugger stop reason
		/// </summary>
		StopReasonEnum StopReason{ get; }
		
		/// <summary>
		/// Contains the breakpoint that caused the stop, if available
		/// </summary>
		IBreakpoint Breakpoint{ get; }
		
		/// <summary>
		/// Contains the stop address
		/// </summary>
		UInt64 Address{ get; }
		
		/// <summary>
		/// Returns the integer status (exit code, signal number,...) depending on the stop reason
		/// </summary>
		Int64 Status{ get; }
		
		/// <summary>
		/// Converts the status to a Signal, only valid if StopReason==Termination
		/// </summary>
		/// <returns>
		/// A <see cref="SignalEnum"/>
		/// </returns>
		SignalEnum StatusToSignal();
	}
	
	/// <summary>
	/// Default implementation of IDebuggerStop
	/// </summary>
	public class DebuggerStop : IDebuggerStop
	{
		private StopReasonEnum _stopReason;
		private IBreakpoint _breakpoint;
		private UInt64 _address;
		private Int64 _status;
		
		public DebuggerStop(StopReasonEnum stopReason, IBreakpoint breakpoint, UInt64 address, Int64 status)
		{
			_stopReason = stopReason;
			_breakpoint = breakpoint;
			_address = address;
			_status = status;
		}
		
		
		#region IDebuggerStop implementation
		public StopReasonEnum StopReason 
		{
			get { return _stopReason; }
		}

		public IBreakpoint Breakpoint 
		{
			get { return _breakpoint; }
		}

		public ulong Address 
		{
			get { return _address; }
		}
		
		public Int64 Status
		{
			get { return _status; }
		}
		
		public SignalEnum StatusToSignal()
		{
			return (SignalEnum)Status;
		}
		#endregion		
	}
}

