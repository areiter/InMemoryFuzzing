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

namespace Fuzzer
{
	class MainClass
	{
		
		public static void Main (string[] args)
		{
			SetupLogging();
			TestApamaLinux();
		}
		
		private static void TestApamaLinux()
		{	
			IDictionary<string, string> config = new Dictionary<string, string>();
			config.Add("gdb_exec", "/opt/gdb-7.2/bin/gdb");
			config.Add("gdb_log", "stream:stderr");
			
			//config.Add("target", "extended-remote :1234");
			
			config.Add("target", "run_local");
			
			//config.Add("target", "attach_local");
			//config.Add("target-options", "14577");
			
			//config.Add("file", "/home/andi/Documents/Uni/master-thesis/src/test_sources/gdb_reverse_debugging_test/gdb_reverse_debugging_test");
			config.Add("file", "/home/andi/hacklet/prog0-x64");
			
			using(ITargetConnector connector = 
				GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<ITargetConnector>("general/gdb"))
			{
				ISymbolTable symbolTable = (ISymbolTable)connector;
				
				connector.Setup(config);
				connector.Connect();
				
				ISymbolTableMethod main = symbolTable.FindMethod("main");
				IBreakpoint snapshotBreakpoint = connector.SetSoftwareBreakpoint(main, 0, "break_snapshot");
				IBreakpoint restoreBreakpoint = connector.SetSoftwareBreakpoint (0x400797, 0, "break_restore");
				
//				IFuzzDescription barVar1_Description = new SingleValueFuzzDescription(bar.Parameters[0], 
//					new RandomByteGenerator( 4, 4, RandomByteGenerator.ByteType.All));		
//				IFuzzDescription barVar1_readableChar = new PointerValueFuzzDescription(bar.Parameters[0],
//					new RandomByteGenerator(5, 1000, RandomByteGenerator.ByteType.PrintableASCIINullTerminated));
				
				connector.DebugContinue ();
				
			 	ISymbolTableVariable argv = main.Parameters[1];
				ISymbolTableVariable dereferencedArgv = argv.Dereference();
				
				
//				FuzzController fuzzController = new FuzzController(
//					connector,
//					snapshotBreakpoint,
//					restoreBreakpoint,
//					barVar1_readableChar);
					
//				fuzzController.Fuzz();
			}
			

			
			Console.ReadLine();
			
		}
		
		
		/// <summary>
		/// Initializes the logger
		/// </summary>
		private static void SetupLogging()
		{
			log4net.Appender.ConsoleAppender appender = new log4net.Appender.ConsoleAppender();	
			appender.Name = "ConsoleAppender";
			appender.Layout = new log4net.Layout.PatternLayout("[%date{dd.MM.yyyy HH:mm:ss,fff}]-%-5level-%t-[%c]: %message%newline");
			log4net.Config.BasicConfigurator.Configure(appender);
		
			
			//_logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		}
	}
}

