// InfoFunctionsRH.cs
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
namespace Fuzzer.TargetConnectors.GDB
{
	public class InfoFunctionsRH : GDBResponseHandler
	{
		public delegate void FunctionsIdentifiedDelegate(ISymbolTableMethod[] resolvedFunctions, ISymbolTableMethod[] unresolvedFunctions);
		
		private FunctionsIdentifiedDelegate _functionsIdentifier;
		private ISymbolTable _symbolTable;
		
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		public override string LogIdentifier 
		{
			get { return "RH_info functions"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			//Request as many lines as available
			if(allowRequestLine)
				return GDBResponseHandler.HandleResponseEnum.RequestLine;
			
			Regex rFile = new Regex(@"File\s*(?<filename>[\s*\S*]*)\:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rMethodWithDebuggingInfo = new Regex(@"(?<returntype>\S*)\s*(?<method>\S*)\([\s*\S*]*\);", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			Regex rNoDebugInfo = new Regex(@"Non-debugging symbols:", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex rMethodNoDebuggingInfo = new Regex(@"0x(?<at>\S*)\s*(?<method>\S*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			
			bool debuggingInfo = false;
			//string lastFile = "";
			List<ISymbolTableMethod> resolvedFunctions = new List<ISymbolTableMethod>();
			List<ISymbolTableMethod> unresolvedfunctions = new List<ISymbolTableMethod>();
			
			for(int i = 0; i<responseLines.Length ; i++)
			{
				string line = responseLines[i];
				
				Match mHeaderFile = rFile.Match(line);
				
				//We got a File header, this means we got debugging information
				if(mHeaderFile.Success)
				{
					//lastFile = mHeaderFile.Result("${filename}");
					debuggingInfo = true;
					continue;
				}
				
				Match mNoDebugInfo = rNoDebugInfo.Match(line);
				
				//We got linker debugging methods
				if(mNoDebugInfo.Success)
				{
					//lastFile = "";
					debuggingInfo = false;
					continue;
				}
				
				Match mMethodWithDebuggingInfo = rMethodWithDebuggingInfo.Match(line);
				if(debuggingInfo && mMethodWithDebuggingInfo.Success)
				{
					unresolvedfunctions.Add(new SymbolTableMethod(_symbolTable, mMethodWithDebuggingInfo.Result("${method}"), null));
					continue;
				}
				

				Match mMethodNoDebuggingInfo = rMethodNoDebuggingInfo.Match(line);
				if(!debuggingInfo && mMethodNoDebuggingInfo.Success)
				{
					resolvedFunctions.Add(new SymbolTableMethod(_symbolTable, mMethodNoDebuggingInfo.Result("${method}"), null));
					continue;
				}

			}
			
			_functionsIdentifier(resolvedFunctions.ToArray(), unresolvedfunctions.ToArray());
			return GDBResponseHandler.HandleResponseEnum.Handled;			
		}

		
		#endregion
		public InfoFunctionsRH (ISymbolTable symbolTable, FunctionsIdentifiedDelegate functionsIdentified, GDBSubProcess gdbProc)
			:base(gdbProc)
		{
			_functionsIdentifier = functionsIdentified;
			_symbolTable = symbolTable;
		}
	}
}

