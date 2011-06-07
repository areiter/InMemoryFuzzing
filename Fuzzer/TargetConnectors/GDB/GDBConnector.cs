// GDBConnector.cs
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

using Fuzzer.IO.ConsoleIO;

using Iaik.Utils;
using Iaik.Utils.CommonAttributes;
using System.Threading;
using System.IO;
using System.Text;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Builds a connection to the target using a gdb subprocess
	/// </summary>
	/// <remarks>
	/// Parameters:
	/// "gdb_exec": path to the gdb executable
	/// "args": 	extra arguments to pass to gdb
	/// "gdb_log":  stream:stderr to write gdb output and commands to standard error output
	/// 			stream:stdout standard output
	/// 			file:filename to write to specified file
	/// The executable will be loaded using the file command
	/// 
	/// </remarks>
	[ClassIdentifier("general/gdb")]
	public class GDBConnector : GDBSubProcess, ITargetConnector, ISymbolTable
	{
		
		
		public delegate void GdbStopDelegate(StopReasonEnum stopReason, GDBBreakpoint breakpoint, UInt64 address, Int64 status);
		
		/// <summary>
		/// File to read the symbol table from.
		/// </summary>
		protected string _file = null;
		 
		/// <summary>
		/// The cached methods
		/// </summary>
		private ISymbolTableMethod[] _cachedMethods = null;		
	
		/// <summary>
		/// Target specifier
		/// </summary>
		private string _target = null;
		
		
		/// <summary>
		/// Contains all current breakpoints
		/// </summary>
		private Dictionary<int, GDBBreakpoint> _breakpoints = new Dictionary<int, GDBBreakpoint>();
		
		/// <summary>
		/// Contains informations about the last debugger stop
		/// </summary>
		private IDebuggerStop _lastDebuggerStop = null;
		
		private ManualResetEvent _gdbStopEventHandler = new ManualResetEvent(false);
		
		public GDBConnector()
		{
			RegisterPermanentResponseHandler(new BreakpointRH(this, GdbStopped));
			RegisterPermanentResponseHandler(new ExitRH(GdbStopped, this));
			RegisterPermanentResponseHandler(new SignalRH(GdbStopped, this));	
			RegisterPermanentResponseHandler(new RecordLogRH(this, GdbStopped)); 
			RegisterPermanentResponseHandler(new UnhandledRH(this));
			
		}			
		
		private void GdbStopped(StopReasonEnum stopReason, GDBBreakpoint breakpoint, UInt64 address, Int64 status)
		{
			_lastDebuggerStop = new DebuggerStop(stopReason, breakpoint, address, status);
			_gdbStopEventHandler.Set();	
		}
		
			                                                  
		#region ITargetConnector implementation
		public override void Setup (IDictionary<string, string> config)
		{
			base.Setup(config);
			_target = DictionaryHelper.GetString("target", config, "extended-remote :1234");			
			_file = DictionaryHelper.GetString("file", config, null);
		}

		public void Connect ()
		{
			StartProcess();

			if(_file != null)
			{
				ManualResetEvent evt = new ManualResetEvent(false);
				bool success = false;
				QueueCommand(new FileCmd(_file, 
				   delegate(bool s){ 
					success = s;
					evt.Set();}, this));
				
				evt.WaitOne();
				if(!success) throw new ArgumentException("Could not load file");
			}
			
			bool connected = false;
			ManualResetEvent connectedEvt = new ManualResetEvent(false);
			TargetCmd targetCmd = new TargetCmd(_target,
			    (Action<bool>)delegate(bool connectionStatus){
					connected = connectionStatus;
					connectedEvt.Set();
			}, this);
			QueueCommand(targetCmd);
			connectedEvt.WaitOne();
			
			if(!connected)
				throw new Exception("Could not establish connection");
		}
			                            
		public void Close ()
		{
			QueueCommand(new CloseCmd(this));
		}

		public ulong ReadMemory (byte[] buffer, ulong address, ulong size)
		{
			UInt64 readSize = 0;
			ManualResetEvent evt = new ManualResetEvent (false);
			QueueCommand (new ReadMemoryCmd (address, size, buffer, 
			     delegate(UInt64 innerSize, byte[] innerBuffer) {
				    readSize = innerSize;
					evt.Set();
			     }, this));
			
			evt.WaitOne();
			return readSize;
		}

		public ulong WriteMemory (byte[] buffer, ulong address, ulong size)
		{
			string tempFile = System.IO.Path.GetTempFileName();
			
			using(FileStream fStream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.Write))
			{
				fStream.Write(buffer, 0, (int)size);
			}
			
			RestoreCmd cmd = new RestoreCmd(tempFile, address, this);
			cmd.CommandFinishedEvent += (Action<GDBCommand>)delegate(GDBCommand thisCmd)
			{
				File.Delete(tempFile);	
			};
			
			QueueCommand(cmd);
			return size;
		}

		public IBreakpoint SetSoftwareBreakpoint (ulong address, ulong size, string identifier)
		{
			int breakpointNum = 0;
			ManualResetEvent evt = new ManualResetEvent(false);
			
			
			QueueCommand(new SetBreakpointCmd(address,
			    delegate(int num, UInt64 breakpointAddress){
					breakpointNum = num;
					evt.Set();
			}, this));
			
			evt.WaitOne();
			GDBBreakpoint breakpoint = new GDBBreakpoint(this, breakpointNum, address, identifier, 
			      BreakpointRemoveFromList );
			
			_breakpoints.Add(breakpointNum, breakpoint);
			
			return breakpoint;
		}
		
		public IBreakpoint SetSoftwareBreakpoint(ISymbolTableMethod method, UInt64 size, string identifier)
		{
			if(method == null || method.AddressSpecifier == null)
				return null;
			
			UInt64? myAddress = method.BreakpointAddressSpecifier.ResolveAddress();
			if(myAddress == null)
				return null;
			
			return SetSoftwareBreakpoint(myAddress.Value, size, identifier);
		}

		public GDBBreakpoint LookupBreakpoint(int breakpointNum)
		{
			if(_breakpoints.ContainsKey(breakpointNum))
				return _breakpoints[breakpointNum];
			
			return null;
		}
		
		public GDBBreakpoint LookupBreakpointByAddress(UInt64 address)
		{
			foreach(GDBBreakpoint br in _breakpoints.Values)
			{
				if(br.Address == address)
					return br;
			}
			
			return null;
		}
		
		
		public IDebuggerStop DebugContinue()
		{
			return DebugContinue(false);
		}
		
		public IDebuggerStop DebugContinue (bool reverse)
		{
			bool success = false;
			ManualResetEvent evt = new ManualResetEvent (false);
			_gdbStopEventHandler.Reset ();
			QueueCommand (new ContinueCmd (reverse,
			    (Action<bool>)delegate(bool bSuc)
                {
				success = bSuc;
				evt.Set ();
			}, this));
			
			while (true)
			{
				//If negative continue response was received,
				//throw an exception
				if (evt.WaitOne (10) && success == false)
					throw new ArgumentException ("Continuing failed");
				
				
				if (_gdbStopEventHandler.WaitOne (10))
					break;
			}
			
			//There are situations where no address is available. e.g. the debugger only returns symbols on
			//break. Retrieve the current location in this situations
			if (_lastDebuggerStop.Address == 0)
			{
				evt.Reset ();
				QueueCommand (new PrintCmd (PrintCmd.Format.Hex, "$pc",
					delegate(object value) {
					if (value is UInt64)
						_lastDebuggerStop = new DebuggerStop (
							_lastDebuggerStop.StopReason, 
							_lastDebuggerStop.Breakpoint,
							(UInt64)value,
							_lastDebuggerStop.Status);
					
					  evt.Set ();
				}, this));
				
				evt.WaitOne ();
			}
			
			return _lastDebuggerStop;
			
		}

		
		public ISnapshot CreateSnapshot()
		{
			return new GDBSnapshot(this, _lastDebuggerStop);
		}
		
		public UInt64? GetRegisterValue(string register)
		{
			UInt64? address = null;
			ManualResetEvent evt = new ManualResetEvent(false);
			QueueCommand(new PrintCmd(PrintCmd.Format.Hex, "$" + register,
			    delegate(object value){
					if(value is UInt64)
						address = (UInt64)value;
				
					evt.Set();
			    }, this));
			
			evt.WaitOne();
			
			return address;
		}
		
		public void SetRegisterValue(string name, string value)
		{
			QueueCommand(new SetCmd("$" + name, value, this));
		}
		
		
		public ISymbolTableMethod[] ListMethods
		{
			get
			{
				CheckCachedMethods(false);
				
				return _cachedMethods;
			}
		}
		
		public ISymbolTableMethod FindMethod(string methodName)
		{
			CheckCachedMethods(false);
			
			foreach(ISymbolTableMethod m in _cachedMethods)
			{
				if(m.Name.Equals(methodName))
					return m;
			}
			
			return null;
		}
		
		public IAddressSpecifier ResolveSymbol(ISymbol symbol)
		{
			ManualResetEvent evt = new ManualResetEvent(false);
			IAddressSpecifier myAddress = null;
			
			QueueCommand(new InfoAddressCmd(symbol,
			     delegate(ISymbol s, IAddressSpecifier address){
				 	myAddress = address;
					evt.Set();
			     }, this));
			
			evt.WaitOne();
			
			return myAddress;
		}
		
		public IAddressSpecifier ResolveSymbolToBreakpointAddress(ISymbolTableMethod symbol)
		{
			IAddressSpecifier address = null;
			ManualResetEvent evt = new ManualResetEvent(false);
			
			QueueCommand(new SetBreakpointNameCmd(symbol,
			       delegate(int breakpointNum, UInt64 breakpointAddress)
			       {
					  address = new StaticAddress(breakpointAddress);
						
				      QueueCommand(new DeleteBreakpointCmd(breakpointNum, this));
					  evt.Set();
				   }, this));
			evt.WaitOne();
			return address;
		}
		
		public ISymbolTableVariable[] GetParametersForMethod(ISymbolTableMethod method)
		{
			List<ISymbolTableVariable> variables = new List<ISymbolTableVariable>();
			string[] myParameterTypes = null;
			ManualResetEvent evt = new ManualResetEvent(false);
			QueueCommand(
			  new WhatIsCmd(this, method, 
			    delegate(ISymbol symbol, string returnType, string[] parameterTypes)
                {
					myParameterTypes = parameterTypes;
					evt.Set();
				}));
			
			evt.WaitOne();
			
			if(myParameterTypes != null)
			{
				ISymbol[] myDiscoveredSymbols = null;
				evt.Reset();
				QueueCommand(new InfoScopeCmd(this, method.Name, 
				   delegate(ISymbol[] discoveredSymbols)
				   {
					  myDiscoveredSymbols = discoveredSymbols;
					  evt.Set();
				   }));
				
				evt.WaitOne();
				if(myDiscoveredSymbols.Length >= myParameterTypes.Length)
				{
					//Now we know the number of parameters and got all local variables valid in the
					//method-scope. GDB (hopefully) always outputs the parameters first, so we got
					//the parameter names.....is there a simpler method??
					for(int i = 0; i<myParameterTypes.Length; i++)
					{
						//TODO: Maybe include the type in ISymbolTableVariable?
						variables.Add(new GDBSymbolTableVariable(this, myDiscoveredSymbols[i].Symbol));
					}
				}
				return variables.ToArray();
			}
			else
				return null;
		}
		
		
		private void CheckCachedMethods(bool forced)
		{
			if(_cachedMethods == null || forced)
			{
				ManualResetEvent evt = new ManualResetEvent(false);
				ISymbolTableMethod[] myResolvedMethods = null;
				ISymbolTableMethod[] myUnresolvedMethods = null;
				
				QueueCommand(new InfoFunctionsCmd(this, 
				   delegate(ISymbolTableMethod[] resolvedMethods, ISymbolTableMethod[] unresolvedMethods){
					  myResolvedMethods = resolvedMethods;
					  myUnresolvedMethods = unresolvedMethods;
					  evt.Set();
				}, this));
				
				evt.WaitOne();
				
				List<ISymbolTableMethod> methods = new List<ISymbolTableMethod>();
				methods.AddRange(myResolvedMethods);
				
				foreach(ISymbolTableMethod m in myUnresolvedMethods)
					m.Resolve();
				
				methods.AddRange(myUnresolvedMethods);
				
				
				//Resolve method parameters, gdb cannot discover and resolve in a single step
				foreach(SymbolTableMethod m in methods)
					m.Parameters = GetParametersForMethod(m);
				
				_cachedMethods = methods.ToArray();
			}
			
		}
		
		public bool Connected 
		{
			get { return Running; }
		}
		#endregion	

		
		
		private void BreakpointRemoveFromList(int breakpointNum)
		{	
			_breakpoints.Remove(breakpointNum);
		}
	}
}

