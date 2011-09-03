using System;
using System.Collections.Generic;
using System.Text;

namespace DevEck.ScriptingEngine.Environment
{
    /// <summary>
    /// Implemented by classes that provide data to running scripts
    /// </summary>
    public interface IRuntimeDataProvider
    {
        /// <summary>
        /// Get the Data object with the specified name
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        /// <returns></returns>
        T GetData<T>(string identifier) where T : class;

        /// <summary>
        /// Gets the delegate for the specified method identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        Delegate GetMethod(string identifier);
    }

}
