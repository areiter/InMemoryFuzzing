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
using System.Linq;
using System.Text;

namespace Iaik.Utils.Net
{
    /// <summary>
    /// Base class for all connection related exceptions
    /// </summary>
    public class ConnectionException : Exception
    {
        public ConnectionException(string message)
            : base(message)
        {
          
        }
    }

    /// <summary>
    /// Is thrown if the connection to the remote host could not be established,
    /// because the remote endpoint is not available
    /// </summary>
    public class ConnectionFailureException : ConnectionException
    {
        public ConnectionFailureException(string message)
            : base(message)
        { }
    }

    /// <summary>
    /// Is thrown if the connection is rejected by the server because of
    /// some policy settings
    /// </summary>
    public class ConnectionRejectedException : ConnectionException
    {
        public ConnectionRejectedException(string message)
            : base(message)
        {
        }
    }
	
	/// <summary>
	/// Is thrown by the connection if the connection to the client has been lost
	/// </summary>
	public class DisconnectedException : ConnectionException
	{
		public DisconnectedException()
			:base ("This was not a graceful connection shutdown")
		{
		}
	}
}
