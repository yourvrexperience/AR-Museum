package Common;

import java.lang.reflect.Method;

public class BasicRegister {

	public Object m_object;
	public Method m_method;
	public String m_name;

	public Object GetObject() { return m_object; };
	public Method GetMethod() { return m_method; };
	public String GetName() { return m_name; };
	
	public BasicRegister(Object _object, Method _method, String _name)
	{
		m_object = _object;
		m_method = _method;
		m_name = _name;
	}
	
	public boolean IsEqual(Object _object, String _name)
	{
		return (m_object == _object) && (m_name == _name);
	}
}
