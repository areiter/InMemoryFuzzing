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
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Iaik.Utils.Hash;
using System.Collections.Generic;
using Iaik.Utils;
using System.Net.Security;
using System.Security.Authentication;
using Mono.Security.Protocol.Tls;

namespace Iaik.Utils.Net
{
	
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
	///
	/// To use certificates they need to be in the "Trust" key store, use certmgr to manage the key stores.
	/// The server certificate needs to have the Netscape Cert Type: SSL Server and the
	/// client certificate needs to have the Netscape Cert Type: SSL Client attribute,
	/// otherwise the certificate is rejected
	///</remarks>
	[FrontEndConnection("net/ssl_socket", typeof(SslConnectionBuilder))]
	public class SslConnection : TcpSocketConnection
	{
		
		/// <summary>
		/// The secure stream
		/// </summary>
		private SslStreamBase _sslStream = null;
		
		/// <summary>
		/// Client certificate
		/// </summary>
		private X509Certificate2 _certificate = null;
		
		/// <summary>
		/// For debugging purpose the targetHost in AuthenticateAsClient
		/// can be overwritten
		/// </summary>
		private string _overwriteAuthenticationTargetHost = null;
		
		/// <summary>
		/// Gets the client certificate
		/// </summary>
		public X509Certificate ClientCertificate
		{
			get
			{ 
				if(_sslStream is SslServerStream)
					return ((SslServerStream)_sslStream).ClientCertificate;
				else
					return null;	
			}						
		}
		
		
		public SslConnection(string host, int port, X509Certificate2 clientCertificate)
			:base(host, port)
		{
			_certificate = clientCertificate;
		}
		
		public SslConnection(string host, int port, X509Certificate2 clientCertificate, 
		                     string overwriteAuthenticationTargetHost)
			:this(host, port, clientCertificate)
		{
			_overwriteAuthenticationTargetHost = overwriteAuthenticationTargetHost;
		}

		
		
		public SslConnection(Socket socket, SslStreamBase stream)
			:base(socket)
		{
			_sslStream = stream;		 
		}

		public override void Connect ()
		{
			//Connect raw tcp socket
			base.Connect();
			
			X509Certificate2Collection certCollection = new X509Certificate2Collection(_certificate);
			
			_sslStream = new SslClientStream(
			   new NetworkStream(_socket, true), 
			   _overwriteAuthenticationTargetHost != null?_overwriteAuthenticationTargetHost:_remoteHost,
			   true,
			   SecurityProtocolType.Tls,
			   certCollection);  
		
			((SslClientStream)_sslStream).CheckCertRevocationStatus = true;
			((SslClientStream)_sslStream).PrivateKeyCertSelectionDelegate +=
				 delegate (X509Certificate cert, string targetHost) 
			{
				X509Certificate2 cert2 = _certificate as X509Certificate2 ?? new X509Certificate2 (_certificate);
				return cert2 != null ? cert2.PrivateKey : null;
			};
			
			((SslClientStream)_sslStream).ClientCertSelectionDelegate += SelectLocalCertificate;
			((SslClientStream)_sslStream).ServerCertValidationDelegate += ValidateRemoteCertificate;
			
			_sslStream.Write(new byte[0], 0, 0);
			
			 
		}

	
		
		public override int Read (byte[] buffer, int offset, int length)
		{
			try
			{
				int read = _sslStream.Read(buffer, offset, length);
				if(read != length)
					throw new ArgumentException("Could not read enough bytes from ssl stream, maybe we got disconnected?");
				
				return read;
			}
			catch(Exception e)
			{
				_logger.Fatal(e);
				_logger.Fatal("Closing connection");
				RaiseDisconnectedEvent();
				throw new DisconnectedException();
			}
		}


		public override void Write (byte[] buffer, int offset, int length)
		{
			try
			{
				_sslStream.Write(buffer, offset, length);
			}
			catch(Exception e)
			{
				_logger.Fatal(e);
				_logger.Fatal("Closing connection");
				RaiseDisconnectedEvent();
				throw new DisconnectedException();
			}
		}


		/// <summary>
		/// Validates the certificate of the remote host
		/// </summary>
		/// <param name="certificate"></param>
		/// <param name="certificateErrors"></param>
		/// <returns></returns>
		private bool ValidateRemoteCertificate(X509Certificate certificate, int[] certificateErrors)
		{
			if(certificateErrors.Length == 0)
				return true;
			
			foreach(int certError in certificateErrors)
				_logger.Fatal(CertificateErrorCodeToMessage(certError));
			
			return false;
		}
		

		/// <summary>
		/// Selects a certificate to authenticate with for the host
		/// </summary>
		/// <param name="clientCertificates"></param>
		/// <param name="serverCertificate"></param>
		/// <param name="targetHost"></param>
		/// <param name="serverRequestedCertificates"></param>
		/// <returns></returns>
		private X509Certificate SelectLocalCertificate(
		       	X509CertificateCollection clientCertificates, 
				X509Certificate serverCertificate, 
				string targetHost, 
				X509CertificateCollection serverRequestedCertificates)
		{
			return clientCertificates[0];
		}
		 

		
		public static string CertificateErrorCodeToMessage(int errorCode)
		{
			switch((uint)errorCode)
			{
			case 0x800B0106: //CERT_E_PURPOSE
				return "The certificate is used for puprose other than specifid by the CA (CERT_E_PURPOSE)";
			case 0x800B0101: //CERT_E_EXPIRED
				return "The certificate expired (CERT_E_EXPIRED)";
			case 0x800B0102: //CERT_E_VALIDITYPERIODNESTING
				return "The validity periods of the certification chain do not nest correctly. (CERT_E_VALIDITYPERIODNESTING)";
			case 0x800B0103: //CERT_E_ROLE
				return "A certificate that can only be used as an end-entity is being used as a CA or visa versa.(CERT_E_ROLE)";
			case 0x800B0104: //CERT_E_PATHLENCONST
				return "A path length constraint in the certification chain has been violated(CERT_E_PATHLENCONST)";
			case 0x800B0105: //CERT_E_CRITICAL
				return "A certificate contains an unknown extension that is marked 'critical'.(CERT_E_CRITICAL)";
			case 0x800B0107: //CERT_E_ISSUERCHAINING
				return "A parent of a given certificate in fact did not issue that child certificate.(CERT_E_ISSUERCHAINING)";
			case 0x800B0108: //CERT_E_MALFORMED
				return "A certificate is missing or has an empty value for an important field, such as a subject or issuer name.(CERT_E_MALFORMED)";
			case 0x800B010: //CERT_E_UNTRUSTEDROOT
				return "A certificate chain processed, but terminated in a root certificate which is not trusted by the trust provider.(CERT_E_UNTRUSTEDROOT)";
			case 0x800B010A: //CERT_E_CHAINING
				return "A certificate chain could not be built to a trusted root authority(CERT_E_CHAINING)";
			default:
				return string.Format("An unknown certificate error occured ({0})", errorCode);
			}
		}
	}
}
