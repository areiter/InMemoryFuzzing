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


// Author: Andreas Reiter <andreas.reiter@student.tugraz.at>
// Author: Georg Neubauer <georg.neubauer@student.tugraz.at>

using System;

namespace Iaik.Utils.Locking
{

    /// <summary>
    /// Replacement locking method
    /// </summary>
    /// <remarks>
    /// See the example of how to use this. It should only be used in combination with the using(...) statement 
    /// and provides a replacement for the lock(...) statement with the enhancement that e.g. a locking hierachy can be 
    /// written to console (or somewhere else), for debugging purpose
    /// </remarks>
    /// <code>
    /// LockProvider l = new LockProvider(new object(), "My Lock");
    /// using(l.AcquireLock())
    /// {
    ///     //Do stuff here
    /// }
    /// </code>
	public class LockProvider
	{
		private object _lockTarget;
		private string _description;

        /// <summary>
        /// Creates a new LockProvider with the specified lockTarget and description
        /// </summary>
        /// <param name="lockTarget"></param>
        /// <param name="description"></param>
		public LockProvider (object lockTarget, string description)
		{
			_lockTarget = lockTarget;
			_description = description;
		}
		
        /// <summary>
        /// Acquires the lock, use LockContext.Dispose or embedd the contex into a using statement to release the lock
        /// </summary>
        /// <returns></returns>
		public LockContext AcquireLock()
		{
			return new LockContext(_lockTarget, _description);
		}
	}
}
