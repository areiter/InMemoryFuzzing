// XmlFuzzFactory.cs
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
using System.Xml;
namespace Fuzzer.XmlFactory
{
	/// <summary>
	/// Creates all components of the fuzzer by using a supplied xml description file
	/// See SampleConfigs/SampleFuzzDescription.xml for a fully documented sample 
	/// description file
	/// </summary>
	/// <remarks>
	/// The file is parsed at object creation, which means that the syntactical correctness is
	/// checked. But it is not checked that all required sections are available
	/// </remarks>
	public class XmlFuzzFactory
	{
		/// <summary>
		/// The description document
		/// </summary>
		private XmlDocument _doc;
		
		public XmlFuzzFactory (string path)
		{
			if (File.Exists (path) == false)
				throw new FileNotFoundException (string.Format ("The specified xml description file '{0}' does not exist", path));
			
			
			_doc = new XmlDocument ();
			_doc.Load (path);			
		}
	}
}

