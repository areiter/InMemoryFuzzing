// BaseDataAnalyzer.cs
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
using log4net;
using System.Collections.Generic;
namespace Fuzzer.Analyzers
{
	public abstract class BaseDataAnalyzer : IDataAnalyzer
	{
		protected ILog _log;
		
		protected string _path = null;
		protected string _prefix;	
		protected XmlElement _errorlogRoot;

		
		public BaseDataAnalyzer ()
		{
			_log = LogManager.GetLogger (LogIdentifier);
		}
		
		#region IDataAnalyzer implementation
		public string Path
		{
			get { return _path; }
			set { _path = value; }
		}
		
		public string Prefix 
		{
			get { return _prefix; }
			set { _prefix = value; }
		}
		
		public abstract string LogIdentifier{ get; }
		
		public void Init(IDictionary<string, string> configValues)
		{
		}
		
		public void Setup (XmlElement errorlogRoot)
		{
			_errorlogRoot = errorlogRoot;
		}
		
		public abstract void Analyze(AnalyzeController ctrl);
		#endregion
		
		protected FileInfo GenerateFile (string fileExtension)
		{
			return new FileInfo(System.IO.Path.Combine (_path, _prefix + "." + fileExtension));
		}
		
		protected XmlElement GenerateNode (string type)
		{
			XmlElement newNode = (XmlElement)_errorlogRoot.AppendChild (_errorlogRoot.OwnerDocument.CreateElement ("item"));
			newNode.Attributes.Append (newNode.OwnerDocument.CreateAttribute ("type")).Value = type;
			newNode.Attributes.Append (newNode.OwnerDocument.CreateAttribute ("prefix")).Value = _prefix;
			return newNode;
		}
	}
}

