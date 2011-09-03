using System;
using System.Collections.Generic;
using System.Text;

namespace DevEck.ScriptingEngine.Environment
{
    /// <summary>
    /// Describes a single Parameter
    /// </summary>
    public class ParameterInfo
    {
        private string _name;
        /// <summary>
        /// Name of the parameter
        /// </summary>
        /// <remarks>
        /// The name that can be used to access this parameter 
        /// from script code
        /// </remarks>
        public string Name
        {
            get { return _name; }
        }

        private Type _parameterType;
        /// <summary>
        /// Type of the parameter
        /// </summary>
        public Type ParameterType
        {
            get { return _parameterType; }
        }


        public ParameterInfo(string name, Type parameterType)
        {
            _name = name;
            _parameterType = parameterType;
        }


        public override bool Equals(object obj)
        {
            if (obj is ParameterInfo)
            {
                return ((ParameterInfo)obj).Name.Equals(_name);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }

    }
}
