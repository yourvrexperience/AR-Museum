using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace yourvrexperience.Utils
{
	public class CommEventData
	{
		private string m_nameEvent;
		private bool m_isBinaryResponse;
		private float m_time;
		private object[] m_list;
        private List<ItemMultiTextEntry> m_headers;

        public string NameEvent
		{
			get { return m_nameEvent; }
		}
        public List<ItemMultiTextEntry> Headers
        {
            get { return m_headers; }
        }
        public bool IsBinaryResponse
		{
			get { return m_isBinaryResponse; }
			set { m_isBinaryResponse = value; }
		}
		public float Time
		{
			get { return m_time; }
			set { m_time = value; }
		}
		public object[] List
		{
			get { return m_list; }
		}

		public CommEventData(string _nameEvent, List<ItemMultiTextEntry> _headers, bool _isBinaryResponse, float _time, params object[] _list)
		{
			m_nameEvent = _nameEvent;
            m_headers = _headers;
            m_isBinaryResponse = _isBinaryResponse;
			m_time = _time;
			m_list = _list;
		}

		public void Destroy()
		{
			m_list = null;
		}

	}
}