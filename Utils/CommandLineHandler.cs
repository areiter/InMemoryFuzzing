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
using System.Collections;
using System.Collections.Generic;

namespace Iaik.Utils
{
	
	/// <summary>
	/// Parses command line options by name and calls the appropriate (registered) handler 
	/// or the default handler which indicates that no command handler could be found for 
	/// the command line options
	/// </summary>
	/// <remarks>
	/// There are two types of command line switches, binary switches where the option is supplied or not, no further arguments re allowed for these switches.
	/// the second type are the default commandline switches in the form "-name=value" where everything behind the first '=' is taken as value 
	///</remarks>
	public class CommandLineHandler
	{
        private const string NAME_VALUE_SEPERATOR = "=";
        private static string[] Prefixes = new string[] { "--", "-" };

        /// <summary>
        /// Raised if an unregistered CommandOption is detected
        /// </summary>
        public event Action<string> UnknownCommandOption;

		/// <summary>
		/// Saves the CommandOption callback for each command
		/// </summary>
		private IDictionary<string, Action<CommandOption>> _optionCallbacks = new Dictionary<string, Action<CommandOption>>();
		
		/// <summary>
		/// Registers a new callback for the given commandoption name.
		/// If there is already an registered callback for the name provided, it gets overriden
		/// </summary>
		/// <param name="name"></param>
		/// <param name="callback"></param>
		public void RegisterCallback(string name, Action<CommandOption> callback)
		{
			if(_optionCallbacks.ContainsKey(name))
				_optionCallbacks[name] = callback;
			else
				_optionCallbacks.Add(name, callback);
		}

        /// <summary>
        /// Parses the given command line options, raises the callback events if any specified
        /// and returns all parsed CommandOption objects
        /// </summary>
        /// <param name="commandLineOptions"></param>
        /// <returns></returns>
        public CommandLineOptions Parse(string[] commandLineOptions)
        {
			CommandLineOptions parsedCommandOptions = new CommandLineOptions();

            foreach (string option in commandLineOptions)
            {
                int? prefixLength = HasValidPrefix(option);

                if (prefixLength == null || prefixLength.Value == option.Length)
                {
                    RaiseUnknownCommandOptionEvent(option);
                    continue;
                }
                int nameValueSeperatorPosition = option.IndexOf(NAME_VALUE_SEPERATOR, prefixLength.Value);

                //Not found? it is a binary command line option
                if (nameValueSeperatorPosition == -1)
                {
                    CommandOption newBinaryCommandOption = new CommandOption(CommandOption.CommandOptionType.Binary, option.Substring(prefixLength.Value));
                    ProcessCommandOption(newBinaryCommandOption);
                    parsedCommandOptions.Add(newBinaryCommandOption);
                }
                else
                {
                    CommandOption newCommandOption = new CommandOption(CommandOption.CommandOptionType.Value,
                        option.Substring(prefixLength.Value, nameValueSeperatorPosition - prefixLength.Value),
                        option.Substring(nameValueSeperatorPosition + 1)
                        );
                    ProcessCommandOption(newCommandOption);
                    parsedCommandOptions.Add(newCommandOption);
                }
            }

            return parsedCommandOptions;
        }

        /// <summary>
        /// Raises the callback associated with this command option,
        /// or the unknown command if no command option callback is registered
        /// </summary>
        /// <param name="commandOption"></param>
        private void ProcessCommandOption(CommandOption commandOption)
        {
            if (_optionCallbacks.ContainsKey(commandOption.Name) == false)
                RaiseUnknownCommandOptionEvent(commandOption.Name);
            else
                _optionCallbacks[commandOption.Name](commandOption);
        }


        /// <summary>
        /// Checks if the given command line option has a valid Prefix (defined in Prefixes)
        /// </summary>
        /// <param name="option"></param>
        /// <returns>returns null on invalid prefix otherwise length of prefix</returns>
        private int? HasValidPrefix(string option)
        {
            foreach (string prefix in Prefixes)
            {
                if (option.StartsWith(prefix))
                    return prefix.Length;
            }

            return null;
            
        }

        /// <summary>
        /// Null-Safe Event raiser
        /// </summary>
        /// <param name="option"></param>
        private void RaiseUnknownCommandOptionEvent(string option)
        {
            if (UnknownCommandOption != null)
                UnknownCommandOption(option);
        }
		
		/// <summary>
		/// Represents a single CommandOption parsed from the CommanLineHandler
		/// </summary>
		public class CommandOption
		{
			/// <summary>
			/// Defines the different available Command Option types
			/// </summary>
			public enum CommandOptionType
			{
				/// <summary>
				/// Binary option type where no further arguments are allowed:
				/// "--enable-this"
				/// </summary>
				Binary,
				
				/// <summary>
				/// Value option type where an argument is supplied with the command line option:
				/// "--name=value"
				/// </summary>
				Value
			}
			
			private CommandOptionType _optionType;
			
			/// <value>
			/// Returns the CommandOptionType (<see>CommandOptionType</see>)
			/// </value>
			public CommandOptionType OptionType
			{
				get{ return _optionType;}
			}
			
			private string _name;
			
			/// <value>
			/// Returns the name of the argument
			/// </value>
			public string Name
			{
				get{ return _name;}
			}
			
			private List<string> _arguments = new List<string>();
			
			/// <value>
			/// Returns the arguments of this CommandOption or an empty error if no option are available
			/// </value>
			public string[] Arguments
			{
				get{ return _arguments.ToArray();}
			}
			
			internal CommandOption(CommandOptionType optionType, string name, params string[] arguments)
			{
				_optionType = OptionType;
				_name = name;
				_arguments.AddRange(arguments);
			}
		}
		
		/// <summary>
		/// Collects all parsed CommandOptionss
		/// </summary>
		public class CommandLineOptions : List<CommandOption>
		{
			/// <summary>
			/// Looks for a parsed command option with the given name
			/// </summary>
			public CommandOption FindCommandOptionByName(string name)
			{
				foreach(CommandOption opt in this)
				{
					if(opt.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
						return opt;
				}
				
				return null;
			}
			
			/// <summary>
			/// Returns a typed command option argument
			/// </summary>
			/// <param name="name">Name of the command option to retrieve</param>
			/// <returns></returns>
			public T FindCommandOptionValueByName<T>(string name)
			{
				CommandOption cmdOpt = FindCommandOptionByName(name);
				
				if(cmdOpt == null)
					throw new ArgumentException(string.Format("Could not find argument '{0}'", name));
				
				if(cmdOpt.Arguments.Length == 0)
					throw new ArgumentException(string.Format("Argument '{0}' does not have a value!", name));
					                            
				return (T)Convert.ChangeType(cmdOpt.Arguments[0], typeof(T));
			}
		}
	}
}
