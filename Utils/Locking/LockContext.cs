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


// Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace Iaik.Utils.Locking
{
    /// <summary>
    /// Implemented by Lock providers
    /// </summary>
	public interface ILockContext : IDisposable
	{
        /// <summary>
        /// Returns the description of this lock context
        /// </summary>
		string LockDescription{get;}
	}

	/// <summary>
	/// Provides a disposable lock 
	/// </summary>
	public class LockContext : ILockContext
	{
		private object _lockTarget;
		
		/// <summary>
		/// Safes some descriptional text for lock failure detection
		/// </summary>
		internal string _lockDescription;
		
		public string LockDescription
		{
			get{ return _lockDescription; }
		}
		
		public LockContext (object lockTarget, string decription)
		{
			
			_lockDescription = decription;
			_lockTarget = lockTarget;
			LockSupervisor.AcquireLock(this);
			Monitor.Enter(_lockTarget);
			LockSupervisor.AcquiredLock(this);
		}
		
		public LockContext(object lockTarget)
			:this(lockTarget, null)
		{
		}
		
		#region IDisposable implementation
		public void Dispose ()
		{
			LockSupervisor.ReleaseLock(this);
			Monitor.Exit(_lockTarget);
		}
		
		#endregion

		
	}
	
	public class CombinedLockContext : ILockContext
	{
		private ILockContext[] _lockContexts;
		private string _lockDescription;
		
		public string LockDescription
		{
			get{ return _lockDescription; }
		}
		
		public CombinedLockContext(params ILockContext[] lockContext)
		{
			_lockContexts = lockContext;
			foreach(LockContext ctx in lockContext)
				_lockDescription += ctx.LockDescription + "; ";
		}
		
		public void Dispose ()
		{
			foreach(LockContext ctx in _lockContexts)
				ctx.Dispose();
		}

		
	}
	
	public static class LockSupervisor
	{
		private static Dictionary<int, List<string>> _lockActions = new Dictionary<int, List<string>>();
		private static List<string> _flatLockActions = new List<string>();
		
		private static string _baseFile = null;
		private static StreamWriter _output = null;
		private static Dictionary<int, StreamWriter> _outputs = new Dictionary<int, StreamWriter>();
		
		public static void Initialize(string dumpFile)
		{
			_baseFile = dumpFile;
			_output = new StreamWriter(dumpFile);
		}
		
		public static void AcquireLock(ILockContext ctx)
		{
			lock(_flatLockActions)
			{
				AddAction("A - " + ctx.LockDescription);
			}
		}
		
		public static void AcquiredLock(ILockContext ctx)
		{
			lock(_flatLockActions)
			{
				AddAction("G - " + ctx.LockDescription);
			}
		}
		
		public static void ReleaseLock(ILockContext ctx)
		{
			lock(_flatLockActions)
			{
				AddAction("R - " + ctx.LockDescription);
			}
			
		}
		
		private static void AddAction(string action)
		{
				
			if(_output != null)
			{
				_output.WriteLine(action + " " + Thread.CurrentThread.ManagedThreadId.ToString());
				_output.Flush();
				_output.BaseStream.Flush();
			
			
				if(_outputs.ContainsKey(Thread.CurrentThread.ManagedThreadId) == false)
				{
					_outputs.Add(Thread.CurrentThread.ManagedThreadId, new StreamWriter(_baseFile + "_" + Thread.CurrentThread.ManagedThreadId.ToString()));
				}
				
				_outputs[Thread.CurrentThread.ManagedThreadId].WriteLine(action + " " + Thread.CurrentThread.ManagedThreadId.ToString());
				_outputs[Thread.CurrentThread.ManagedThreadId].Flush();
				_outputs[Thread.CurrentThread.ManagedThreadId].BaseStream.Flush();
			
			}
			
			_flatLockActions.Add(action);
			
			if(_lockActions.ContainsKey(Thread.CurrentThread.ManagedThreadId) == false)
				_lockActions.Add(Thread.CurrentThread.ManagedThreadId, new List<string>());
			
			_lockActions[Thread.CurrentThread.ManagedThreadId].Add(action);
		}
	}
}
