using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace yourvrexperience.Networking
{
	public class PlayerConnectionData : IEquatable<PlayerConnectionData>
	{
		private int _id;
		private string _networkAddress;
		private GameObject _referenceObject;
		private List<Dictionary<string, object>> _messages = new List<Dictionary<string, object>>();
		private byte[] _binaryData;

		public int Id
		{
			get { return _id; }
		}
		public string Name
		{
			get { return "Client[" + _id + "]"; }
		}
		public string NetworkAddress
		{
			get { return _networkAddress; }
			set { _networkAddress = value; }
		}
		public GameObject Reference
		{
			get { return _referenceObject; }
		}
		public int TotalMessages
        {
			get { return _messages.Count; }
        }

		public PlayerConnectionData(int id, GameObject reference)
		{
			_id = id;
			_referenceObject = reference;
		}

		public void Destroy()
		{
			_referenceObject = null;
		}

		public void PushMessage(Dictionary<string, object> data)
		{
			_messages.Add(data);
		}

		public void SetBinaryData(byte[] binaryData)
		{
			// GET NAME EVENT
			int counter = 0;
			int sizeNameEvent = BitConverter.ToInt32(binaryData, counter);
			counter += 4;
			byte[] binaryNameEvent = new byte[sizeNameEvent];
			Array.Copy(binaryData, counter, binaryNameEvent, 0, sizeNameEvent);
			counter += sizeNameEvent;
			string nameEvent = Encoding.ASCII.GetString(binaryNameEvent);

			// GET DATA CONTENT
			int sizeContentEvent = BitConverter.ToInt32(binaryData, counter);
			counter += 4;
			_binaryData = new byte[sizeContentEvent];
			Array.Copy(binaryData, counter, _binaryData, 0, sizeContentEvent);

			// DISPATCH LOCAL EVENT
			// NetworkEventController.Instance.DispatchLocalEvent(nameEvent, _id, _binaryData);
		}

		public Dictionary<string, object> PopMessage()
		{
			if (_messages.Count == 0) return null;
			Dictionary<string, object> message = _messages[_messages.Count - 1];
			_messages.RemoveAt(_messages.Count - 1);
			return message;
		}

		public int GetHashCode(PlayerConnectionData obj)
		{
			return obj.Id;
		}

		public bool Equals(PlayerConnectionData other)
		{
			return Id == other.Id;
		}
	}
}
