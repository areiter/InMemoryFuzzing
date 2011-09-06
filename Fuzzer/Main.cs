using System;

using Iaik.Utils.CommonFactories;
using Fuzzer.TargetConnectors;
using System.Collections.Generic;
using System.Diagnostics;
using Iaik.Utils.IO;
using System.Globalization;
using System.Reflection;
using Iaik.Utils;
using Fuzzer.FuzzDescriptions;
using Fuzzer.DataGenerators;
using Iaik.Utils.libbfd;
using System.Runtime.InteropServices;
using System.IO;
using Fuzzer.TargetConnectors.GDB.CoreDump;
using System.Net.Sockets;
using Fuzzer.RemoteControl;
using System.Threading;
using System.Text;
using Fuzzer.TargetConnectors.GDB;
using Fuzzer.DataLoggers;
using Fuzzer.XmlFactory;
using Fuzzer.Analyzers;
using Fuzzer.TargetConnectors.RegisterTypes;

namespace Fuzzer
{
	class MainClass
	{
		public enum ProgramMode
		{
			Analyze,
			Fuzz
		}
		
		private static ProgramMode? _programMode = null;
		private static FuzzController[] _fuzzControllers = null;
		private static AnalyzeController _analyzeController = null;
		
		public static string ConfigPath = "";
		public static void Main (string[] args)
		{
			SetupLogging ();
		
			
			if (args == null || args.Length == 0)
				OutputHelp (null);
			else
			{
				CommandLineHandler cmdLineHandler = new CommandLineHandler ();
				cmdLineHandler.RegisterCallback ("fuzz", SetFuzzingMode );
				cmdLineHandler.RegisterCallback ("analyze", SetAnalyzationMode );
				cmdLineHandler.RegisterCallback ("xmlinput", ParseXmlInput);
				cmdLineHandler.RegisterCallback ("help", OutputHelp);
				cmdLineHandler.Parse (args);
			}
			
			if(_programMode == null)
			{
				OutputHelp(null);
				return;
			}
				
			if (_programMode.Value == MainClass.ProgramMode.Fuzz && 
			    _fuzzControllers != null)
			{
				foreach (FuzzController fc in _fuzzControllers)
					fc.Fuzz ();
			}
			else if(_programMode.Value == MainClass.ProgramMode.Analyze &&
			        _analyzeController != null)
				_analyzeController.Analyze();
			else
			{
				Console.WriteLine("Could not find a configuration \n\n");
				OutputHelp(null);
			}
			
		}
	
		private static void SetFuzzingMode (CommandLineHandler.CommandOption cmdOption)
		{
			_programMode = ProgramMode.Fuzz;
		}
		
		private static void SetAnalyzationMode (CommandLineHandler.CommandOption cmdOption)
		{
			_programMode = ProgramMode.Analyze;
		}
		
		private static void ParseXmlInput (CommandLineHandler.CommandOption cmdOption)
		{
			if (cmdOption.Arguments.Length == 0)
				OutputHelp (null);
			else
			{
				if(_programMode == null)
				{
					Console.WriteLine("Missing program mode (fuzz|analyze). Program mode always needs to be the first argument\n\n");
					OutputHelp(null);
				}
				else if(_programMode.Value == MainClass.ProgramMode.Fuzz)
				{
					Console.WriteLine ("Using XmlFuzzFactory, parsing '{0}'", cmdOption.Arguments[0]);
					XmlFuzzFactory factory = new XmlFuzzFactory (cmdOption.Arguments[0]);
					factory.Init ();
					_fuzzControllers = factory.CreateFuzzController ();
				}
				else if(_programMode.Value == MainClass.ProgramMode.Analyze)
				{
					Console.WriteLine("Using XmlAnalyzeFactory, parsing '{0}'", cmdOption.Arguments[0]);
					XmlAnalyzeFactory factory = new XmlAnalyzeFactory(cmdOption.Arguments[0]);
					factory.Init();
					_analyzeController = factory.CreateAnalyzeController();
				}
				else
					throw new NotImplementedException(string.Format("The specified program mode '{0}' it not implemented yet", _programMode.Value));
			}
		}

		private static void OutputHelp (CommandLineHandler.CommandOption cmdOption)
		{
			Console.WriteLine ("Use {--fuzz|--analyze} --xmlinput=[path to input file]");
			Console.WriteLine ();
			
			Environment.Exit (0);
		}
		
//		private static void TestApamaLinux()
//		{	
//			IDictionary<string, string> config = new Dictionary<string, string>();
//			config.Add("gdb_exec", "/opt/gdb-7.2/bin/gdb");
//			config.Add("gdb_log", "stream:stderr");
//			
//			//config.Add("target", "extended-remote :1234");
//			
//			config.Add("target", "run_local");
//			
//			//config.Add("target", "attach_local");
//			//config.Add("target-options", "14577");
//			
//			//config.Add("file", "/home/andi/Documents/Uni/master-thesis/src/test_sources/gdb_reverse_debugging_test/gdb_reverse_debugging_test");
//			config.Add("file", "/home/andi/hacklet/prog0-x64");
//			
//			using(ITargetConnector connector = 
//				GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<ITargetConnector>("general/gdb"))
//			{
//				ISymbolTable symbolTable = (ISymbolTable)connector;
//				
//				connector.Setup(config);
//				connector.Connect();
//				
//				ISymbolTableMethod main = symbolTable.FindMethod("main");
//				IBreakpoint snapshotBreakpoint = connector.SetSoftwareBreakpoint(main, 0, "break_snapshot");
//				IBreakpoint restoreBreakpoint = connector.SetSoftwareBreakpoint (0x4007b5, 0, "break_restore");
//				
////				IFuzzDescription barVar1_Description = new SingleValueFuzzDescription(bar.Parameters[0], 
////					new RandomByteGenerator( 4, 4, RandomByteGenerator.ByteType.All));		
////				IFuzzDescription barVar1_readableChar = new PointerValueFuzzDescription(bar.Parameters[0],
////					new RandomByteGenerator(5, 1000, RandomByteGenerator.ByteType.PrintableASCIINullTerminated));
//				
//				connector.DebugContinue ();
////				Registers r = ((Fuzzer.TargetConnectors.GDB.GDBConnector)connector).GetRegisters();
////				using(FileStream fSink = new FileStream("/home/andi/x86-64.registers", FileMode.CreateNew, FileAccess.Write))
////				{
////					StreamHelper.WriteTypedStreamSerializable(r, fSink);
////				}
//			 	ISymbolTableVariable argv = main.Parameters[1];
//				ISymbolTableVariable dereferencedArgv = argv.Dereference();
//				
//				DataGeneratorLogger datagenLogger = new DataGeneratorLogger("/home/andi/log");
////				IFuzzDescription fuzzArgv = new PointerValueFuzzDescription(
////					dereferencedArgv, new RandomByteGenerator(
////				                          100, 10000, RandomByteGenerator.ByteType.PrintableASCIINullTerminated));
////				IStackFrameInfo stackFrameInfo = connector.GetStackFrameInfo();
//				
////				
////				FuzzController fuzzController = new FuzzController(
////					connector,
////					snapshotBreakpoint,
////					restoreBreakpoint,
////					new LoggerCollection(
////						new GDBLogger((GDBConnector)connector, "/home/andi/log"),
////						new StackFrameLogger(connector, "/home/andi/log"),
////						datagenLogger
////					),
////					fuzzArgv);
////					
////				fuzzController.Fuzz();
//			}
//			
//
//			
//			Console.ReadLine();
//			
//		}
		
		
		/// <summary>
		/// Initializes the logger
		/// </summary>
		private static void SetupLogging()
		{
			log4net.Appender.FileAppender fileAppender = new log4net.Appender.FileAppender();
			fileAppender.Name = "FileAppender";
			//fileAppender.Writer = new StreamWriter("/home/andi/fuzzer.log");
			fileAppender.ImmediateFlush = true;
			fileAppender.Layout = new log4net.Layout.PatternLayout("[%date{dd.MM.yyyy HH:mm:ss,fff}]-%-5level-[%c]: %message%newline");
			fileAppender.File = "/home/andi/fuzzer.log";
			fileAppender.ActivateOptions();
			
			log4net.Appender.ConsoleAppender appender = new log4net.Appender.ConsoleAppender();	
			appender.Name = "ConsoleAppender";
			appender.Layout = new log4net.Layout.PatternLayout("[%date{dd.MM.yyyy HH:mm:ss,fff}]-%-5level-[%c]: %message%newline");
			
		
			log4net.Appender.ForwardingAppender forwarder = new log4net.Appender.ForwardingAppender();
			forwarder.AddAppender(fileAppender);
			forwarder.AddAppender(appender);

			log4net.Config.BasicConfigurator.Configure(forwarder);
			
			//_logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		}
	}
}

