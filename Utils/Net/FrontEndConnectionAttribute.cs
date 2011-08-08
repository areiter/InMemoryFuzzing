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


// 
// 
//  Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
//  Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;
using Iaik.Utils.CommonAttributes;

namespace Iaik.Utils.Net
{

	/// <summary>
	/// Attribute that is defined for all FrontEndConnection
	/// </summary>
	public class FrontEndConnectionAttribute : ClassIdentifierAttribute
	{
		private Type _connectionBuilder = null;
		
		/// <summary>
		/// If the attribute has an associated connection builder,
		/// use it to build connection types with a more complex setup process
		/// </summary>
		public Type ConnectionBuilder
		{
			get{ return _connectionBuilder;}
		}
		
		
		public FrontEndConnectionAttribute (string connectionName)
			:base(connectionName)
		{
		}
		
		public FrontEndConnectionAttribute(string connectionName, Type connectionBuilder)
			:this(connectionName)
		{
			_connectionBuilder = connectionBuilder;
		}
		
	}
}
