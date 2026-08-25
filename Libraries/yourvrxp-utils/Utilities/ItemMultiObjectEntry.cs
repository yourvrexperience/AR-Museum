using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace yourvrexperience.Utils
{
	public class ItemMultiObjectEntry : IEqualityComparer<ItemMultiObjectEntry>
	{
		public const string TokenSeparatorEntry = "#";
		public const string TokenSeparatorData = ";";

		private List<object> _objects;

		public List<object> Objects
		{
			get { return _objects; }
		}

		public ItemMultiObjectEntry(params object[] parameters)
		{
			_objects = new List<object>();
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i] != null)
				{
					_objects.Add(parameters[i]);
				}
			}
		}

        public ItemMultiObjectEntry(ItemMultiObjectEntry item)
        {
            _objects = new List<object>();
            for (int i = 0; i < item.Objects.Count; i++)
            {
                _objects.Add(item.Objects[i]);
            }
        }

        public void AddObject(object item)
		{
			if (_objects == null)
			{
				_objects = new List<object>();
			}
			_objects.Add(item);
		}

		public bool EqualsEntry(params object[] parameters)
		{
			bool output = true;
			for (int i = 0; i < _objects.Count; i++)
			{
				if (i < parameters.Length)
				{
					if (_objects[i] != parameters[i])
					{
						output = false;
					}
				}
				else
				{
					output = false;
				}
			}
			return output;
		}

		public bool Equals(ItemMultiObjectEntry origin, ItemMultiObjectEntry target)
		{
			bool output = true;
			for (int i = 0; i < origin.Objects.Count; i++)
			{
				if (i < target.Objects.Count)
				{
					if (origin.Objects[i] != target.Objects[i])
					{
						output = false;
					}
				}
				else
				{
					output = false;
				}
			}
			return output;
		}

		public int GetHashCode(ItemMultiObjectEntry obj)
		{
			int output = 0;
			for (int i = 0; i < obj.Objects.Count; i++)
			{
				output += obj.Objects[i].GetHashCode();
			}
			return output;
		}

		public override string ToString()
		{
			string output = "";
			for (int i = 0; i < _objects.Count; i++)
			{				
				output += _objects[i].GetType().ToString() + TokenSeparatorData + _objects[i].ToString();
				if (i + 1 < _objects.Count)
				{
					output += TokenSeparatorEntry;
				}
			}
			return output;
		}

		public static ItemMultiObjectEntry Parse(string dataString)
		{
			ItemMultiObjectEntry output = new ItemMultiObjectEntry();			
			string[] data = dataString.Split(new String[]{TokenSeparatorEntry}, dataString.Length, StringSplitOptions.None);
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i] != null)
				{
					string[] item = data[i].Split(new String[]{TokenSeparatorData}, data.Length, StringSplitOptions.None);
					if (item.Length == 2)
					{
						Type myType = Type.GetType(item[0]);
						if (myType == Type.GetType("System.Int32"))
						{
							int myIntType;
							if (int.TryParse(item[1], out myIntType))
							{
								output.AddObject(myIntType);
							}							
						}
						else
						if (myType == Type.GetType("System.Double"))
						{							
							float myFloatType;							
							if (float.TryParse(item[1], out myFloatType))
							{
								output.AddObject(myFloatType);
							}							
						}
						else
						if (myType == Type.GetType("System.Boolean"))
						{
							bool myBoolType;
							if (bool.TryParse(item[1], out myBoolType))
							{
								output.AddObject(myBoolType);
							}
						}
						else
						if (myType == Type.GetType("System.String"))
						{
							string myStringType = item[1];
							output.AddObject(myStringType);
						}
					}
				}
			}
			return output;
		}
	}
}
