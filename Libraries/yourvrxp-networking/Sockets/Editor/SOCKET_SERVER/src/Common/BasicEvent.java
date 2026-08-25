package Common;

public class BasicEvent {

	public String m_type;
	public Object[] m_params;

	private long m_delay = -1;
	private long m_timeAcum = 0;
	private long m_previousTime = -1;
	
	public String GetType() { return m_type; }
	public Object[] GetParams() { return m_params; }

	public BasicEvent(String _type, long _delay, Object... _params)
	{
		m_type = _type;
		m_delay = _delay;
		m_params = new Object[_params.length];
		for (int i = 0; i < m_params.length; i++)
		{
			m_params[i] = _params[i];
		}
	}

	private long GetDeltaTime()
	{
	   	if (m_previousTime == -1)
	   	{
	   		m_previousTime = System.nanoTime();	
	   	}
		long currentTime = System.nanoTime();
		long deltaTime = currentTime - m_previousTime;
		m_previousTime = currentTime;
		
		return (deltaTime / 1000000);
	}

	public boolean UpdateDelay()
	{
		if (m_delay > 0)
		{
			m_delay -= GetDeltaTime();
			if (m_delay <= 0)
			{
				m_delay = -1;
				return true;
			}
			else
			{
				return false;
			}
		}
		else
		{
			return true;
		}
	}
}
