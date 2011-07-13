// XmlFactoryHelpers.cs
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
using Iaik.Utils;
using System.Xml;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using Iaik.Utils.CommonFactories;
namespace Fuzzer.XmlFactory
{
	public static class XmlFactoryHelpers
	{
		/// <summary>
		/// Parses [Value] Subtags of Option files and inserts its values to formatter and values
		/// </summary>
		/// <param name="rootElement"></param>
		/// <param name="configDir"></param>
		/// <param name="formatter"></param>
		/// <param name="values"></param>
		public static void ParseValueIncludes(XmlElement rootElement, string configDir, 
			                           SimpleFormatter formatter, IDictionary<string, string> values )
		{
			
			//First resolve all includes
			foreach(XmlElement includeNode in rootElement.SelectNodes("Include"))
			{
				string fullPath;
				if(Path.IsPathRooted(includeNode.InnerText))
					fullPath = includeNode.InnerText;
				else
					fullPath = Path.Combine(configDir, includeNode.InnerText);
				
				if(File.Exists(fullPath) == false)
					throw new FileNotFoundException(string.Format(
						"Could not find include file '{0}'", fullPath));
				else
				{
					XmlDocument includeDoc = new XmlDocument();
					
					try
					{
						includeDoc.Load(fullPath);
						
						foreach(XmlElement valueNode in includeDoc.DocumentElement.SelectNodes("Value"))
						{
							string name = valueNode.GetAttribute("name");
							string value = valueNode.InnerText;
				
							if(values.ContainsKey(name))
								values[name] = value;
							else
								values.Add(name, value);
							
							formatter.DefineTextMacro(name, value);
							Console.WriteLine("Added {0}={1}", name, value);
						}
					}
					catch(Exception ex)
					{
						throw new ArgumentException(string.Format(
							"Could not load include file '{0}' (Exception: {1})", fullPath, ex));
					}
				}
			}

		}
		
		public static T CreateInstance<T>(XmlElement rootElement, string nameArg, string paramElementName) where T: class
		{
			string classIdentifier = rootElement.GetAttribute(nameArg);
			
			IDictionary<string, string> values = new Dictionary<string, string>();
			
			foreach(XmlElement paramNode in rootElement.SelectNodes(paramElementName))
			{
				string paramName = paramNode.GetAttribute(nameArg);				
				values.Add(paramName, paramNode.InnerText);
			}
			
			if(values.Count > 0)
				return (T)GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<T>(
					classIdentifier, values);
			else
				return (T)GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<T>(classIdentifier);
				                                      
		}
	}
}

