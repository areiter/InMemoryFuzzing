// CalculatedSymbolTableVariable.cs
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
using Ciloci.Flee;
using Iaik.Utils;
using System.Globalization;
using System.Collections.Generic;
namespace Fuzzer.TargetConnectors
{
	
	/// <summary>
	/// Accepts any valid math expression e.g. 1234+6789, 
	/// ${0xDEADBEEF}+1234 to specify hex values, or
	/// ${reg:rbp}+24 (or any other valid register on the target machine) to reference to registers
	/// </summary>
	public abstract class CalculatedSymbolTableVariable : ISymbolTableVariable
	{
		protected ITargetConnector _connector;
		protected string _expression;
		protected int _size;
		
		public CalculatedSymbolTableVariable (ITargetConnector connector, string expression, int size)
		{
			_connector = connector;
			_expression = expression;
		}
	

		#region ISymbolTableVariable implementation
		public abstract ISymbolTableVariable Dereference ();

		public abstract ISymbolTableVariable Dereference (int index);

		public string Name 
		{
			get { return _expression;}
		}

		public int Size 
		{
			get { return 0;}
		}

		public Nullable<ulong> Address 
		{
			get 
			{
				ExpressionContext context = new ExpressionContext ();
				IGenericExpression<UInt64> genExpression = context.CompileGeneric<UInt64> (StringHelper.ReplaceVariables (_expression, VariableDetectedCB));
				return genExpression.Evaluate ();
			}
		}
						
		public string VariableDetectedCB (string variable)
		{
			if (variable.StartsWith ("0x", StringComparison.InvariantCultureIgnoreCase))
				return UInt64.Parse (variable.Substring (2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo).ToString ();
			else
			{
				KeyValuePair<string, string>? keyVPair = StringHelper.SplitToKeyValue (variable, "|");
				if (keyVPair == null)
					throw new ArgumentException (string.Format ("Check variable syntax, '{0}' is invalid", variable));
				
				switch (keyVPair.Value.Key)
				{
				case "reg":
					UInt64? regVal = _connector.GetRegisterValue (keyVPair.Value.Value);
					if (regVal == null)
						throw new ArgumentException (string.Format ("Register '{0}' is not defined", keyVPair.Value.Value));
					return regVal.Value.ToString ();
					
				default:
					throw new NotSupportedException (string.Format ("Check variable syntax, '{0}' is invalid or unknown", variable));
				}
			}
		}
		#endregion

		#region ISymbol implementation
		public string Symbol 
		{
			get { return Name; }
		}
		#endregion
}
}

