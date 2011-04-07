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
			
			connector.Setup(config);
		}
	}
}

