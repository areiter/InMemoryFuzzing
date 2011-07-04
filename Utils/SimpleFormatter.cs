/**
 *  Original version of this file is available at deveck.net
 **/

using System;
using System.Collections.Generic;
using System.Text;

namespace Iaik.Utils
{

	/// <summary>
	/// Simple Textformatter 
	/// 
	/// {[Name]:ALIGNMENT:WIDTH:FILLCHAR} or {index:ALIGNMENT:WIDTH:FILLCHAR} or {"text":...}
	/// [Name] is the name of a defined text macro
	/// index is the index in the passed parameter list
	/// "text" is a freestyle text to be formatted
	///	ALIGNMENT  =LN ... left-aligned and do not fill
	///			   =LF ... left aligned and fill up
	///			   = M ... centered
	///			   = R ... right-aligned
	///	WIDTH      Tells the formatter to which width the text should be aligned
	///	FILLCHAR   For LF and R. Char used to fill
	/// </summary>
	public class SimpleFormatter
	{
		public delegate string GetParameterDelegate (string parameterName);


		public static string StaticFormat (string format, params object[] args)
		{
			SimpleFormatter formatter = new SimpleFormatter ();
			
			return formatter.Format (format, args);
		}

		/// <summary>
		/// Event is raised for undefined macros to get the macro value
		/// </summary>
		public event GetParameterDelegate OnGetParameter;

		/// <summary>
		/// Contains all the defined macros
		/// </summary>
		private Dictionary<string, string> _textMacros = new Dictionary<string, string> ();

		private bool _throwException = true;
		private bool _ignoreUnknownMakros = false;

		/// <summary>
		/// Tells the formatter to throw or ignore exceptions while formatting
		/// </summary>
		public bool ThrowException {
			get { return _throwException; }
			set { _throwException = value; }
		}

		/// <summary>
		/// Tells the formatter to ignore unknown macros
		/// </summary>
		public bool IgnoreUnknownMacros {
			get { return _ignoreUnknownMakros; }
			set { _ignoreUnknownMakros = value; }
		}

		/// <summary>
		/// Defines a text macro
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void DefineTextMacro (string name, string value)
		{
			if (_textMacros.ContainsKey (name) == false)
				_textMacros.Add (name, "");
			
			_textMacros[name] = value;
		}

		/// <summary>
		/// Removes a text macro
		/// </summary>
		/// <param name="name"></param>
		public void UndefineTextMacro (string name)
		{
			if (_textMacros.ContainsKey (name))
				_textMacros.Remove (name);
		}


		/// <summary>
		/// Formats a string, see class description for formatting instructions
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public string Format (string format, params object[] args)
		{
			StringBuilder formatted = new StringBuilder ();
			int? offset = 0;
			
			while (offset != null) {
				int lastOffset = offset.Value;
				offset = FindStartMarker (offset.Value, format);
				
				try {
					if (offset != null) {
						formatted.Append (format.Substring (lastOffset, offset.Value - lastOffset));
						offset++;
						string currentFormatPacket = GetFormatPacket (ref offset, format);
						formatted.Append (FormatPacket (currentFormatPacket, args));
					} else {
						formatted.Append (format.Substring (lastOffset));
					}
				} catch (Exception) {
					if (_throwException)
						throw;
				}
				
			}
			
			return formatted.ToString ();
		}


		/// <summary>
		/// Finds the next formatable part "{..."
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="str"></param>
		/// <returns></returns>
		private int? FindStartMarker (int offset, string str)
		{
			int index = FindNextNotEscaped (offset, str, "{");
			
			if (index >= 0)
				return index;
			else
				return null;
		}

		/// <summary>
		/// Finds the next not escaped character (tofind)
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="findIn"></param>
		/// <param name="toFind"></param>
		/// <returns></returns>
		private int FindNextNotEscaped (int offset, string findIn, string toFind)
		{
			bool done = false;
			
			while (!done) {
				int index = findIn.IndexOf (toFind, offset);
				
				if (index == -1 || index == 0)
					return index;
				
				
				if (findIn[index - 1] != '\\')
					return index;
				else
					offset = index + 1;
			}
			
			
			return -1;
		}

		/// <summary>
		/// Gets the formatable packet. It assumes that offset is already located after the start marker "{"
		/// </summary>
		/// <param name="offset"></param>
		/// <param name="str"></param>
		/// <returns></returns>
		private string GetFormatPacket (ref int? offset, string str)
		{
			if (offset == null)
				return "";
			int start = offset.Value;
			offset = FindNextNotEscaped (offset.Value, str, "}") + 1;
			
			if (offset == 0)
				throw new FormatException ("Cannot find end Marker '}'");
			
			return str.Substring (start, offset.Value - start - 1);
		}

		/// <summary>
		/// Does the formating stuff
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private string FormatPacket (string format, object[] args)
		{
			string[] splitted = EscapeSafeSplit (format, ':');
			
			string formatted = "";
			
			if (splitted.Length > 0) {
				int index = 0;
				if (int.TryParse (splitted[0], out index)) {
					if (index < 0 || index >= args.Length)
						throw new FormatException ("Argument {" + index.ToString () + "} not found!");
					
					formatted = StringifyParameter (args[index]);
				} else if (splitted[0].StartsWith ("[") && splitted[0].EndsWith ("]")) {
					string macroName = splitted[0].Substring (1, splitted[0].Length - 2);
					
					if (_textMacros.ContainsKey (macroName) == false) {
						if (OnGetParameter != null)
							formatted = StringifyParameter (OnGetParameter (macroName)); else if (_ignoreUnknownMakros)
							return "{" + format + "}";
						else
							throw new FormatException ("Macro '" + macroName + "' not defined");
					} else
						formatted = StringifyParameter (_textMacros[macroName]);
				} else if (splitted[0].StartsWith ("\"") && splitted[0].EndsWith ("\"")) {
					formatted = splitted[0].Substring (1, splitted[0].Length - 2);
				} else
					throw new FormatException ("Unknown format '" + splitted[0] + "'");
			}
			
			
			formatted = Unescape (formatted);
			
			if (splitted.Length > 1) {
				int size = -1;
				if (splitted.Length > 2)
					int.TryParse (splitted[2], out size);
				
				char fillChar = ' ';
				if (splitted.Length > 3) {
					if (splitted[3].Length > 0)
						fillChar = splitted[3][0];
				}
				
				if (splitted[1].ToLower () == "ln" || splitted[1].ToLower () == "l")
					formatted = FormatLN (size, formatted); else if (splitted[1].ToLower () == "lf")
					formatted = FormatLF (size, formatted, fillChar); else if (splitted[1].ToLower () == "m")
					formatted = FormatM (size, formatted, fillChar); else if (splitted[1].ToLower () == "r")
					formatted = FormatR (size, formatted, fillChar);
			}
			
			return formatted;
			
		}

		private string StringifyParameter (object arg)
		{
			if (arg == null)
				return "";
			else
				return arg.ToString ();
		}

		private string Unescape (string escaped)
		{
			int index = 0;
			StringBuilder retVal = new StringBuilder ();
			while (index >= 0) {
				int lastIndex = index;
				index = FindNextNotEscaped (lastIndex + 1, escaped, "\\");
				
				if (index == -1)
					retVal.Append (escaped.Substring (lastIndex));
				else {
					retVal.Append (escaped.Substring (lastIndex, index - lastIndex));
					index++;
				}
			}
			
			return retVal.ToString ();
		}

		private string[] EscapeSafeSplit (string toEscape, char splitBy)
		{
			List<string> str = new List<string> ();
			
			int offset = 0;
			while (offset >= 0) {
				int lastOffset = offset;
				
				offset = FindNextNotEscaped (offset, toEscape, splitBy.ToString ());
				
				if (offset >= 0) {
					str.Add (toEscape.Substring (lastOffset, offset - lastOffset));
					offset++;
				} else
					str.Add (toEscape.Substring (lastOffset));
			}
			
			return str.ToArray ();
		}

		private string FormatLN (int length, string val)
		{
			if (length == -1)
				return val;
			
			if (length < val.Length)
				return val.Substring (0, length);
			else
				return val;
		}

		private string FormatLF (int length, string val, char fillChar)
		{
			if (length == -1)
				return val;
			
			if (length < val.Length)
				return val.Substring (0, length);
			else {
				StringBuilder builder = new StringBuilder ();
				
				for (int i = 0; i < length - val.Length; i++) {
					builder.Append (fillChar);
				}
				
				return val + builder.ToString ();
			}
			
		}

		private string FormatM (int length, string val, char fillChar)
		{
			if (length == -1)
				return val;
			
			if (length < val.Length)
				return val.Substring ((val.Length - length) / 2, length);
			else {
				int left = (int)Math.Floor (((float)length - (float)val.Length) / 2f);
				int right = (int)Math.Ceiling (((float)length - (float)val.Length) / 2f);
				
				string retval = "";
				for (int l = 0; l < left; l++)
					retval += fillChar.ToString ();
				
				retval += val;
				
				for (int r = 0; r < right; r++)
					retval += fillChar.ToString ();
				
				return retval;
			}
		}

		private string FormatR (int length, string val, char fillChar)
		{
			if (length == -1)
				return val;
			
			if (length < val.Length)
                return val.Substring(val.Length - length);
            else
            {
                int left = (length - val.Length);
                string retval = "";
                for (int l = 0; l < left; l++)
                    retval += fillChar.ToString();

                retval += val;
                return retval;
            }
        }
    }
}
 