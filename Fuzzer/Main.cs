using System;

using Iaik.Utils.CommonFactories;
using Fuzzer.TargetConnectors;
using System.Collections.Generic;
using System.Diagnostics;
using Iaik.Utils.IO;
using System.Globalization;
using System.Reflection;

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
			config.Add("target", "extended-remote :1234");
			config.Add("file", "testing");
			
			using(ISymbolTable symbolTable = 
				GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<ISymbolTable>("symbol_table/gdb"))
			{
				symbolTable.Setup(config);
			}
			
			return;
			
			using(ITargetConnector connector = 
				GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<ITargetConnector>("general/gdb"))
			{
			
				connector.Setup(config);
				connector.Connect();

				IBreakpoint breakMain = connector.SetSoftwareBreakpoint(0x400553, 8, "break_main");
				IBreakpoint breakfoo = connector.SetSoftwareBreakpoint(0x400539, 8, "break_foo");
				IDebuggerStop stop = connector.DebugContinue();
				ISnapshot snapshot = connector.CreateSnapshot();
				stop = connector.DebugContinue();
				snapshot.Restore();
				breakMain.Delete();
				Console.ReadLine();
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

