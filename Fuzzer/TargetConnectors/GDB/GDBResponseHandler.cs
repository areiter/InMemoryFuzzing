// GDBResponseHandler.cs
//  
//  Author:
//       Andreas Reiter <andreas.reiter@student.tugraz.at>
// 
//  Copyright 2011  Andreas Reiter
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
using System;
using log4net;
namespace Fuzzer.TargetConnectors.GDB
{
	/// <summary>
	/// Response handlers for GDB Commands or standalone response handlers
	/// They get the last response line from gdb and can decide whether this line
	/// is destined for this handler, the response is not complete yet (requests another response line)
	/// or if this line is not of interest for this handler at all
	/// </summary>
	public abstract class GDBResponseHandler
	{
		public enum HandleResponseEnum
		{
			/// <summary>
			/// Line got handled by this response handler and is out of the game ^^
			/// </summary>
			Handled,
			
			/// <summary>
			/// Line is not if interest for this handler
			/// </summary>
			NotHandled,
			
			/// <summary>
			/// Response handler requests another line.
			/// Requesting a new line is only valid if gdb is not in "ReadyForInput state ( "(gdb)" )
			/// </summary>
			RequestLine
		}
		
		protected ILog _logger = null;
		protected GDBSubProcess _gdbProc;
		
		public GDBResponseHandler (GDBSubProcess gdbProc)
		{
			_logger = LogManager.GetLogger(LogIdentifier);
			_gdbProc = gdbProc;
		}
		
		public abstract string LogIdentifier{get;}
		
		/// <summary>
		/// Handles the specified response
		/// </summary>
		/// <param name="connector">Connector used</param>
		/// <param name="responseLines">At least a single response line</param>
		/// <param name="allowRequestLine">Specifies if requesting more response lines is allowed</param>
		/// <returns></returns>
		public abstract HandleResponseEnum HandleResponse(GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine);
	}
}

