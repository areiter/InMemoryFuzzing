// RemoteExecCommand.cs
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
using System.Collections.Generic;
using System.Text;
namespace Fuzzer.RemoteControl
{
	public class RemoteExecCommand : RemoteCommand
	{
		private string _name;
		private string _path;
		private IList<string> _args;
		private IList<string> _envp;
		
		
		public string Name
		{
			get{ return _name; }
		}
		
		public string Path
		{
			get{ return _path;}
		}
		
		public IList<string> Args
		{
			get{ return _args; }
		}
		
		public IList<string> EnvP
		{
			get{ return _envp;}
		}
			
		
		public RemoteExecCommand (string name, string path, IList<string> args, IList<string> envp)
		{
			_name = name;
			_path = path;
			_args = args;
			_envp = envp;
		}

		
		#region implemented abstract members of Fuzzer.RemoteControl.RemoteCommand
		public override string Receiver 
		{
			get { return "EXEC"; }
		}
		
		
		public override byte[] Data 
		{
			get 
			{
				List<byte> data = new List<byte> ();
				
				data.AddRange (BitConverter.GetBytes ((Int16)Encoding.ASCII.GetByteCount (_name)));
				data.AddRange (Encoding.ASCII.GetBytes (_name));
				data.AddRange (BitConverter.GetBytes ((Int16)Encoding.ASCII.GetByteCount (_path)));
				data.AddRange (Encoding.ASCII.GetBytes (_path));
				
				data.AddRange (BitConverter.GetBytes ((Int16)_args.Count));
				foreach (string arg in _args)
				{
					data.AddRange (BitConverter.GetBytes ((Int16)Encoding.ASCII.GetByteCount (arg)));
					data.AddRange (Encoding.ASCII.GetBytes (arg));
				}
				
				data.AddRange (BitConverter.GetBytes ((Int16)_envp.Count));
				foreach (string env in _envp) {
					data.AddRange (BitConverter.GetBytes ((Int16)Encoding.ASCII.GetByteCount (env)));
					data.AddRange (Encoding.ASCII.GetBytes (env));
				}

				return data.ToArray ();
			}
		}
		
		#endregion
	}
}

