using System;
using System.Collections.Generic;
using System.Text;

namespace DevEck.ScriptingEngine.Environment.Basic
{
    /// <summary>
    /// Defines some options that are applied while code generation
    /// </summary>
    public class BasicCodeGenerationOptions
    {
        private string _namespace = "DevEck.ScriptingEngine.Environment.Basic.Script";

        /// <summary>
        /// Gets the namespace in which to generate the Script Code
        /// </summary>
        public string Namespace
        {
            get { return _namespace; }
        }

    }
}
