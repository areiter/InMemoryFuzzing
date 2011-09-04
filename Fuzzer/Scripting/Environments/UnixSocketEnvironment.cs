// UnixSocketEnvironment.cs
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
using DevEck.ScriptingEngine.Environment.Basic;
using DevEck.ScriptingEngine.Environment;
using System.IO;
using Fuzzer.FuzzLocations;
namespace Fuzzer.Scripting.Environments
{
	public enum UnixSocketHookType
	{
		BeforeSocketCreation,
		AfterSocketCreation,
		AfterSocketConnect,
		BeforeSocketClose,
		AfterSocketClose
	}
	
	public class UnixSocketEnvironment : BasicEnvironment
	{
		public delegate UnixSocketHookType GetHookTypeDelegate();
		
		private UnixSocketFuzzLocation _fuzzLocation;
		private UnixSocketHookType _hookType;
		
		public UnixSocketHookType HookType
		{
			get { return _hookType; }
			set { _hookType = value;}
		}
		
		
		public UnixSocketEnvironment (ScriptingLanguage language, UnixSocketFuzzLocation fuzzLocation)
            : base(language)
		{
			
			_fuzzLocation = fuzzLocation;
			
			_extraImports.Add ("Fuzzer.Scripting.Environments");
			
			ParameterInfo fuzzLocationParameterInfo = new ParameterInfo ("fuzzLocation", typeof(UnixSocketFuzzLocation));
			base.GlobalParameters.Add (fuzzLocationParameterInfo);
			base.SetParameter (fuzzLocationParameterInfo, fuzzLocation);
			
			MethodInfo hookTypeGetterInfo = new MethodInfo ("HookType", typeof(UnixSocketHookType));
			base.GlobalMethods.Add (hookTypeGetterInfo);
			base.SetMethod (hookTypeGetterInfo, new GetHookTypeDelegate (GetHookType));
		
		}
		
		private UnixSocketHookType GetHookType ()
		{
			return _hookType;
		}
	}
}

