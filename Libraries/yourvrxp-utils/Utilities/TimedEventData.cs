using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace yourvrexperience.Utils
{
	public class TimedEventData
	{
		private string _nameEvent;
		private float _time;
		private int _origin;
		private int _target;
		private object[] _parameters;

		public string NameEvent
		{
			get { return _nameEvent; }
		}
		public float Time
		{
			get { return _time; }
			set { _time = value; }
		}
		public int Origin
		{
			get { return _origin; }
			set { _origin = value; }
		}
		public int Target
		{
			get { return _target; }
			set { _target = value; }
		}

		public object[] Parameters
		{
			get { return _parameters; }
		}

		public TimedEventData(string nameEvent, float time, params object[] parameters)
		{
			_nameEvent = nameEvent;
			_origin = -1;
			_target = -1;
			_time = time;
			_parameters = parameters;
		}

		public TimedEventData(string nameEvent, int origin, int target, float time, params object[] parameters)
		{
			_nameEvent = nameEvent;
			_origin = origin;
			_target = target;
			_time = time;
			_parameters = parameters;
		}

		public void Destroy()
		{
			_parameters = null;
			_time = -1000;
		}

	}
}