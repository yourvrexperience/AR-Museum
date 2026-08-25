using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
    public static class NetworkUtils
    {
		public const string TagSeparator = "<nt>";

        public static void Serialize(object[] _list, ref string _output, ref string _types)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                if (_list[i] is byte)
                {
                    _output += ((byte)_list[i]).ToString();
                    _types += "byte";
                }
                else if (_list[i] is int)
                {
                    _output += ((int)_list[i]).ToString();
                    _types += "int";
                }
                else if (_list[i] is float)
                {
                    _output += ((float)_list[i]).ToString();
                    _types += "float";
                }
                else if (_list[i] is string)
                {
                    _output += ((string)_list[i]);
                    _types += "string";
                }
                else if (_list[i] is bool)
                {
                    _output += ((bool)_list[i]).ToString();
                    _types += "bool";
                }
                else if (_list[i] is Vector3)
                {
                    _output += yourvrexperience.Utils.Utilities.SerializeVector3((Vector3)_list[i]);
                    _types += "Vector3";
                }
                else if (_list[i] is Vector2)
                {
                    _output += yourvrexperience.Utils.Utilities.SerializeVector3((Vector2)_list[i]);
                    _types += "Vector2";
                }
				else if (_list[i] is Quaternion)
                {
                    _output += yourvrexperience.Utils.Utilities.SerializeQuaternion((Quaternion)_list[i]);
                    _types += "Quaternion";
                }
                if (i + 1 < _list.Length)
                {
                    _output += TagSeparator;
                    _types += TagSeparator;
                }
            }
        }

        public static void Deserialize(List<object> _parameters, string _data, string _types)
        {
            string[] data = _data.Split(TagSeparator, StringSplitOptions.None);
            string[] types = _types.Split(TagSeparator, StringSplitOptions.None);

            for (int i = 0; i < data.Length; i++)
            {
                string type = types[i];
                if (type.Equals("byte"))
                {
                    _parameters.Add(byte.Parse(data[i]));
                }
                else if (type.Equals("int"))
                {
                    _parameters.Add(int.Parse(data[i]));
                }
                else if (type.Equals("float"))
                {
                    _parameters.Add(float.Parse(data[i]));
                }
                else if (type.Equals("string"))
                {
                    _parameters.Add(data[i]);
                }
                else if (type.Equals("bool"))
                {
                    _parameters.Add(bool.Parse(data[i]));
                }
                else if (type.Equals("Vector3"))
                {
                    _parameters.Add(yourvrexperience.Utils.Utilities.DeserializeVector3(data[i]));
                }
				else if (type.Equals("Vector2"))
                {
                    _parameters.Add(yourvrexperience.Utils.Utilities.DeserializeVector2(data[i]));
                }
				else if (type.Equals("Quaternion"))
                {
                    _parameters.Add(yourvrexperience.Utils.Utilities.DeserializeQuaternion(data[i]));
                }				
            }
        }
    }
}
