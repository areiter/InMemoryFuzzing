// XmlAnalyzeFactory.cs
//  
//  Author:
//       Andreas Reiter <andreas.reiter@student.tugraz.at>
// 
//  Copyright 2011  Andreas Reiter <andreas.reiter@student.tugraz.at>
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
using Iaik.Utils;
using System.Collections.Generic;
using Fuzzer.TargetConnectors;
using Fuzzer.TargetConnectors.RegisterTypes;
using Fuzzer.Analyzers;
using Iaik.Utils.CommonFactories;
namespace Fuzzer.XmlFactory
{
	/// <summary>
	/// Creates all components of the analyzation process by using settings defined in
	/// the specified xml file.
	/// </summary>
	public class XmlAnalyzeFactory
	{
		/// <summary>
		/// Analyzation config file
		/// </summary>
		private XmlDocument _doc;
		
		/// <summary>
		/// Path to the config file
		/// </summary>
		private string _configDir;
		
		/// <summary>
		/// Formatter which contains all defined values
		/// </summary>
		private SimpleFormatter _formatter;
		
		/// <summary>
		/// Contains all defined values
		/// </summary>
		private IDictionary<string, string> _values = new Dictionary<string, string>();
		
		/// <summary>
		/// Architecture dependend Register information
		/// </summary>
		private Registers _registers = null;
		
		/// <summary>
		/// Architecture dependend register type resolver
		/// </summary>
		private IRegisterTypeResolver _registerTypeResolver = null;
		
		/// <summary>
		/// Directory where the files to analyze are located
		/// </summary>
		private string _sourcePath = null;
		
		/// <summary>
		/// Output file, where all discovered error are recorded
		/// </summary>
		private string _errorLog = null;
		
		/// <summary>
		/// Contains all defined analyzers
		/// </summary>
		private List<IDataAnalyzer> _analyzers = null;
		
		public XmlAnalyzeFactory (string path)
		{
			FileInfo configFile = new FileInfo(path);
			
			if (!configFile.Exists)
				throw new FileNotFoundException (string.Format ("The specified xml description file '{0}' does not exist", path));
		
			
			_configDir = configFile.DirectoryName;
			
			_doc = new XmlDocument ();
			_doc.Load (configFile.FullName);			
		}
		
		/// <summary>
		/// Initializes the analyzation environment
		/// </summary>
		public void Init ()
		{
			_formatter = new SimpleFormatter();
			XmlFactoryHelpers.ParseValueIncludes(_doc.DocumentElement,
				_configDir, _formatter, _values);
			
			InitRegisters();
			InitRegisterTypeResolver();
			InitFuzzLogPath();
			InitErrorLog();
			InitAnalyzers();
			
		}
		
		private void InitRegisters()
		{
			XmlElement registerFileNode = (XmlElement)_doc.DocumentElement.SelectSingleNode("RegisterFile");
			
			if(registerFileNode == null)
				throw new ArgumentException("Could not find RegisterFile-Node");
			
			string registerFilePath = _formatter.Format(registerFileNode.InnerText);
			if(!File.Exists(registerFilePath))
				throw new ArgumentException(string.Format("Specified register file '{0}' does not exist", 
				                                          registerFilePath));
			
			using(FileStream src = File.OpenRead(registerFilePath))
				_registers = StreamHelper.ReadTypedStreamSerializable<Registers>(src);
			                                                      
		}
		
		private void InitRegisterTypeResolver()
		{
			XmlElement registerTypeResolverNode = (XmlElement)
				_doc.DocumentElement.SelectSingleNode("RegisterTypeResolver");
			
			if(registerTypeResolverNode == null)
				throw new ArgumentException("Missing RegisterTypeResolver-Node");
			
			_registerTypeResolver = 
				XmlFactoryHelpers.CreateInstance<IRegisterTypeResolver>(
					registerTypeResolverNode, "name", "Param");
		}
		
		private void InitFuzzLogPath()
		{
			_sourcePath = XmlHelper.ReadString(_doc.DocumentElement, "FuzzLogPath");
			
			if(_sourcePath == null)
				throw new ArgumentException("Missing FuzzLogPath-node");
			
			_sourcePath = _formatter.Format(_sourcePath);
			
			if(Directory.Exists(_sourcePath) == false)
				throw new ArgumentException(
				      string.Format("Specified FuzzLogPath '{0}' does not exist", _sourcePath));
		}
		
		private void InitErrorLog()
		{
			_errorLog = XmlHelper.ReadString(_doc.DocumentElement, "ErrorLog");
		}
		
		private void InitAnalyzers()
		{
			_analyzers = new List<IDataAnalyzer>();
			foreach(XmlElement analyzerNode in _doc.DocumentElement.SelectNodes("AddAnalyzer"))
			{
				string analyzerId = analyzerNode.GetAttribute("name");
				
				IDataAnalyzer analyzer = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<IDataAnalyzer>(
					analyzerId);
				
				if(analyzer == null)
					throw new ArgumentException(string.Format(
						"Could not create instance from analyzer '{0}'", analyzerId));
				
				IDictionary<string, string> values = new Dictionary<string, string>();
				
				foreach(XmlElement paramNode in analyzerNode.SelectNodes("Param"))
					values.Add(paramNode.GetAttribute("name"), paramNode.InnerText);
				
				analyzer.Init(values);
				_analyzers.Add(analyzer);
			}
		}
		
		public AnalyzeController CreateAnalyzeController()
		{
			AnalyzeController ctrl = new AnalyzeController();
			ctrl.Setup(_errorLog, _sourcePath, _registers, _registerTypeResolver, _analyzers.ToArray());
			return ctrl;
		}
	}
}

