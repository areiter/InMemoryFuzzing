// RemoteProcessInfo.cs
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
using System.Text;
namespace Fuzzer.RemoteControl
{
	public class RemoteProcessInfo
	{
		private string _command;
		private int _pid;
		
		public string Command
		{
			get { return _command; }
		}
		
		public int Pid
		{
			get { return _pid;}
		}
			
		
		public RemoteProcessInfo (byte[] data, ref int currentOffset)
		{
			Int16 valueCount = BitConverter.ToInt16 (data, currentOffset);
			currentOffset += 2;
			
			for (int i = 0; i < valueCount; i++)
			{
				Int16 length = BitConverter.ToInt16 (data, currentOffset);
				currentOffset += 2;
				
				string value = Encoding.ASCII.GetString (data, currentOffset, length);
				currentOffset += length;
				
				string[] sValue = value.Split (new char[] { '=' }, 2);
				
				if (sValue.Length == 2)
				{
					switch (sValue[0])
					{
					case "cmd":
						_command = sValue[1];
						break;
					case "pid":
						int.TryParse (sValue[1], out _pid);
						break;
					}
				}
			}
				
			
		}
	}
}

