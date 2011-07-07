// ProgramErrorAnalyzer.cs
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
using System.IO;
using Fuzzer.TargetConnectors;
using Iaik.Utils;
using System.Xml;
namespace Fuzzer.Analyzers
{
	/// <summary>
	/// The propably simplest analyzer. It looks for .errorlog files with the specified
	/// prefix. 
	/// error log files are generated if the program crashes during the fuzzing process.
	/// It contains the termination reason (error, exit,...) some status info like termination signal,
	/// or exit code
	/// </summary>
	/// <remarks>
	/// <para>Uses the files: *.errorlog</para>
	/// </remarks>
	public class ProgramErrorAnalyzer : BaseDataAnalyzer
	{
		public override string LogIdentifier 
		{
			get { return "ProgramErrorAnalyzer"; }
		}		
		
		public override void Analyze (AnalyzeController ctrl)
		{
			FileInfo errorlogFile = GenerateFile ("errorlog");
			
			if (errorlogFile.Exists)
			{
				using (FileStream fs = errorlogFile.OpenRead ())
				{
					StopReasonEnum stopReason = (StopReasonEnum)StreamHelper.ReadInt32 (fs);
					Int64 status = StreamHelper.ReadInt64 (fs);
					UInt64 address = StreamHelper.ReadUInt64 (fs);
					
					XmlElement node = GenerateNode ("Item");
					XmlHelper.WriteString (node, "Prefix", _prefix);
					XmlHelper.WriteString (node, "StopReason", stopReason.ToString ());
					XmlHelper.WriteInt64 (node, "Status", status);
					XmlHelper.WriteUInt64 (node, "Address", address);
				}
			}
		}

	}
}

