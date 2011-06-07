using System;

using Iaik.Utils.CommonFactories;
using Fuzzer.TargetConnectors;
using System.Collections.Generic;
using System.Diagnostics;
using Iaik.Utils.IO;
using System.Globalization;
using System.Reflection;
using Iaik.Utils;

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
			config.Add("file", "/home/andi/Documents/Uni/master-thesis/src/test_sources/gdb_reverse_debugging_test/gdb_reverse_debugging_test");
			
			using(ITargetConnector connector = 
				GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<ITargetConnector>("general/gdb"))
			{
				ISymbolTable symbolTable = (ISymbolTable)connector;
				
				connector.Setup(config);
				connector.Connect();
				
				ISymbolTableMethod bar = symbolTable.FindMethod("bar");
				IBreakpoint breakBar = connector.SetSoftwareBreakpoint(bar, 0, "break_bar");
				IDebuggerStop stop = null;
				
				while(stop == null || stop.Breakpoint != breakBar)
				{
					stop = connector.DebugContinue();
					
					if(stop.StopReason == StopReasonEnum.Exit || stop.StopReason == StopReasonEnum.Terminated)
						break;
				}

				byte[] buffer = new byte[1024*1024];
				foreach(ISymbolTableVariable variable in bar.Parameters)
				{
					UInt64? address = variable.Address;
					if(address != null)
					{
						UInt64 varReadSize = connector.ReadMemory(buffer, address.Value, 4);
						byte[] b = new byte[varReadSize];
						Array.Copy(buffer, b, (long)varReadSize);
						Console.WriteLine("{0}[at 0x{1:X}]=[{2}]", variable.Name, address, ByteHelper.ByteArrayToHexString(b));
						int val = BitConverter.ToInt32(buffer, 0);
					}
				}
				
				
				
				buffer = new byte[1024*1024];
				UInt64 readSize = connector.ReadMemory(buffer, symbolTable.FindMethod("main").AddressSpecifier.ResolveAddress().Value, 10000);
				ISnapshot snapshot = connector.CreateSnapshot();
				stop = connector.DebugContinue();
				snapshot.Restore();
				breakBar.Delete();
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

