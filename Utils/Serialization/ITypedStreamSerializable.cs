/* Copyright 2010 Andreas Reiter <andreas.reiter@student.tugraz.at>, 
 *                Georg Neubauer <georg.neubauer@student.tugraz.at>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


//
//
// Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;
using Iaik.Utils.CommonAttributes;
using System.Reflection;
using System.Collections.Generic;
using Iaik.Utils.CommonFactories;

namespace Iaik.Utils.Serialization
{


	/// <summary>
	/// Implemented by classes that can be created from stream, without knowing
	/// their type.
	/// All implementing classes need to attach the TypedStreamSerializableAttribute
	/// </summary>
	public interface ITypedStreamSerializable : IStreamSerializable
	{
	}
	
	
	/// <summary>
	/// Defines an unique identifier for the attached type
	/// </summary>
	public class TypedStreamSerializableAttribute : ClassIdentifierAttribute
	{
		
		public TypedStreamSerializableAttribute (string typeIdentifier)
			: base(typeIdentifier)
		{
		}			
	}
	
	public static class TypedStreamSerializableHelper
	{
		/// <summary>
		/// Contains types by name and by assembly
		/// </summary>
		private static Dictionary<Assembly, Dictionary<string, Type>> _cachedTypes = new Dictionary<Assembly, Dictionary<string, Type>>();
		
		/// <summary>
		/// Looks for the <see>TypedStreamSerializableAttribute</see> in the given object
		/// </summary>
		/// <param name="victim"></param>
		/// <returns></returns>
		public static TypedStreamSerializableAttribute FindAttribute (ITypedStreamSerializable victim)
		{
			object[] attributes = victim.GetType ().GetCustomAttributes (typeof(TypedStreamSerializableAttribute), false);
			
			if (attributes == null || attributes.Length == 0)
				throw new ArgumentException ("TypedStreamSerializableAttribute not defined", "victim");
			
			return (TypedStreamSerializableAttribute)attributes[0];
		}
		
		
		/// <summary>
		/// Finds the type with the specified identifier in the specified assembly. This method also provides
		/// a caching mechanism
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="asm"></param>
		/// <returns></returns>
		public static Type FindTypedStreamSerializableType (string identifier, Assembly asm)
		{
			lock(_cachedTypes)
			{
				if (_cachedTypes.ContainsKey (asm) == false)
					_cachedTypes.Add (asm, new Dictionary<string, Type> ());
				
				if (_cachedTypes[asm].ContainsKey (identifier) == false)
				{
					Type t = GenericClassIdentifierFactory.FindTypeForIdentifier<ITypedStreamSerializable> (identifier, asm);
					
					if (t == null)
						return null;
					
					_cachedTypes[asm].Add (identifier, t);
				}
				
				
				return _cachedTypes[asm][identifier];
			}
		}
		
		public static Type FindTypedStreamSerializableTypeWithException (string identifier, Assembly asm)
		{
			Type t = FindTypedStreamSerializableType (identifier, asm);
			
			if (t == null)
				throw new ArgumentException ("Could not find type with identifier " + identifier);
			
			return t;
		}

		
		/// <summary>
		/// Finds the specified type and trys to invoke the ctor with the specified arguments
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="asm"></param>
		/// <param name="argument"></param>
		/// <returns></returns>
		public static ITypedStreamSerializable CreateTypedStreamSerializable (string identifier, Assembly asm, params object[] arguments)
		{
			Type t = FindTypedStreamSerializableTypeWithException (identifier, asm);
			
			return CreateTypedStreamSerializable (t, arguments);
			
		}
		
		
		public static ITypedStreamSerializable CreateTypedStreamSerializable (Type t, params object[] arguments)
		{
			List<Type> types = new List<Type> ();
			foreach (object arg in arguments)
				types.Add (arg.GetType ());
			
			ConstructorInfo ctor = t.GetConstructor (types.ToArray ());
			
			if (ctor == null)
				throw new ArgumentException (string.Format ("'{0}' does not have matching ctor", t));
			
			
			return (ITypedStreamSerializable)ctor.Invoke (arguments);
		}
		
		
	}
	
	
}
