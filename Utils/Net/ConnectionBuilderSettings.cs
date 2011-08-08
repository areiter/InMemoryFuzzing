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

namespace Iaik.Utils.Net
{

	/// <summary>
	/// All settings required by the connection builders
	/// </summary>
	public class ConnectionBuilderSettings
	{

		private RequestSecretDelegate _requestSecret = null;
		
		/// <summary>
		/// Used to request a protected secret from the user
		/// </summary>
		public RequestSecretDelegate RequestSecret 
		{
			get { return _requestSecret; }
			set { _requestSecret = value;}
		}

		public ConnectionBuilderSettings ()
		{
		}
		
		public ConnectionBuilderSettings(RequestSecretDelegate requestSecret)
		{
			_requestSecret = requestSecret;
		}
	}
}
