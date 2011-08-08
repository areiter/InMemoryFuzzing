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
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Security.Cryptography;
using Iaik.Utils.Hash;
using System.Collections.Generic;
using Iaik.Utils;
using System.Security;
using System.Runtime.InteropServices;

namespace Iaik.Utils.Net
{

	/// <summary>
	/// Loads the specified certificate file, asks the user for the
	/// encryption password if needed and sets up the ssl connection
	/// </summary>
	/// <remarks>
	/// Allowed arguments are
	/// <list type="table"> 
	/// <item><term>host</term><description>Specifies the remote host</description></item>
	/// <item><term>port</term><description>Specifies the remote port</description></item>
	/// <item><term>client_certificate</term><description>Specifies the client certificate file to load for ssl authentication</description></item>
	/// <item><term>debug_target_host</term><description>In productive environments the target host sent for 
	/// authentication is the specified host. For debugging purpose this behaviour can be overridden to match
	/// the common name specified in the server certificate</description></item>
	/// </list>
	///</remarks>
	public class SslConnectionBuilder : IConnectionBuilder
	{
		/// <summary>
		/// Remote host
		/// </summary>
		private string _host;
		
		/// <summary>
		/// Remote port
		/// </summary>
		private int _port;
		
		/// <summary>
		/// Certificate file
		/// </summary>
		private string _certificateFile;
		
		/// <summary>
		/// Overwrites the host name used in the ssl authentication process
		/// </summary>
		private string _overwriteAuthenticationTargetHost = null;
		
		#region IConnectionBuilder implementation
		public FrontEndConnection SetupConnection ()
		{
			if(File.Exists(_certificateFile) == false)
				throw new FileNotFoundException(string.Format("Certificate file '{0}' could not be found", _certificateFile));

			
			X509Certificate2 cert;
			
			try
			{
				cert = new X509Certificate2(_certificateFile);
			}
			catch(CryptographicException)
			{
				//maybe the certificate is password protected?
				ProtectedPasswordStorage pw = 
					_settings.RequestSecret(string.Format("Enter encryption secret for certificate '{0}'", 
				                            Path.GetFileName(_certificateFile)));
				
				if(pw == null)
					throw new ArgumentException(string.Format("Could not get secret for certificate '{0}'",
											_certificateFile));
				
				//FIXME: Mono implementation of this ctor does not work
				cert = new X509Certificate2(_certificateFile, pw.ExportSecureString());
			}
			
			return new SslConnection(_host, _port, cert, _overwriteAuthenticationTargetHost);
		}
	
		
		private ConnectionBuilderSettings _settings = null;
		
		public ConnectionBuilderSettings Settings
		{	
			get { return _settings; }
			set { _settings = value; }
		}
		
		#endregion
		
		public SslConnectionBuilder(string host, int port, string certificateFile, string overwriteAuthenticationTargetHost)
		{
			_host = host;
			_port = port;
			_certificateFile = certificateFile;
			_overwriteAuthenticationTargetHost = overwriteAuthenticationTargetHost;
		}
		
		public SslConnectionBuilder(IDictionary<string, string> arguments)
			:this(DictionaryHelper.GetString("host", arguments, null),
			      DictionaryHelper.GetInt("port", arguments, -1),
			      DictionaryHelper.GetString("client_certificate", arguments, null),
			      DictionaryHelper.GetString("debug_target_host", arguments, null))
		{
		}
	}
}
