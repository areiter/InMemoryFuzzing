// AnalyzeController.cs
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
using System.Xml;
using System.IO;
using System.Collections.Generic;
using Fuzzer.TargetConnectors.GDB.CoreDump;
using Fuzzer.TargetConnectors;
using log4net;
using Fuzzer.TargetConnectors.RegisterTypes;
namespace Fuzzer.Analyzers
{
	public class AnalyzeController
	{
		private string _destination = null;
		private string _errorlog = null;
		private IDataAnalyzer[] _analyzers = null;
		private XmlDocument _doc = null;
		private int _highestPrefix = 0;
		
		private ILog _log = LogManager.GetLogger("AnalyzeController");
		
		/// <summary>
		/// Register description of the target system
		/// </summary>
		private Registers _targetRegisters;
		
		public Registers TargetRegisters
		{
			get { return _targetRegisters; }
		}
		
		/// <summary>
		/// Resolves register types to corresponding names for platform independence
		/// </summary>
		private IRegisterTypeResolver _registerTypeResolver;

		public IRegisterTypeResolver RegisterTypeResolver
		{
			get { return _registerTypeResolver;}
		}
		
		/// <summary>
		/// TODO: Currently the only connector is the gdb connector. If other
		/// connectors are added an abstraction is needed here
		/// </summary>
		private GDBProcessRecordSection _executedInstructions = null;
		
		public IEnumerable<InstructionDescription> ExecutedInstructions
		{
			get { return _executedInstructions; }
		}
		
		public void Setup (string errorlog, string destination, Registers targetRegisters, IRegisterTypeResolver registerTypeResolver, params IDataAnalyzer[] analyzers)
		{
			_errorlog = errorlog;
			_destination = destination;
			_analyzers = analyzers;
			_targetRegisters = targetRegisters;
			_registerTypeResolver = registerTypeResolver;
			
			_doc = new XmlDocument ();
			_doc.AppendChild (_doc.CreateElement ("Errorlog"));
			
			
			foreach (IDataAnalyzer analyzer in analyzers)
			{
				analyzer.Path = _destination;
				analyzer.Setup (_doc.DocumentElement);
			}
			
			
			// It is supposed that there exists at least a single file for each prefix.
			// Find the highest prefix here
			foreach (string file in Directory.GetFiles (_destination))
			{
				
				string[] splitted = Path.GetFileName(file).Split ('.');
				if (splitted.Length > 0)
				{
					int prefix;
					if (int.TryParse (splitted[0], out prefix) && _highestPrefix < prefix)
						_highestPrefix = prefix;
				}
			}
		}
		
		public void Analyze ()
		{
			_log.InfoFormat ("Analyzing '{0}'...", _destination);
			
			//It is supposed that the naming of the files looks as follows:
			//<prefix>.<logger specific extension> (e.g. fuzzdata, errorlog, execlog, stackframeinfo,...)
			for (int prefix = 1; prefix < _highestPrefix; prefix++)
			{
				if (prefix == 1 || prefix % 10 == 0)
					_log.InfoFormat ("progress: {0}% ({1}/{2})", (int)((double)prefix / (double)_highestPrefix * 100.0), prefix, _highestPrefix);
				
				if (!ReadExecutionLog (prefix.ToString ()))
				{
					_log.WarnFormat ("[prefix: {0}] Could not find execution log", prefix);
					continue;
				}
				
				Array.ForEach<IDataAnalyzer> (_analyzers, delegate(IDataAnalyzer dataAnalyzer) 
				{
					dataAnalyzer.Prefix = prefix.ToString ();
					dataAnalyzer.Analyze (this);
				});
			
				if (prefix % 10 == 0)
					_doc.Save (_errorlog);
			}
			
			
			_doc.Save (_errorlog);
		}
		
		private bool ReadExecutionLog (string prefix)
		{
			string execLog = Path.Combine (_destination, prefix + ".execlog");
			
			if (File.Exists (execLog))
			{
				using (Stream src = File.OpenRead (execLog))
					_executedInstructions = new GDBProcessRecordSection (src, _targetRegisters);
				
				return true;
			}
			
			return false;
		}
		
		public AnalyzeController ()
		{
		}
	}
}

