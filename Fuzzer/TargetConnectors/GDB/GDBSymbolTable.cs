// GDBSymbolTable.cs
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
using Fuzzer.IO.ConsoleIO;
using System.Collections.Generic;
using Iaik.Utils;
using System.Threading;
using Iaik.Utils.CommonAttributes;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Provides access to ELF-embedded debugging and linker symbol tables
	/// using a gdb sub process
	/// </summary>
	/// 
	[ClassIdentifier("symbol_table/gdb")]
	public class GDBSymbolTable : GDBSubProcess, ISymbolTable
	{
		/// <summary>
		/// File to read the symbol table from.
		/// </summary>
		protected string _file = null;
		 
		
		public GDBSymbolTable ()
		{
			RegisterPermanentResponseHandler(new UnhandledRH());
		}
		
		public override void Setup (IDictionary<string, string> config)
		{
			base.Setup (config);
			
			_file = DictionaryHelper.GetString("file", config, null);
			if(_file == null)
				throw new ArgumentException("No 'file' specified");
			
			StartProcess();
			
			ManualResetEvent evt = new ManualResetEvent(false);
			bool success = false;
			QueueCommand(new FileCmd(_file, 
			   delegate(bool s){ 
				success = s;
				evt.Set();}));
			
			evt.WaitOne();
			if(!success) throw new ArgumentException("Could not load file");
		}
		
		public ISymbolTableMethod[] ListMethods
		{
			get
			{
				throw new NotImplementedException();
			}
		}
	}
}

