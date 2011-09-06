// ScriptedDataGeneratorEnvironment.cs
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
using System.Collections.Generic;
namespace Fuzzer.Scripting.Environments
{
	public class ScriptedDataGeneratorEnvironment : BasicEnvironment
	{
		
		public delegate bool ValueIsSetDelegate(string name);
		public delegate object GetValueDelegate(string name);
		public delegate void SetValueDelegate(string name, object value);
		
		private Action<byte[]> _setDataCallback;
		
		/// <summary>
		/// Contains persistive script values
		/// </summary>
		private Dictionary<string, object> _scriptValues = new Dictionary<string, object>();
		
		public ScriptedDataGeneratorEnvironment (
			ScriptingLanguage language, 
			Action<byte[]> setDataCallback,
			IDictionary<string, string> config)
            : base(language)
		{
			_extraImports.Add ("Iaik.Utils");
			
			_setDataCallback = setDataCallback;
			
			foreach (KeyValuePair<string, string> kvPair in config)
			{
				if (kvPair.Key.StartsWith ("scriptval_"))
					_scriptValues.Add (kvPair.Key, kvPair.Value);
			}
			
			MethodInfo setDataMethodInfo = new MethodInfo ("SetData", null, 
				new ParameterInfo ("data", typeof(byte[])));
			GlobalMethods.Add (setDataMethodInfo);
			SetMethod (setDataMethodInfo, _setDataCallback);
			
			MethodInfo valueIsSetMethodInfo = new MethodInfo ("IsValueSet", typeof(bool), 
				new ParameterInfo ("name", typeof(string)));
			GlobalMethods.Add (valueIsSetMethodInfo);
			SetMethod (valueIsSetMethodInfo, new ValueIsSetDelegate (ValueIsSet));
		
			MethodInfo getValueMethodInfo = new MethodInfo ("GetValue", typeof(object), 
				new ParameterInfo ("name", typeof(string)));
			GlobalMethods.Add (getValueMethodInfo);
			SetMethod (getValueMethodInfo, new GetValueDelegate (GetValue));
			
			MethodInfo setValueMethodInfo = new MethodInfo ("SetValue", null,
				new ParameterInfo ("name", typeof(string)),
				new ParameterInfo ("value", typeof(object)));			
			GlobalMethods.Add (setValueMethodInfo);
			SetMethod (setValueMethodInfo, new SetValueDelegate (SetValue));
		}
		
		private bool ValueIsSet (string name)
		{
			return _scriptValues.ContainsKey (name);
		}
		
		private object GetValue (string name)
		{
			if (ValueIsSet (name) == false)
				return null;
			else
				return _scriptValues[name];
		}
		
		public void SetValue (string name, object value)
		{
			if (ValueIsSet (name))
				_scriptValues[name] = value;
			else
				_scriptValues.Add (name, value);
		}
	}
}

