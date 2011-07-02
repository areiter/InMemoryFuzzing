/* Copyright 2010 Andreas Reiter <andreas.reiter@student.tugraz.at>
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


using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.Serialization;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Iaik.Utils
{
    public static class XmlHelper
    {

        #region Base Types


        public static bool ReadBool(XmlElement rootElement, string name, bool defaultValue)
        {
            string val = ReadString(rootElement, name);

            try
            {
                bool bVal = false;
                if (bool.TryParse(val, out bVal))
                    return bVal;
                else
                    return defaultValue;
            }
            catch (Exception)
            {
            }
            return defaultValue;
        }

        public static void WriteBool(XmlElement rootElement, string name, bool value)
        {
            WriteString(rootElement, name, value.ToString());
        }


        public static int ReadInt(XmlElement rootElement, string name, int defaultValue)
        {
            int? val = ReadInt(rootElement, name);

            if (val == null)
                return defaultValue;
            else
                return val.Value;
        }

        public static int? ReadInt(XmlElement rootElement, string name)
        {
            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element != null)
            {
                int val = 0;
                if (int.TryParse(element.InnerText, out val))
                    return val;
                return null;
            }
            return null;
        }

        public static void WriteInt(XmlElement rootElement, string name, int? value)
        {
            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element == null)
                element = (XmlElement)rootElement.AppendChild(rootElement.OwnerDocument.CreateElement(name));

            if (value == null)
                element.InnerText = "";
            else
                element.InnerText = value.Value.ToString();
        }

        public static int[] ReadIntArray(XmlElement rootElement, string name)
        {
            List<int> list = new List<int>();

            foreach (XmlElement el in rootElement.SelectNodes(name))
            {
                if (el != null)
                {
                    int val = 0;
                    if (int.TryParse(el.InnerText, out val))
                        list.Add(val);
                }

            }

            return list.ToArray();
        }

        public static void WriteIntArray(XmlElement rootElement, string name, int[] array)
        {
            XmlNodeList nl = rootElement.SelectNodes(name);
            foreach (XmlElement el in nl)
            {
                rootElement.RemoveChild(el);
            }

            foreach (int i in array)
            {
                XmlElement element = (XmlElement)rootElement.AppendChild(rootElement.OwnerDocument.CreateElement(name));
                element.InnerText = i.ToString();
            }
        }

        public static string ReadString(XmlElement rootElement, string name)
        {
            if (rootElement == null)
                return null;

            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element != null)
            {
                return element.InnerText;
            }
            return null;
        }

        public static string[] ReadStringArray(XmlElement rootElement, string name)
        {
            List<string> list = new List<string>();

            foreach (XmlElement el in rootElement.SelectNodes(name))
            {
                if (el != null)
					list.Add(el.InnerText);					         
            }

            return list.ToArray();
        }

		
        public static void WriteString(XmlElement rootElement, string name, string value)
        {
            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element == null)
                element = (XmlElement)rootElement.AppendChild(rootElement.OwnerDocument.CreateElement(name));

            element.InnerText = value;

        }

        public static void WriteStringArray(XmlElement rootElement, string name, string[] array)
        {
            XmlNodeList nl = rootElement.SelectNodes(name);
            foreach (XmlElement el in nl)
            {
                rootElement.RemoveChild(el);
            }

            foreach (string s in array)
            {
                XmlElement element = (XmlElement)rootElement.AppendChild(rootElement.OwnerDocument.CreateElement(name));
                element.InnerText = s;
            }
        }

		
        public static double? ReadDouble(XmlElement rootElement, string name)
        {
            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element != null)
            {
                double val = 0;
                if (double.TryParse(element.InnerText, System.Globalization.NumberStyles.Number,
                    System.Globalization.NumberFormatInfo.InvariantInfo, out val))
                    return val;
                return null;
            }
            return null;
        }

        public static void WriteDouble(XmlElement rootElement, string name, double? value)
        {
            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element == null)
                element = (XmlElement)rootElement.AppendChild(rootElement.OwnerDocument.CreateElement(name));

            if (value == null)
                element.Value = "";
            else
                element.Value = value.Value.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static T ReadEnum<T>(XmlElement rootElement, string name, T defaultVal)
        {
            string enumValue = ReadString(rootElement, name);

            if (enumValue == null)
                return defaultVal;
            else
            {
                try
                {
                    return (T)Enum.Parse(typeof(T), enumValue);
                }
                catch (Exception)
                {
                    return defaultVal;
                }
            }
        }

        public static void WriteEnum(XmlElement rootElement, string name, object value)
        {
            WriteString(rootElement, name, value.ToString());
        }

        public static T ReadSerializableObject<T>(XmlElement element)
            where T : ISerializable
        {
            using (MemoryStream sink = new MemoryStream(Convert.FromBase64String(element.InnerText)))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (T)formatter.Deserialize(sink);
            }
        }

        public static void WriteSerializableObject(XmlElement element, ISerializable obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);

                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                element.InnerText = Convert.ToBase64String(stream.ToArray());
            }

        }

        public static DateTime? ReadDateTime(XmlElement rootElement, string name)
        {
            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element != null)
            {
                if (element.InnerText == null || element.InnerText == "")
                    return null;

                return new DateTime(long.Parse(element.InnerText));

            }
            return null;
        }

        public static void WriteDateTime(XmlElement rootElement, string name, DateTime? dateTime)
        {
            XmlElement element = (XmlElement)rootElement.SelectSingleNode(name);

            if (element == null)
                element = (XmlElement)rootElement.AppendChild(rootElement.OwnerDocument.CreateElement(name));

            if (dateTime != null)
                element.InnerText = dateTime.Value.Ticks.ToString();
        }
#endregion

    }
}