// FuzzLocationInfo.cs
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
using Fuzzer.TargetConnectors;
using Fuzzer.FuzzDescriptions;
using Fuzzer.DataGenerators;
using System.Collections.Generic;
using Iaik.Utils;
using System.Globalization;
namespace Fuzzer.XmlFactory
{
	/// <summary>
	/// Contains all data required by a single fuzz location
	/// </summary>
	public class FuzzLocationInfo
	{
		/// <summary>
		/// Associated connector
		/// </summary>
		private ITargetConnector _connector;
		
		/// <summary>
		/// Date region to fuzz
		/// </summary>
		private ISymbolTableVariable _dataRegion;
		
		public ISymbolTableVariable DataRegion
		{
			get { return _dataRegion; }
			set{ _dataRegion = value; }
		}
		
		/// <summary>
		/// Descripes the method how to fuzz the specified data region
		/// </summary>
		private IFuzzDescription _fuzzDescription;
		
		public IFuzzDescription FuzzDescription
		{
			get { return _fuzzDescription; }
			set { _fuzzDescription = value; }
		}
		
		/// <summary>
		/// DataGenerator to use for this data region
		/// </summary>
		private IDataGenerator _dataGen;
		
		public IDataGenerator DataGenerator
		{
			get { return _dataGen; }
			set { _dataGen = value; }
		}
		
		private IFuzzStopCondition _fuzzStopCondition = null;
		
		public IFuzzStopCondition FuzzStopCondition
		{
			get { return _fuzzStopCondition; }
			set { _fuzzStopCondition = value;}
		}
		
		public FuzzLocationInfo (ITargetConnector connector)
		{
			_connector = connector;
		}
		
		/// <summary>
		/// Specifies the data region to fuzz, syntax:
		///    	 <specifier>:<identifier>
		///    	 
		///    	 The size of the specified region is specified using the data generator settings
		///    	 
		///    	 specifier can be one of the following values:		    	 
		///    	 variable: identifier=>name of a variable valid in the context of the specified method. 
		///   	                       Can only be used if debugging symbols are available
		///    	 address: identifier=>0x<address> hex address of the memory region to fuzz
		///    	 calc: identifier=>any valid math expression
		///    	 
		///    	 a math expression can be 1234+6789, 
		///    	   {[0xDEADBEEF]}+1234 to specify hex values, or
		///    	   {[reg:rbp]}+24 (or any other valid register on the target machine) to reference to registers
		/// </summary>
		/// <param name="regionIdentifier">
		/// A <see cref="System.String"/>
		/// </param>
		public void SetDataRegion (string regionSpecifier)
		{
			KeyValuePair<string, string>? regionSpecifierPair = StringHelper.SplitToKeyValue (regionSpecifier, "|");
			if (regionSpecifierPair == null)
				throw new FuzzParseException ("DataRegion contains invalid formatted specifier '{0}'", regionSpecifier);
			
			
			
			switch (regionSpecifierPair.Value.Key) 
			{
			case "variable":
				_dataRegion = _connector.SymbolTable.CreateVariable (regionSpecifierPair.Value.Value, 8);
				break;
			case "address":
				UInt64 address;
				if (regionSpecifierPair.Value.Value.StartsWith ("0x", StringComparison.InvariantCultureIgnoreCase) &&
					UInt64.TryParse (regionSpecifierPair.Value.Value.Substring (2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address))
					_dataRegion = _connector.SymbolTable.CreateVariable (new StaticAddress (address), 8);
				else
					throw new FuzzParseException ("Cannot parse address in format '{0}' use '0x12345678'", regionSpecifierPair.Value.Value);
				break;
			case "calc":
				_dataRegion = _connector.SymbolTable.CreateCalculatedVariable (regionSpecifierPair.Value.Value, 8);
				break;
				
			default:
				throw new FuzzParseException ("Invalid data region specifier '{0}'", regionSpecifierPair.Value.Key);
			}
		}
}
}

