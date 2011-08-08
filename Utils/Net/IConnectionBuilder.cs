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
using Iaik.Utils.Hash;

namespace Iaik.Utils.Net
{
	/// <summary>
	/// Requests a protected secret from the client
	/// </summary>
	public delegate ProtectedPasswordStorage RequestSecretDelegate(string userHint);
	
	/// <summary>
	/// Some connection types may require user interaction
	/// (e.g. ssl connection requires the user to enter
	/// the certificate password). If a FrontEndConnectionAttribute
	/// is associated with a connection builder, the connection builder
	/// is used to setup the connection
	/// </summary>
	public interface IConnectionBuilder
	{

		/// <summary>
		/// Settings required by the connection builder
		/// </summary>
		ConnectionBuilderSettings Settings{get; set;}
		
		/// <summary>
		/// Creates the associated Frontend connection
		/// </summary>
		/// <returns></returns>
		FrontEndConnection SetupConnection();
	}
}
