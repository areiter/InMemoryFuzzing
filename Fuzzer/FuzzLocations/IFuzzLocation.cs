// IFuzzLocation.cs
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
using System.Xml;
using Fuzzer.TargetConnectors;
using Fuzzer.FuzzDescriptions;
using Fuzzer.DataLoggers;
using System.Collections.Generic;
namespace Fuzzer.FuzzLocations
{
	public enum LoggerDestinationEnum
	{
		DataGenLogger
	}
	
	
	/// <summary>
	/// Specifies a single fuzzlocation e.g. a memory location, a socket file, a tcp port,...
	/// and its data generator, trigger and stop condition
	/// </summary>
	public interface IFuzzLocation : IDisposable
	{
		
		/// <summary>
		/// Returns if the fuzz location has already finished its operation, which
		/// means that the stop condition has been reached.
		/// </summary>
		bool IsFinished{ get; }
		
		
		/// <summary>
		/// Checks if the fuzz location wants to be invoked
		/// </summary>
		/// <param name="trigger"></param>
		/// <param name="data"></param>
		/// <returns></returns>
		bool IsTriggered(TriggerEnum trigger, object data);
			
		/// <summary>
		/// Sets the logger for the specified destination if fuzz location supports it
		/// </summary>
		/// <param name="loggerDestination"></param>
		/// <param name="logger"></param>
		void SetLogger(LoggerDestinationEnum loggerDestination, IDataLogger logger);
		
		/// <summary>
		/// Another Fuzzer run gets started
		/// </summary>
		void NextFuzzRun();
		
		/// <summary>
		/// Initializes the fuzz location
		/// </summary>
		/// <param name="fuzzLocationRoot"></param>
		/// <remarks>
		/// For simplicity it is assumed that an xml based configuration is used, if other technologies are used,
		/// it should always be possible to convert the configuration to xml configuration
		/// </remarks>
		void Init(XmlElement fuzzLocationRoot, ITargetConnector connector, Dictionary<string, IFuzzLocation> predefinedFuzzers );
		
		/// <summary>
		/// Initializes the changeable content of the fuzz location (Data Generator,...)
		/// </summary>
		/// <param name="fuzzLocationRoot">
		/// A <see cref="XmlElement"/>
		/// </param>
		/// <returns>Returns identifier to select the desired instance of the changeable attributes (provide in Run)</returns>
		object InitChanges(XmlElement fuzzLocationRoot);
		

		void ApplyChangeableId (object id);
		
		/// <summary>
		/// Invokes the fuzz location
		/// </summary>
		/// <param name="ctrl">
		/// A <see cref="FuzzController"/>
		/// </param>
		void Run(FuzzController ctrl);
	}
}

