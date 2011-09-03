using System;
using System.Collections.Generic;
using System.Text;

namespace DevEck.ScriptingEngine.Environment
{
    /// <summary>
    /// Defines the currently available scripting languages
    /// The whole provided code needs to be in this language.
    /// Partial scripts are not supported
    /// </summary>
    public enum ScriptingLanguage
    {
        /// <summary>
        /// Use VisualBasic .NET
        /// </summary>
        VBdotNet,

        /// <summary>
        /// Use C#
        /// </summary>
        CSharp
    }

    /// <summary>
    /// Implemented by classes that provide additional Objects or Data
    /// to User-Written Scripts
    /// </summary>
    /// <remarks>
    /// A minimal implementation would be to simply pass an empty Script environment.
    /// You can imagine this like any program that is compiled at runtime
    /// </remarks>
    public interface IScriptEnvironment
    {
        /// <summary>
        /// Returns all globally defined parameters.
        /// </summary>
        IList<ParameterInfo> GlobalParameters { get; }

        /// <summary>
        /// Returns all globally defined Methods
        /// </summary>
        IList<MethodInfo> GlobalMethods { get; }

        /// <summary>
        /// Sets the given global parameter to the specified value
        /// </summary>
        /// <param name="parameterInfo"></param>
        /// <param name="parameter"></param>
        void SetParameter(ParameterInfo parameterInfo, object parameter);

        /// <summary>
        /// Sets the method to the given delegate
        /// </summary>
        /// <param name="methodInfo"></param>
        /// <param name="method"></param>
        void SetMethod(MethodInfo methodInfo, Delegate method);

        /// <summary>
        /// Gets or Sets the Code of the Entry method
        /// </summary>
        /// <remarks>
        /// For simple script support only MainCode is needed.
        /// To enhance the ScriptEngine functionality you can use ExtraCode
        /// </remarks>
        string MainCode { get; set; }

        /// <summary>
        /// Gets or Sets ExtraCode
        /// </summary>
        /// <remarks>
        /// This code must be full code with class Definition
        /// the resulting code would look as follows
        /// <code>
        /// public class MyScript
        /// {
        ///     //global properties
        ///     
        ///     //global methods
        ///     
        ///     public void Execute()
        ///     {
        ///         //Main Code goes here
        ///     }
        ///     
        ///     //Extra Code goes here
        ///     class foo
        ///     {
        ///         void bar()
        ///         {
        ///         }
        ///     }
        /// }
        /// </code>
        /// </remarks>
        string ExtraCode { get; set; }

        /// <summary>
        /// Executes the script
        /// </summary>
        void Execute();
    }
}
