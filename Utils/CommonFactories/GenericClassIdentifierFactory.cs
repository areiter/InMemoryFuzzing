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
using System.Reflection;
using Iaik.Utils.CommonAttributes;
using System.Collections.Generic;

namespace Iaik.Utils.CommonFactories
{

	/// <summary>
	/// Creates ojects of classes where only the <see>ClassIdentifierAttribute</see> 
	/// name, the target/base type and the target assembly is known
	/// </summary>
	public static class GenericClassIdentifierFactory
	{

        /// <summary>
        /// Examines the specified identifier. If a class identifier with the same name is found, this class is used.
        /// If no classidentifier is found the identifier is interpreted as type identifier.
        /// </summary>
        /// <param name="identifier">Classidentifier or type identifier</param>
        /// <returns>Type of the classidentifier or type identifier</returns>
        public static Type FindTypeForIdentifier<T>(string identifier)
        {
            return FindTypeForIdentifier<T>(identifier, typeof(T).Assembly);
        }
        
        public static Type FindTypeForIdentifier<T>(string identifier, Assembly targetAsm)
        {
            foreach (Type t in targetAsm.GetTypes())
            {
                if (t.IsAbstract == false &&
                   t.IsClass == true &&
                   typeof(T).IsAssignableFrom(t))
                {

                    //We got a candidate that is at least convertible to the desired type,
                    //now check if there is an attached ClassIdentifierAttribute
                    object[] attributes = t.GetCustomAttributes(typeof(ClassIdentifierAttribute), false);

                    if (attributes != null)
                    {
                        foreach (ClassIdentifierAttribute classIdentifierAttribute in attributes)
                        {
                            if (classIdentifierAttribute.Identifier.Equals(identifier))
                                return t;
                        }
                    }
                }
            }

            Type myType = Type.GetType(identifier, false);
            if (myType == null || typeof(T).IsAssignableFrom(myType) == false)
                return null;

            return myType;
        }


		/// <summary>
		/// Examines the specified identifier. If a class identifier with the same name is found, this class is used.
		/// If no classidentifier is found the identifier is interpreted as type identifier.
		/// </summary>
		/// <param name="identifier">Classidentifier or type identifier</param>
		/// <param name="ctorParams">Arguments passed to the ctor of the resulting type</param>
		/// <returns>Instance of Type retrieved with identifier or null</returns>
		public static T CreateFromClassIdentifierOrType<T>(string identifier, params object[] ctorParams) where T: class
		{
            Type t = FindTypeForIdentifier<T>(identifier);

            if (t == null)
                return null;
            else
                return (T)CreateInstance(t, ctorParams);			
		}
		
		/// <summary>
		/// Creates an instance of a class by specifying their classIdentifier
		/// </summary>
		/// <typeparam name="T">Defines the type or base type of the resulting object</typeparam>
		/// <param name="classIdentifier">Specifies the class identifier of the class that gets instantiated.
		/// The class identifier is specified by attaching the <see>ClassIdentifierAttribute</see> to the target
		/// class</param>
		/// <param name="targetAsm">Specifies the assembly to look for the target type</param>
		/// <param name="ctorParams">Specifies the parameters that are passed to the ctor</param>
		/// <returns>The target type</returns>
		public static T CreateFromClassIdentifier<T>(string classIdentifier, Assembly targetAsm, 
		                                             params object[] ctorParams) where T: class
		{
			foreach(Type t in targetAsm.GetTypes())
			{
				if(t.IsAbstract == false &&
				   t.IsClass == true &&
				   typeof(T).IsAssignableFrom(t))
				{
					
					//We got a candidate that is at least convertible to the desirec type,
					//now check if there is an attached ClassIdentifierAttribute
					object[] attributes = t.GetCustomAttributes(typeof(ClassIdentifierAttribute), false);
					
					if(attributes != null)
					{
						foreach(ClassIdentifierAttribute classIdentifierAttribute in attributes)
						{
							if(classIdentifierAttribute.Identifier.Equals(classIdentifier))
								return (T)CreateInstance(t, ctorParams);
								
						}
					}
				}				   
			}
			
			return null;
			
		}
		
		public static object CreateInstance(Type t, params object[] ctorParams)
		{
			List<Type> types = new List<Type>();
			
			foreach(object ctorParam in ctorParams)
				types.Add(ctorParam.GetType());
			
			ConstructorInfo ctorInfo = t.GetConstructor(types.ToArray());
			
			if(ctorInfo == null)
				throw new ArgumentException("Could not find appropriae ctor");
			
			return ctorInfo.Invoke(ctorParams);
		}
		
	}
}
