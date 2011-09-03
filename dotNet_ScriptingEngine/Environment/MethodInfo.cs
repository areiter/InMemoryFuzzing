using System;
using System.Collections.Generic;
using System.Text;

namespace DevEck.ScriptingEngine.Environment
{
    /// <summary>
    /// Represents a Method that can be directly used from script code
    /// </summary>
    public class MethodInfo
    {
        private string _name;

        /// <summary>
        /// Get the name of the method
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        private Type _returnType;

        /// <summary>
        /// Gets the return type of the method or null
        /// </summary>
        public Type ReturnType
        {
            get { return _returnType; }
        }

        private ParameterInfo[] _parameters;

        /// <summary>
        /// Gets the parameters of the method
        /// </summary>
        public ParameterInfo[] Parameters
        {
            get { return _parameters; }
        }

        public MethodInfo(string name, Type returnType, params ParameterInfo[] parameters)
        {
            _name = name;
            _returnType = returnType;
            _parameters = parameters;
        }

        public override bool Equals(object obj)
        {
            if (obj is MethodInfo)
            {
                return ((MethodInfo)obj)._name.Equals(_name);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }
    }
}
