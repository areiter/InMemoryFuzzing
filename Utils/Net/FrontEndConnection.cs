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
// Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;

namespace Iaik.Utils.Net
{
    /// <summary>
    /// Implements the IFrontEndConnection interface for common usage.
    /// This class can not be instantiated, use one of the implementations.
    /// Derived classes need to specify the FrontEndConnectionAttribute to
    /// identify the connections. If the attribute is not defined a 
    /// <see>NotSupportedException</see> is thrown
    /// </summary>
    public abstract class FrontEndConnection : Stream
    {
		/// <summary>
		/// Raised when the connection to a remote end point has been established
		/// </summary>
		public event Action<FrontEndConnection> ConnectionEstablished;
		
		/// <summary>
		/// Raised when the connection to the remote end is closed
		/// </summary>
		public event Action<FrontEndConnection> Disconnected;
		
		
		/// <summary>
		/// Logger
		/// </summary>
		protected ILog _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

				
        /// <summary>
        /// Locks the connection, according to microsoft lock(this) is bad practise ;-)
        /// </summary>
        protected object _syncLock = new object();

	
		/// <summary>
		/// Indicates if the Connection is connected
		/// </summary>
		public abstract bool Connected {get;}
       
        /// <summary>
        /// Connects to the remote host
        /// </summary>
        public abstract void Connect();

        /// <summary>
        /// Closes the connection to the remote host
        /// </summary>
        public override abstract void Close();

		
        #region IDisposable Members
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
		}

        #endregion

        #region Stream overrides
		public override bool CanRead 
		{
			get { return true; }
		}
		
		public override bool CanWrite 
		{
			get { return true; } 
		}
		
		public override bool CanSeek 
		{
			get { return false; }
		}

		public override void WriteByte (byte value)
		{
			Write(new byte[]{value}, 0, 1);		
		}
		
		public override int ReadByte ()
		{
			byte[] buffer = new byte[1];
			Read(buffer, 0, 1);
			return buffer[0];
		}

		#region Unsupported
		public override long Length 
		{
			get { throw new System.NotSupportedException();}
		}

		public override long Position 
		{
			get { throw new System.NotSupportedException();}
			set { throw new System.NotSupportedException();}
		}

		public override long Seek (long offset, SeekOrigin origin)
		{
			throw new System.NotSupportedException();
		}

		public override void SetLength (long value)
		{
			throw new System.NotSupportedException();
		}

		#endregion
		
		
		public abstract override void Flush();
		
		public abstract override void Write(byte[] buffer, int offset, int length);
		
		public abstract override int Read(byte[] buffer, int offset, int length);
	
		#endregion
		
		/// <summary>
		/// Gets the unique identifier of this connection,
		/// defined in the FrontEndConnectionAttribute
		/// </summary>
		public string Name
		{
			get
			{
				//No bounds checking is required because this is done in the ctor
				return ((FrontEndConnectionAttribute)this.GetType().
				        GetCustomAttributes(typeof(FrontEndConnection), false)[0]).Identifier;
			}
		}
		
		protected void RaiseConnectedEvent()
		{
			if(ConnectionEstablished != null)
				ConnectionEstablished(this);
		}
		
		protected void RaiseDisconnectedEvent()
		{
			if(Disconnected != null)
				Disconnected(this);
		}
		
		public FrontEndConnection()
		{
			//Checks if the resulting type has the FrontEndConnectionAttribute defined
			object[] attributes = this.GetType().GetCustomAttributes(typeof(FrontEndConnectionAttribute), false);
			if(attributes == null || attributes.Length == 0)
				throw new NotSupportedException("FrontEndConnectionAttribute is not defined");
			else if(attributes.Length > 1)
				throw new NotSupportedException("FrontEndConnectionAttribute is defined more than once");
		}
    }
}
