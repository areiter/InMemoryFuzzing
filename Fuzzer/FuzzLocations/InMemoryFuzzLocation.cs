// InMemoryFuzzLocation.cs
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
using Iaik.Utils.CommonAttributes;
using System.Xml;
using Fuzzer.TargetConnectors;
using Fuzzer.DataGenerators;
using Iaik.Utils;
using System.Collections.Generic;
using Fuzzer.XmlFactory;
using Iaik.Utils.CommonFactories;
using Fuzzer.FuzzDescriptions;
using System.Globalization;
namespace Fuzzer.FuzzLocations
{
	[ClassIdentifier("fuzzer/in_memory")]
	public class InMemoryFuzzLocation : BaseFuzzLocation
	{
		private ISymbolTableVariable _fuzzTarget = null;
		
		public ISymbolTableVariable FuzzTarget
		{
			get { return _fuzzTarget; }
		}
		
		public IDataGenerator DataGenerator
		{
			get { return _dataGenerator;}
		}
		
		private IFuzzTech _tech = null;
		
		
		
		#region implemented abstract members of Fuzzer.FuzzLocations.BaseFuzzLocation
		protected override bool SupportsStopCondition 
		{
			get { return true; }
		}
		
		
		protected override bool SupportsDataGen 
		{
			get { return true; }
		}
		
		protected override bool SupportsTrigger 
		{
			get { return true;	}
		}
		
		#endregion
		
		public override void Init (XmlElement fuzzLocationRoot, ITargetConnector connector)
		{
			base.Init (fuzzLocationRoot, connector);
		
			SetDataRegion (XmlHelper.ReadString (fuzzLocationRoot, "DataRegion"), connector);
			SetDataType (XmlHelper.ReadString (fuzzLocationRoot, "DataType"));
		}
		
		private void SetDataRegion (string regionSpecifier, ITargetConnector connector)
		{
			KeyValuePair<string, string>? regionSpecifierPair = StringHelper.SplitToKeyValue (regionSpecifier, "|");
			if (regionSpecifierPair == null)
				throw new FuzzParseException ("DataRegion contains invalid formatted specifier '{0}'", regionSpecifier);
			
			
			
			switch (regionSpecifierPair.Value.Key) 
			{
			case "variable":
				_fuzzTarget = connector.SymbolTable.CreateVariable (regionSpecifierPair.Value.Value, 8);
				break;
			case "address":
				UInt64 address;
				if (regionSpecifierPair.Value.Value.StartsWith ("0x", StringComparison.InvariantCultureIgnoreCase) &&
					UInt64.TryParse (regionSpecifierPair.Value.Value.Substring (2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address))
					_fuzzTarget = connector.SymbolTable.CreateVariable (new StaticAddress (address), 8);
				else
					throw new FuzzParseException ("Cannot parse address in format '{0}' use '0x12345678'", regionSpecifierPair.Value.Value);
				break;
			case "calc":
				_fuzzTarget = connector.SymbolTable.CreateCalculatedVariable (regionSpecifierPair.Value.Value, 8);
				break;
				
			default:
				throw new FuzzParseException ("Invalid data region specifier '{0}'", regionSpecifierPair.Value.Key);
			}
		}
		
		private void SetDataType (string dataTypeSpecifier)
		{
			_tech = GenericClassIdentifierFactory.CreateFromClassIdentifierOrType<IFuzzTech> (dataTypeSpecifier, this);
			
			if (_tech == null)
				throw new ArgumentException (string.Format ("DataType '{0}' not found", dataTypeSpecifier));
			
			_tech.Init ();
		}
		
			
		
		public override void Run (FuzzController ctrl)
		{
			base.Run (ctrl);			
			_tech.Run (ctrl);
		}

	}
}

