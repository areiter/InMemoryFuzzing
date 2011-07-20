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
using System.Text;
using System.Collections.Generic;
using System.Globalization;

namespace Iaik.Utils
{

    /// <summary>
    /// Static class which provides some string helper methods
    /// </summary>
	public static class StringHelper
	{
        /// <summary>
        /// Intends each line of the source string by the specified indention string
        /// </summary>
        /// <param name="src"></param>
        /// <param name="indention"></param>
        /// <returns></returns>
		public static string IndentPerLine (string src, string indention)
		{
			StringBuilder destination = new StringBuilder ();
			
			foreach (string line in src.Split ('\n'))
			{
				destination.Append (indention);
				destination.Append (line);
				destination.Append ('\n');
			}
			
			return destination.ToString ();
		}
		
		/// <summary>
		/// Splits the specified string by the specified delimiter into key[delimiter]value
		/// </summary>
		/// <param name="s"></param>
		/// <param name="delimiter"></param>
		/// <returns></returns>
		public static KeyValuePair<string, string>? SplitToKeyValue (string s, string delimiter)
		{
			string[] splitted = s.Split (new string[] { delimiter }, 2, StringSplitOptions.RemoveEmptyEntries);
			
			if (splitted.Length != 2)
				return null;
			
			return new KeyValuePair<string, string> (splitted[0], splitted[1]);
		}
		
		public delegate string VariableDetectedCB(string name);
		
		
		/// <summary>
		/// Looks for {[..]} constructions in the specified expression,
		/// and calls the callback for each of them replacing its occurance with the
		/// return value of the callback
		/// </summary>
		/// <param name="expression"></param>
		/// <param name="cb"></param>
		/// <returns></returns>
		public static string ReplaceVariables (string expression, VariableDetectedCB cb)
		{
			SimpleFormatter f = new SimpleFormatter ();
			f.OnGetParameter += delegate(string parameterName) {
				return cb (parameterName);
			};
			
			
			return f.Format (expression);
		}
		
		public static UInt64 StringToUInt64 (string val)
		{
			NumberStyles style = NumberStyles.Number;
			int offset = 0;
			
			if(val.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
			{
				style = NumberStyles.HexNumber;
				offset = 2;
			}
			                  
			return UInt64.Parse(val.Substring(offset), style);
		}

	}
}
