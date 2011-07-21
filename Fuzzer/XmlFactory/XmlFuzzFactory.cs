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
			XmlFactoryHelpers.ParseValueIncludes(_doc.DocumentElement, _configDir, _formatter, _values);
			InitRemote ();
			InitTargetConnection ();
			InitFuzzDescription ();
			InitLoggers ();
		}
		
		public FuzzController[] CreateFuzzController ()
		{
			List<FuzzController> fuzzControllers = new List<FuzzController> ();
			foreach (FuzzDescriptionInfo info in _fuzzDescriptions)
			{
				List<IFuzzDescription> fuzzDescriptions = new List<IFuzzDescription> ();
				foreach (FuzzLocationInfo desc in info.FuzzLocations)
					fuzzDescriptions.Add (desc.FuzzDescription);
			
				IBreakpoint snapshot = _connector.SetSoftwareBreakpoint (info.RegionStart.ResolveAddress ().Value, 0, "snapshot");
				IBreakpoint restore = _connector.SetSoftwareBreakpoint (info.RegionEnd.ResolveAddress ().Value, 0, "restore");
				
				fuzzControllers.Add (new FuzzController (_connector, snapshot, restore, _logDestination,
					new LoggerCollection (_loggers.ToArray ()), fuzzDescriptions.ToArray ()));
			
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
							fuzzLocationInfo.DataGenerator.SetLogger(datagenLogger);
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
				fuzzDescription.AddFuzzLocation (ReadFuzzLocation (fuzzLocationNode));
			
			return fuzzDescription;
		}
		
		/// <summary>
		/// Reads a single FuzzLocation with its datagenerator and data type
		/// </summary>
		/// <param name="rootNode"></param>
		/// <returns></returns>
		private FuzzLocationInfo ReadFuzzLocation (XmlElement rootNode)
		{
			FuzzLocationInfo fuzzLocationInfo = new FuzzLocationInfo (_connector);
			fuzzLocationInfo.SetDataRegion (XmlHelper.ReadString (rootNode, "DataRegion"));
			
			
			string stopCondition = XmlHelper.ReadString (rootNode, "StopCondition");
			if (stopCondition == null || stopCondition == string.Empty || stopCondition.Equals ("none"))
				fuzzLocationInfo.FuzzStopCondition = null;
			else
			{
				string[] stopConditionParts = stopCondition.Split (new char[] { '|' }, 2);
				
				if (stopConditionParts[0].Equals ("count", StringComparison.InvariantCultureIgnoreCase) && stopCondition.Length == 2)
					fuzzLocationInfo.FuzzStopCondition = new CountFuzzStopCondition (int.Parse (stopConditionParts[1]));
				else
					throw new NotImplementedException (string.Format ("Invalid stop condition identifier '{0}'", stopCondition));
			}
			
			
			IDataGenerator dataGen = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<IDataGenerator> (
				XmlHelper.ReadString (rootNode, "DataGenerator"));
			IDictionary<string, string> arguments = new Dictionary<string, string> ();
			foreach (XmlElement datagenArgNode in rootNode.SelectNodes ("DataGenArg"))
				arguments.Add (datagenArgNode.GetAttribute ("name"), datagenArgNode.InnerText);
			dataGen.Setup (arguments);
			
			fuzzLocationInfo.DataGenerator = dataGen;
			
			IFuzzDescription fuzzDescription = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<IFuzzDescription> (
				XmlHelper.ReadString (rootNode, "DataType"));
			fuzzDescription.SetDataGenerator (dataGen);
			fuzzDescription.SetFuzzTarget (fuzzLocationInfo.DataRegion);
			fuzzLocationInfo.FuzzDescription = fuzzDescription;
			
			return fuzzLocationInfo;
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

