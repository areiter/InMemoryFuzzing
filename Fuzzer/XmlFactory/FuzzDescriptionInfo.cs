// FuzzDescriptionInfo.cs
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
using System.Collections.Generic;
using Iaik.Utils;
using System.Globalization;
namespace Fuzzer.XmlFactory
{
	public class FuzzDescriptionInfo
	{
		/// <summary>
		/// Connector associated with this FuzzDescriptionInfo
		/// </summary>
		private ITargetConnector _connector;
		
		/// <summary>
		/// Address to start the fuzzing process
		/// </summary>
		private IAddressSpecifier _regionStart;
		
		public IAddressSpecifier RegionStart
		{
			get { return _regionStart;}
		}
		
		
		/// <summary>
		/// Address to restore the program state as it was at regionstart
		/// </summary>
		private IAddressSpecifier _regionEnd;
		
		public IAddressSpecifier RegionEnd
		{
			get { return _regionEnd; }
		}
		
		/// <summary>
		/// Contains all fuzz locations for this single region
		/// </summary>
		private List<FuzzLocationInfo> _fuzzLocations = new List<FuzzLocationInfo>();
		
		public IEnumerable<FuzzLocationInfo> FuzzLocations
		{
			get { return _fuzzLocations; }
		}
			
		public FuzzDescriptionInfo (ITargetConnector connector)
		{
			_connector = connector;
		}
		
		/// <summary>
		/// Sets the start of the region to fuzz. See remarks for regionSpecifier syntax
		/// </summary>
		/// <remarks>
		/// Valid region specifiers are:
		///		method:<method_name> ... Resolves to the start (after the prolog) of the specified function
		///		methodret:<method_name> ... Resolves to the "end" of the specified method. A break is set to the instruction right after the method call
		///		address:0x12345678   ... Resolves to the specified address
		///		source:<filename>:<linenum> ... Resolves to the first instruction of the specified source code line.
		///                                     This specifier is only availabe if debugging symbols and source code
		///                                     is available, and the symbol table implementation supports it.
		/// 
		/// If the specifier has an invalid format a FuzzParseException is thrown
		/// </remarks>
		/// <param name="regionSpecifier"></param>
		public void SetFuzzRegionStart (string regionSpecifier)
		{
			_regionStart = ParseRegionAddress (regionSpecifier, false);
		}
		
		/// <summary>
		/// Sets the end of the region to fuzz
		/// </summary>
		/// <remarks>
		/// For details see SetFuzzRegionStart
		/// </remarks>
		/// <param name="regionSpecifier"></param>
		public void SetFuzzRegionEnd (string regionSpecifier)
		{
			_regionEnd = ParseRegionAddress (regionSpecifier, true);
		}
		
		public void AddFuzzLocation (FuzzLocationInfo fuzzLocation)
		{
			_fuzzLocations.Add (fuzzLocation);
		}
		
		private IAddressSpecifier ParseRegionAddress (string regionSpecifier, bool allowMethodRet)
		{
			KeyValuePair<string, string>? regionSpecifierPair = StringHelper.SplitToKeyValue (regionSpecifier, "|");
			if (regionSpecifierPair == null)
				throw new FuzzParseException ("RegionStart contains invalid formatted region specifier '{0}'", regionSpecifier);
			
			IAddressSpecifier breakAddress = null;
			
			switch (regionSpecifierPair.Value.Key) {
			case "method":
				AssertSymbolTable ();
				ISymbolTableMethod method = _connector.SymbolTable.FindMethod (regionSpecifierPair.Value.Value);
				if (method == null)
					throw new FuzzParseException ("Could not find method with name '{0}', have you realy attached debugging symbols?", regionSpecifierPair.Value.Value);
				
				breakAddress = method.BreakpointAddressSpecifier;
				break;
			
			case "methodret":
				if(!allowMethodRet)
					throw new FuzzParseException ("Specifier 'methodret' is not allowed");
				else
					throw new NotImplementedException();
			
			case "address":
				UInt64 address;
				if (regionSpecifierPair.Value.Value.StartsWith ("0x", StringComparison.InvariantCultureIgnoreCase) && UInt64.TryParse (regionSpecifierPair.Value.Value.Substring (2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address))
					breakAddress = new StaticAddress (address);
				else
					throw new FuzzParseException ("Cannot parse address in format '{0}' use '0x12345678'", regionSpecifierPair.Value.Value);
				break;
			
			case "source":
				AssertSymbolTable ();
				breakAddress = _connector.SymbolTable.SourceToAddress (regionSpecifierPair.Value.Value);
				if (breakAddress == null)
					throw new FuzzParseException ("Specified source '{0}' is invalid", regionSpecifierPair.Value.Value);
				break;
			
			default:
				throw new FuzzParseException ("Specifier '{0}' is invalid", regionSpecifierPair.Value.Key);
			}
			
			return breakAddress;
		}
		
		private void AssertSymbolTable()
		{
			if(_connector.SymbolTable == null)
				throw new ArgumentException("Connector does not have a symbol table");
		}
				                                  
				                                  
	}
	
	public class FuzzParseException:Exception
	{
		public FuzzParseException(string format, params object[] args)
			:base(string.Format(format, args))
		{
		}
	}
}

