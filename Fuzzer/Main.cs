using System;

using Iaik.Utils.CommonFactories;
using Fuzzer.TargetConnectors;
using System.Collections.Generic;

namespace Fuzzer
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			TestApamaLinux();
		}
		
		private static void TestApamaLinux()
		{
			IDictionary<string, string> config = new Dictionary<string, string>();
			config.Add("protocol", "gdb:127.0.0.1:1234");
			
			ITargetConnector connector = 
				GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<ITargetConnector>("linux/apama");
			
			try
			{
				connector.Setup(config);
				connector.Connect();
				Console.WriteLine("Connected={0}", connector.Connected);
				IBreakpoint breakMain = connector.SetSoftwareBreakpoint(0x4004f7, 8);
				//connector.RemoveSoftwareBreakpoint(0x4004f7, 8);
				breakMain.RemoveBreakpoint();
			}
			finally
			{
				connector.Close();
			}
		}
	}
}

