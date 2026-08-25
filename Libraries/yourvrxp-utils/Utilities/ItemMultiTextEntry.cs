using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;

namespace yourvrexperience.Utils
{
	public class ItemMultiTextEntry
	{
		public const string TokenSeparatorData = ";";

		private List<string> _items;

		public List<string> Items
		{
			get { return _items; }
		}

		public ItemMultiTextEntry(params string[] parameters)
		{
			_items = new List<string>();
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].Length > 0)
				{
					_items.Add(parameters[i]);
				}
			}
		}

		public ItemMultiTextEntry Clone()
		{
			ItemMultiTextEntry output = new ItemMultiTextEntry(_items.ToArray());
			return output;
		}

		public bool EqualsEntry(params string[] list)
		{
			bool output = true;
			for (int i = 0; i < _items.Count; i++)
			{
				if (i < list.Length)
				{
					if (_items[i] != list[i])
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

		public bool EqualsEntry(ItemMultiTextEntry item)
		{
			bool output = true;
			for (int i = 0; i < _items.Count; i++)
			{
				if (i < item.Items.Count)
				{
					if (_items[i] != item.Items[i])
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

		public string Package()
		{
			string output = "";
			for (int i = 0; i < _items.Count; i++)
			{
				if (i < _items.Count - 1)
				{
					output += _items[i] + TokenSeparatorData;
				}
				else
				{
					output += _items[i];
				}
			}
			return output;
		}

		public string Package(string separator)
		{
			string output = "";
			for (int i = 0; i < _items.Count; i++)
			{
				if (i < _items.Count - 1)
				{
					output += _items[i] + separator;
				}
				else
				{
					output += _items[i];
				}
			}
			return output;
		}

		public string PackageInstructions()
		{
			string output = "";
			List<string> instructions = new List<string>();
			for (int i = 1; i < _items.Count; i++)
			{
				instructions.Add(_items[i]);
			}

			for (int i = 0; i < instructions.Count; i++)
			{
				if (i < instructions.Count - 1)
				{
					output += instructions[i] + TokenSeparatorData;
				}
				else
				{
					output += instructions[i];
				}
			}
			return output;
		}

		public void UpdateInstructions(string instructions)
		{
			while (_items.Count > 1)
			{
				_items.RemoveAt(_items.Count - 1);
			}

			string[] replaceInstructions = instructions.Split(new string[] { TokenSeparatorData }, StringSplitOptions.None);
			for (int i = 0; i < replaceInstructions.Length; i++)
			{
				if (replaceInstructions[i].Length > 0)
				{
					_items.Add(replaceInstructions[i]);
				}
			}
#if DEBUG_MODE_DISPLAY_LOG
			for (int i = 0; i < _items.Count; i++)
			{
				Debug.LogError("Inventory Item[" + i + "]=" + _items[i]);
			}
#endif
		}
	}
}
