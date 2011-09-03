// ScriptEvaluator.cs
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
using DevEck.ScriptingEngine.Environment;
using System.Xml;
using System.Collections.Generic;
using Iaik.Utils;
using Iaik.Utils.CommonFactories;
using System.IO;

namespace Fuzzer.Scripting
{
	public class ScriptEvaluator<T> where T: IScriptEnvironment
	{
		private T _environment;
		private IDictionary<string, string> _config;
		
		public ScriptEvaluator (IDictionary<string, string> config, params object[] ctorParams)
		{
			_config = config;
			
			if(DictionaryHelper.GetBool("enable_scripting", config, false) == false)
			{
				_environment = null;
				return;
			}
			
			if(config.ContainsKey("script_lang") == false)
				throw new ArgumentException("'script_lang' not defined");
			ScriptingLanguage scriptLang =
				DictionaryHelper.GetEnum<ScriptingLanguage>("script_lang", config, ScriptingLanguage.CSharp);
			
			_environment = GenericClassIdentifierFactory.CreateInstance(typeof(T), scriptLang, ctorParams);
			
			if(_environment == null)
				throw new ArgumentException("Could not create scripting environment");
			
			
			if(config.ContainsKey("script_file"))
			{
				_environment2File.ReadAllText(DictionaryHelper.GetString("script_file", config, null));
				
			}
			else if(config.Contains("script_code"))
			{
			}
			else
				throw new ArgumentException("No script_file or script_code argument found");
		}
	}
}

