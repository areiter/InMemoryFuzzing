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
namespace Fuzzer.TargetConnectors.GDB
{
	public class InfoFunctionsRH : GDBResponseHandler
	{
		#region implemented abstract members of Fuzzer.TargetConnectors.GDB.GDBResponseHandler
		protected override string LogIdentifier 
		{
			get { return "RH_info functions"; }
		}
		
		
		public override GDBResponseHandler.HandleResponseEnum HandleResponse (GDBSubProcess subProcess, string[] responseLines, bool allowRequestLine)
		{
			Regex fileRead = new Regex(@"Reading symbols from [\S*\s*]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			Regex fileNotFound = new Regex(@"[\S*\s*]*: No such file or directory.\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
			for(int i = 0; i<responseLines.Length ; i++)
			{
				string line = responseLines[i];
				
				Match m = fileNotFound.Match(line);
				if(m.Success)
				{
					_fileLoaded(false);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}
				
				m = fileRead.Match(line);
				if(m.Success)
				{
					_fileLoaded(true);
					return GDBResponseHandler.HandleResponseEnum.Handled;
				}				
			}
			
			return GDBResponseHandler.HandleResponseEnum.NotHandled;			
		}
		
		}
		
		#endregion
		public InfoFunctionsRH ()
		{
		}
	}
}

