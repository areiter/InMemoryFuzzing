// XmlFuzzFactory.cs
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
using System.Xml;
using Fuzzer.RemoteControl;
using Iaik.Utils;
using System.Collections;
using System.Collections.Generic;
using Fuzzer.TargetConnectors;
using Iaik.Utils.CommonFactories;
using Fuzzer.DataGenerators;
using Fuzzer.FuzzDescriptions;
using Fuzzer.DataLoggers;
using Fuzzer.FuzzLocations;
namespace Fuzzer.XmlFactory
{
	/// <summary>
	/// Creates all components of the fuzzer by using a supplied xml description file
	/// See SampleConfigs/SampleFuzzDescription.xml for a fully documented sample 
	/// description file
	/// </summary>
	/// <remarks>
	/// The file is parsed at object creation, which means that the syntactical correctness is
	/// checked. But it is not checked that all required sections are available
	/// </remarks>
	public class XmlFuzzFactory
	{
		public enum ExecutionTriggerEnum
		{
			/// <summary>
			/// Executes program once on initialization
			/// </summary>
			Immediate,
			
			/// <summary>
			/// Executes program at the beginning of every fuzzer run
			/// </summary>
			OnFuzzStart,
			
			/// <summary>
			/// Executes program at the end of every fuzzer run
			/// </summary>
			OnFuzzStop
		}
		
		/// <summary>
		/// The description document
		/// </summary>
		private XmlDocument _doc;
		
		/// <summary>
		/// Directory where the config file is located
		/// </summary>
		private string _configDir;
		
		/// <summary>
		/// Connection to the target system for remote controlling
		/// </summary>
		private RemoteControlProtocol _remoteControlProtocol = null;
		
		/// <summary>
		/// Connector to the fuzzing stub
		/// </summary>
		private ITargetConnector _connector = null;
		
		/// <summary>
		/// Contains all triggered executions
		/// </summary>
		private IDictionary<ExecutionTriggerEnum, List<RemoteExecCommand>> _triggeredExecutions =
			new Dictionary<ExecutionTriggerEnum, List<RemoteExecCommand>>();
				
		/// <summary>
		/// Contains informations about the current program being launched (on the remote side)
		/// </summary>
		private RemoteExecutionInfo _currentRemoteExecInfo = null;
		
		/// <summary>
		/// contains all defined fuzz descriptions
		/// </summary>
		private List<FuzzDescriptionInfo> _fuzzDescriptions = new List<FuzzDescriptionInfo>();
		
		/// <summary>
		/// Contains all defined loggers
		/// </summary>
		private List<IDataLogger> _loggers = new List<IDataLogger>();
		
		/// <summary>
		/// Destination for log operations
		/// </summary>
		private string _logDestination = null;
		
		/// <summary>
		/// Contains all values included in the configuration file
		/// </summary>
		private IDictionary<string, string> _values = new Dictionary<string, string>();
		
		/// <summary>
		/// string formatter which contains all defined values
		/// </summary>
		private SimpleFormatter _formatter = null;
		
		/// <summary>
		/// Contains all predefined fuzzers.
		/// They can be referenced by using fuzzer/predefined and specifying its id
		/// </summary>
		private Dictionary<string, IFuzzLocation> _predefinedFuzzers = new Dictionary<string, IFuzzLocation>();
		
		/// <summary>
		/// Contains all fuzzlocations that get executed to trigger the fuzz location position.
		/// They get executed in order they are contained in the description file
		/// </summary>
		private List<IFuzzLocation> _preConditions = new List<IFuzzLocation>();
			
		public XmlFuzzFactory (string path)
		{
			FileInfo configFile = new FileInfo(path);
			
			if (!configFile.Exists)
				throw new FileNotFoundException (string.Format ("The specified xml description file '{0}' does not exist", path));
		
			
			_configDir = configFile.DirectoryName;
			
			_doc = new XmlDocument ();
			_doc.Load (configFile.FullName);	
		}
		
		/// <summary>
		/// Initializes the fuzzing environment
		/// </summary>
		public void Init ()
		{
			_formatter = new SimpleFormatter ();
			_formatter.IgnoreUnknownMacros = true;
			XmlFactoryHelpers.ParseValueIncludes (_doc.DocumentElement, _configDir, _formatter, _values);
			InitRemote ();
			InitTargetConnection ();
			InitPreDefinedFuzzers ();
			InitPreCondition ();
			InitFuzzDescription ();
			InitLoggers ();
		}
		
		public FuzzController[] CreateFuzzController ()
		{
			List<FuzzController> fuzzControllers = new List<FuzzController> ();
			foreach (FuzzDescriptionInfo info in _fuzzDescriptions)
			{

				IBreakpoint snapshot = _connector.SetSoftwareBreakpoint (info.RegionStart.ResolveAddress ().Value, 0, "snapshot");
				IBreakpoint restore = _connector.SetSoftwareBreakpoint (info.RegionEnd.ResolveAddress ().Value, 0, "restore");

				FuzzDescription fuzzDescription = new FuzzDescription (snapshot, restore);
				fuzzDescription.FuzzLocation.AddRange (info.FuzzLocations);

				fuzzControllers.Add (new FuzzController (_connector, _logDestination,
					new LoggerCollection (_loggers.ToArray ()), 
						fuzzDescription, _preConditions.ToArray()));
			
			}
			
			return fuzzControllers.ToArray ();
			
		}

		
		
		/// <summary>
		/// Initializes the remote control and extracts the commands to execute 
		/// from the configuration file
		/// </summary>
		private void InitRemote ()
		{
			
			
			
			if (_remoteControlProtocol != null)
			{
				_remoteControlProtocol.Dispose ();
				_remoteControlProtocol = null;
			}
			
			XmlElement remoteControlNode = (XmlElement)_doc.DocumentElement.SelectSingleNode ("RemoteControl");

			//remote control is not mandatory, but strongly recommended.
			//Without remote control there is no way to capture remote memory allocations
			//and therefore a lot information to analyze gets lost
			if (remoteControlNode != null)
			{
				_remoteControlProtocol = new RemoteControlProtocol ();
				_remoteControlProtocol.SetConnection (RemoteControlConnectionBuilder.Connect (
					  _formatter.Format (XmlHelper.ReadString (remoteControlNode, "Host")),
					  int.Parse (_formatter.Format (XmlHelper.ReadString (remoteControlNode, "Port")))));
				
				_remoteControlProtocol.ExecStatus += Handle_remoteControlProtocolExecStatus;
			
				
				foreach (XmlElement execNode in remoteControlNode.SelectNodes ("Exec"))
				{
					ExecutionTriggerEnum execTrigger = 
						(ExecutionTriggerEnum)Enum.Parse (typeof(ExecutionTriggerEnum), execNode.GetAttribute ("trigger"), true);
					
					
					string cmd = XmlFactoryHelpers.GenerateFilename (_configDir, XmlHelper.ReadString (execNode, "Cmd"), _formatter);
					
					if(cmd == null)
						throw new ArgumentException("Exec node without cmd");
					
					List<string> arguments = new List<string>(XmlHelper.ReadStringArray(execNode, "Arg"));
					List<string> environment = new List<string>(XmlHelper.ReadStringArray(execNode, "Env"));
					
					for(int i = 0; i<arguments.Count; i++)
						arguments[i] = _formatter.Format(arguments[i]);
					
					for(int i = 0; i<environment.Count; i++)
						environment[i] = _formatter.Format(environment[i]);
					
					if(_triggeredExecutions.ContainsKey(execTrigger) == false)
						_triggeredExecutions[execTrigger] = new List<RemoteExecCommand>();
					
					_triggeredExecutions[execTrigger].Add(
						new RemoteExecCommand(cmd, cmd, arguments, environment));
				}
				
				RemoteExec(ExecutionTriggerEnum.Immediate);
			}			
		}
		
		/// <summary>
		/// Execute all programs that are registered for the specified trigger
		/// </summary>
		/// <param name="toExec"></param>
		private void RemoteExec (ExecutionTriggerEnum toExec)
		{
			if (_triggeredExecutions.ContainsKey (toExec))
			{
				foreach (RemoteExecCommand cmdExec in _triggeredExecutions[toExec])
				{
					_currentRemoteExecInfo = 
						new RemoteExecutionInfo (cmdExec);
		
					_currentRemoteExecInfo.SendCommand (_remoteControlProtocol);
					
					if(!_currentRemoteExecInfo.SyncEvent.WaitOne(5000))
						throw new ArgumentException(
						   string.Format("Could not execute command '{0}', check the connection and the " +
						                 "remote terminal for errors", cmdExec.Path));
					
					if(_currentRemoteExecInfo.ExecStatus != RemoteExecutionInfo.ExecutionStatus.Success)
						throw new ArgumentException(string.Format(
						   "Remote program reported an errorcode #{0}", _currentRemoteExecInfo.ErrorCode));
				}
			}
		}
		
		/// <summary>
		/// Initializes the connection to the target
		/// </summary>
		private void InitTargetConnection()
		{
			XmlElement connectorRoot = (XmlElement)_doc.DocumentElement.SelectSingleNode("TargetConnection");
			
			if(connectorRoot == null)
				throw new ArgumentException("Could not find 'TargetConnection' node");
			
			string connectorIdentifier = _formatter.Format(XmlHelper.ReadString(connectorRoot, "Connector"));
			ITargetConnector connector = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<ITargetConnector>(connectorIdentifier);
			
			if(connector == null)
				throw new ArgumentException(string.Format("Could not find connector with identifier '{0}'", connectorIdentifier));

			IDictionary<string, string > configuration = new Dictionary<string, string>();
			
			foreach(XmlElement configNode in connectorRoot.SelectNodes("Config"))
				configuration.Add(configNode.GetAttribute("key"), _formatter.Format(configNode.InnerXml));
			
			connector.Setup(configuration);
			connector.Connect();
			_connector = connector;
		}
		
		/// <summary>
		/// Initializes all predefined fuzzers
		/// </summary>
		private void InitPreDefinedFuzzers ()
		{
			foreach (XmlElement element in _doc.DocumentElement.SelectNodes ("DefineFuzzer"))
			{
				string id = XmlHelper.ReadString (element, "Id");
				_predefinedFuzzers.Add (id, ReadFuzzLocation (element, false));
			}
		}
		
		/// <summary>
		/// Reads and initializes the pre-fuzz-conditions.
		/// they get executed once the configuration file is read and the connection has been established
		/// </summary>
		private void InitPreCondition ()
		{
			List<IFuzzLocation> fuzzLocations = new List<IFuzzLocation> ();
			foreach (XmlElement preTriggerElement in _doc.DocumentElement.SelectNodes ("PreCondition"))
			{
				IFuzzLocation fuzzLocation = ReadFuzzLocation (preTriggerElement, true);
				fuzzLocations.Add (fuzzLocation);
			}
			
			_preConditions = fuzzLocations;
		}
		
		/// <summary>
		/// Reads all fuzzDescriptions
		/// TODO: currently only the first fuzz description is read
		/// </summary>
		private void InitFuzzDescription ()
		{
			XmlElement fuzzDescriptionRoot = (XmlElement)_doc.DocumentElement.SelectSingleNode ("FuzzDescription");
			
			if (fuzzDescriptionRoot == null)
				throw new ArgumentException ("Could not find 'fuzzDescriptionRoot' node");
			
			_fuzzDescriptions.Add (ReadFuzzDescription (fuzzDescriptionRoot));
		}
		
		/// <summary>
		/// Initializes all defined loggers
		/// </summary>
		private void InitLoggers ()
		{
			XmlElement destinationNode = (XmlElement)_doc.DocumentElement.SelectSingleNode("Logger/Destination");
			if(destinationNode == null)
				throw new FuzzParseException("Could not find logger destination node");
			
			_logDestination = _formatter.Format(destinationNode.InnerText);
			
			foreach(XmlElement useLoggerNode in _doc.DocumentElement.SelectNodes("Logger/UseLogger"))
			{
				switch(useLoggerNode.GetAttribute("name"))
				{
				case "datagenlogger":
				{
					DataGeneratorLogger datagenLogger = new DataGeneratorLogger(_logDestination);
					foreach(var fuzzDescInfo in _fuzzDescriptions)
					{
						foreach(var fuzzLocationInfo in fuzzDescInfo.FuzzLocations)
							fuzzLocationInfo.SetLogger(LoggerDestinationEnum.DataGenLogger, datagenLogger);
					}
					_loggers.Add(datagenLogger);
					break;
				}
				case "connectorlogger":
				{
					_loggers.Add(_connector.CreateLogger(_logDestination));
					break;
				}
				case "stackframelogger":
				{
					_loggers.Add(new StackFrameLogger(_connector, _logDestination));
					break;
				}
				case "remotepipelogger":
				{
					if(_remoteControlProtocol != null)
					{
						string[] pipeNames = XmlHelper.ReadStringArray(useLoggerNode, "PipeName");
						_loggers.Add(new RemotePipeLogger(_remoteControlProtocol, _logDestination, pipeNames));
					}
					break;
				}
					
				}
			}
		}
		
		/// <summary>
		/// Reads a single FuzzDescription Tag and generates 
		/// a FuzzDescriptionInfo object
		/// </summary>
		/// <param name="rootNode">
		/// A <see cref="XmlElement"/>
		/// </param>
		private FuzzDescriptionInfo ReadFuzzDescription (XmlElement rootNode)
		{
			FuzzDescriptionInfo fuzzDescription = new FuzzDescriptionInfo (_connector);

			fuzzDescription.SetFuzzRegionStart (XmlHelper.ReadString (rootNode, "RegionStart"));
			fuzzDescription.SetFuzzRegionEnd (XmlHelper.ReadString (rootNode, "RegionEnd"));
		
			foreach (XmlElement fuzzLocationNode in rootNode.SelectNodes ("FuzzLocation"))
				fuzzDescription.AddFuzzLocation (ReadFuzzLocation (fuzzLocationNode, true));
			
			return fuzzDescription;
		}
		
		private IFuzzLocation ReadFuzzLocation (XmlElement fuzzLocationNode, bool readChangeableContent)
		{
			string fuzzerType = XmlHelper.ReadString (fuzzLocationNode, "FuzzerType");
			if (fuzzerType == null)
				throw new FuzzParseException ("Could not find 'FuzzerType'-Node");
			
			IFuzzLocation fuzzLocation = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<IFuzzLocation> (fuzzerType);
			
			if (fuzzLocation == null)
				throw new FuzzParseException (string.Format ("Could not find fuzz location implementation with identifier '{0}'", fuzzerType));
			
			fuzzLocation.Init (fuzzLocationNode, _connector, _predefinedFuzzers);
			
			if(readChangeableContent)
				fuzzLocation.InitChanges (fuzzLocationNode);
			return fuzzLocation;
		}
		
		
		/// <summary>
		/// Called after an exec call has been sent to the remote target
		/// </summary>
		/// <param name="name"></param>
		/// <param name="pid"></param>
		/// <param name="status"></param>
		private void Handle_remoteControlProtocolExecStatus (string name, int pid, int status)
		{
			if(_currentRemoteExecInfo != null && _currentRemoteExecInfo.Cmd.Name == name)
			{
				_currentRemoteExecInfo.ExecStatus = (status == 0 ? RemoteExecutionInfo.ExecutionStatus.Success:
					RemoteExecutionInfo.ExecutionStatus.Error);
				_currentRemoteExecInfo.ErrorCode = status;
				_currentRemoteExecInfo.PID = pid;
				_currentRemoteExecInfo.SyncEvent.Set();
			}
		}
	}
}

