package Common;

import java.lang.reflect.InvocationTargetException;
import java.lang.reflect.Method;
import java.util.Vector;
import java.util.function.Function;

public class BasicController extends Thread
{
   private boolean m_keepRunning = true;
   private Vector<BasicRegister> m_listenerArray = new Vector<BasicRegister>();	
   private Vector<BasicEvent> m_eventsDelayed = new Vector<BasicEvent>();

   private static BasicController instance = null;
   protected BasicController() {
   }
	
   public static BasicController getInstance() {
      if(instance == null) {
         instance = new BasicController();
         instance.start();
      }
      return instance;
   }
   	
	public void AddMyEventListener( Object _object, String _name )
	{
		try {
			Method OnBasicEventMethod = _object.getClass().getDeclaredMethod(_name, BasicEvent.class);
			m_listenerArray.addElement( new BasicRegister( _object, OnBasicEventMethod, _name) );
		} catch (Exception e) {
			e.printStackTrace();
		}	
	}

	public boolean RemoveMyEventListener( Object _object, String _name )
	{
		for (int i = 0; i < m_listenerArray.size(); i++)
		{
			BasicRegister item = m_listenerArray.elementAt(i);
			if (item.IsEqual(_object, _name))
			{
				m_listenerArray.removeElementAt(i);
				return true;
			}
		}
		return false;
	}

	public void DispatchMyEvent( String _event, Object... _parameters)
	{
		BasicEvent newEvent = new BasicEvent(_event, -1, _parameters);
		for (int i = 0; i < m_listenerArray.size(); i++)
		{
			BasicRegister item = m_listenerArray.elementAt(i);
			try {
				item.GetMethod().invoke(item.GetObject(), newEvent);
			} catch (Exception e) {
				e.printStackTrace();
			}
		}		
	}

	public void DispatchMyEvent( BasicEvent _event )
	{ 
		for (int i = 0; i < m_listenerArray.size(); i++)
		{
			BasicRegister item = m_listenerArray.elementAt(i);
			try {
				item.GetMethod().invoke(item.GetObject(), _event);
			} catch (Exception e) {
				e.printStackTrace();
			}
		}		
	}
	
	public void DelayMyEvent( String _event, int _delay, Object... _parameters)
	{
		BasicEvent newEvent = new BasicEvent(_event, _delay, _parameters);
		m_eventsDelayed.add(newEvent);
	}
	
	@Override
	public void run() 
	{
		String data = null;
		while (m_keepRunning)
		{
			try 
        	{
				wait(10);
        	} catch (Exception err) {};
        	
        	for (int i = 0; i < m_eventsDelayed.size(); i++)
        	{
        		BasicEvent sEvent = m_eventsDelayed.elementAt(i);
        		if (sEvent.UpdateDelay())
        		{
        			m_eventsDelayed.removeElementAt(i);
        			DispatchMyEvent(sEvent);
        			i--;
        		}
        	}
		}
	}
}
