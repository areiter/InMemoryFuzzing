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



using System;
using System.Collections.Generic;
using System.Xml;

namespace Iaik.Utils
{

    /// <summary>
    /// Static class which provides some helper functions for IDictionary<,> 
    /// </summary>
	public static class DictionaryHelper
	{
        /// <summary>
        /// Gets a boolean
        /// </summary>
        /// <param name="name"></param>
        /// <param name="paramDict"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
		public static bool GetBool (string name, IDictionary<string, string> paramDict, bool defaultValue)
		{
			if (paramDict.ContainsKey (name) == false)
				return defaultValue;
			
			bool val;
			
			if (bool.TryParse (paramDict[name], out val))
				return val;
			
			if (paramDict[name] == "1")
				return true;
			else if (paramDict[name] == "0")
				return false;
			
			return defaultValue;
		}
		
        /// <summary>
        /// Gets an integer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="paramDict"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
		public static int GetInt(string name, IDictionary<string, string> paramDict, int defaultValue)
		{
			if(paramDict.ContainsKey(name) == false)
				return defaultValue;
			
			int val;
			
			if(int.TryParse(paramDict[name], out val))
				return val;
			
			return defaultValue;
		}
		
		/// <summary>
        /// Gets an integer
        /// </summary>
        /// <param name="name"></param>
        /// <param name="paramDict"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
		public static int? GetInt(string name, IDictionary<string, string> paramDict)
		{
			if(paramDict.ContainsKey(name) == false)
				return null;
			
			int val;
			
			if(int.TryParse(paramDict[name], out val))
				return val;
			
			return null;
		}

        /// <summary>
        /// Gets a string
        /// </summary>
        /// <param name="name"></param>
        /// <param name="paramDict"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
		public static string GetString(string name, IDictionary<string, string> paramDict, string defaultValue)
		{
			if(paramDict.ContainsKey(name) == false)
				return defaultValue;
			
		
			return paramDict[name];
		}
		
		public static T GetEnum<T> (string name, IDictionary<string, string> paramDict, T defaultVal)
		{
			try
			{
				return (T)Enum.Parse (typeof(T), GetString (name, paramDict, defaultVal.ToString ()));
			}
			catch (Exception)
			{
				return defaultVal;
			}
		}
		
		/// <summary>
		/// Builds a dictionary from an xml structure of the form:
		/// [root]
		///  [-nodeName- name="-name-"]-value-[/-nodeName]
		///  [-nodeName- name="-name2-"]-value2-[/-nodeName]
		///  ...
		/// [/root]
		/// </summary>
		/// <param name="root">
		/// A <see cref="XmlElement"/>
		/// </param>
		/// <param name="nodeName">
		/// A <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="IDictionary<System.String, System.String>"/>
		/// </returns>
		public static IDictionary<string, string> ReadDictionaryXml (XmlElement root, string nodeName)
		{
			Dictionary<string, string> dict = new Dictionary<string, string> ();
			
			foreach (XmlElement element in root.SelectNodes (nodeName))
				dict.Add (element.GetAttribute("name"), element.InnerText);
			
			return dict;
		}
	}
}
