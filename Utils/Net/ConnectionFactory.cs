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
using Iaik.Utils.CommonFactories;

namespace Iaik.Utils.Net
{


	public static class ConnectionFactory
	{
		public static FrontEndConnection CreateFrontEndConnection(
		           string identifier,
		           ConnectionBuilderSettings settings, 
		           params object[] parameters)
		{
			Type t = GenericClassIdentifierFactory.FindTypeForIdentifier<FrontEndConnection>(identifier);
			
			if( t == null)
				return null;
			
			FrontEndConnectionAttribute attr = (FrontEndConnectionAttribute)t.GetCustomAttributes(typeof(FrontEndConnectionAttribute), false)[0];
			
			if(attr.ConnectionBuilder != null)
			{
				IConnectionBuilder connectionBuilder = (IConnectionBuilder)
					GenericClassIdentifierFactory.CreateInstance(attr.ConnectionBuilder, parameters);
				
				connectionBuilder.Settings = settings;
				return connectionBuilder.SetupConnection();
			}
			else
				return GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<FrontEndConnection>(identifier, parameters);
			
		}
	}
}
