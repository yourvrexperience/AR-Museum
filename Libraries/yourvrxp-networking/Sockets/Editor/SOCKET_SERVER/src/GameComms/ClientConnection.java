package GameComms;

import java.io.*;
import java.util.*;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.net.Socket;

import Main.*;

public class ClientConnection  {
	
	public static final int MESSAGE_EVENT			= 0;
	public static final int MESSAGE_TRANSFORM		= 1;
	public static final int MESSAGE_DATA			= 2;
	
	public static int TIMEOUT_PING = (2000 * 1000000);
	public static int TIMES_TO_DISCONNECT = 5;
	
	public static int BYTES_SIZE				= 4;
	public static int BYTES_ID_OWNER			= 4;
	public static int BYTES_ID_OBJECT			= 4;	
	public static int BYTES_INDEX_PREFAB		= 4;
	public static int SIZE_BYTES_DOUBLE			= 8;

	private int m_networkID = -1;
	private String m_playerID = "";
	private int m_localID;
	
	private Socket m_socket;

	private InputStream m_is;
	private OutputStream m_os;

	private DataInputStream m_din;
	private DataOutputStream m_dout;
	
	private long m_timeAcum = 0;
	private int m_counterDisconnect = -11;
	private long last_time = 0;
	
	private int m_typeData = -1;
	private int m_dataLength = 0;

	public int GetLocalID() { return m_localID; }
	public int GetNetworkID() { return m_networkID; }
	public String GetPlayerID() { return m_playerID; }	
	public Socket GetSocket() { return m_socket; }

	public void SetNetworkID(int _value) { m_networkID = _value; }
	public void SetPlayerID(String _value) { m_playerID = _value; }	
	
	public static int GetDataSizeTransform()
	{
		return BYTES_ID_OWNER + BYTES_ID_OBJECT + BYTES_INDEX_PREFAB + (SIZE_BYTES_DOUBLE * 10);
	}
	
	ClientConnection(int _localID, Socket _socket) throws Exception
	{
		m_localID = _localID;
		m_socket = _socket;
		m_is = m_socket.getInputStream();
		m_os = m_socket.getOutputStream();
		
		m_din = new DataInputStream(m_is);
		m_dout = new DataOutputStream(m_os);		
	}
	
	public Boolean IsThereDataAvailable() throws Exception
	{
		return (m_socket.getInputStream().available() > 0);
	}
	
	public int NumberDataAvailable() throws Exception
	{
		return (m_socket.getInputStream().available());
	}
	
	public void ClearAllData()
	{
		try 
		{
			byte[] imgDataBa = new byte[1024 * 1024];
			m_din.readFully(imgDataBa);
		}
		catch (Exception err)
		{
		}
	}
	
	public int ReadPacket(ByteArrayOutputStream _packet)
	{
		try 
		{
			if (m_dataLength == 0)
			{
				m_typeData = m_din.readByte();
				byte[] sizeEventBytes = new byte[4];
				int totalBytesRead = m_din.read(sizeEventBytes, 0, 4);
				m_dataLength = BytesToIntegerLE(sizeEventBytes);
			}
			if (m_dataLength - NumberDataAvailable() <= 0)
			{
				byte[] packetData = new byte[m_dataLength];
				m_din.read(packetData, 0, m_dataLength);
				if ((m_dataLength > 0) && (m_dataLength < 200000))
				{
					_packet.write(packetData);
					m_dataLength = 0;
					return m_typeData;
				}
				else
				{
					System.out.println("ClientConnection::ReadPacket::ERROR PACKET TO BIG::typeData="+m_typeData);
					System.out.println("ClientConnection::ReadPacket::ERROR PACKET TO BIG::dataLength="+m_dataLength);
					m_dataLength = 0;
					return -1;
				}			
			}
			else
			{
				if (ServerGame.EnableLogMessages) System.out.println("ClientConnection::ReadPacket::STILL WAITING FOR DATA["+m_dataLength+"]::DataAvailable()="+NumberDataAvailable());
				return -1;
			}
		}
		catch (Exception err)
		{
			return -1;
		}
	}
	
	public String ReadEvent(byte[] _packet) throws Exception
	{
		String data = new String(_packet, "UTF-8");
		if (data.indexOf(ServerRoom.EVENT_CLIENT_TCP_REPONSE_ALIVE)!=-1)
		{			
			m_counterDisconnect = 0;
			return null;
		}
		else
		{
			return data;	
		}		
	}

	public Boolean ReadTransform(byte[] _packet) throws Exception
	{
		if (_packet.length == GetDataSizeTransform())
		{
			return true;
		}
		else
		{
			return false;	
		}		
	}
	
	public static byte[] IntegerToBytesLE(int myInteger)
    {
        return ByteBuffer.allocate(4).order(ByteOrder.LITTLE_ENDIAN).putInt(myInteger).array();
    }

    public static int BytesToIntegerLE(byte[] byteBarray)
    {
        return ByteBuffer.wrap(byteBarray).order(ByteOrder.LITTLE_ENDIAN).getInt();
    }

    public static byte[] IntegerToBytesBE(int myInteger)
    {
        return ByteBuffer.allocate(4).order(ByteOrder.BIG_ENDIAN).putInt(myInteger).array();
    }

    public static int BytesToIntegerBE(byte[] byteBarray)
    {
        return ByteBuffer.wrap(byteBarray).order(ByteOrder.BIG_ENDIAN).getInt();
    }
	
	public void SendEvent(String _message) throws Exception
	{
		ByteArrayOutputStream bObj = new ByteArrayOutputStream();
		bObj.reset();
		bObj.write((byte)MESSAGE_EVENT);
		bObj.write(IntegerToBytesLE(_message.length()));
		bObj.write(_message.getBytes("UTF8"));
		m_dout.write(bObj.toByteArray());
		m_dout.flush();
	}
	
	public void SendTransform(byte[] _tranform) throws Exception
	{
		ByteArrayOutputStream bObj = new ByteArrayOutputStream();
		bObj.reset();
		bObj.write((byte)MESSAGE_TRANSFORM);
		bObj.write(IntegerToBytesLE(_tranform.length));
		bObj.write(_tranform);
		m_dout.write(bObj.toByteArray());
		m_dout.flush();
	}

	public void SendData(byte[] _data) throws Exception
	{
		ByteArrayOutputStream bObj = new ByteArrayOutputStream();
		bObj.reset();
		bObj.write((byte)MESSAGE_DATA);
		bObj.write(IntegerToBytesLE(_data.length));
		bObj.write(_data);
		m_dout.write(bObj.toByteArray());
		m_dout.flush();
	}

	private long DeltaTime()
	{
		long time = System.nanoTime();
		if (last_time == 0)
		{
			last_time = time;
			return 0;
		}
		else
		{
			long delta_time = (time - last_time);
			last_time = time;
			return delta_time;
		}	
	}
	
	public Boolean TestAlive()
	{			
		m_timeAcum += DeltaTime();
		if (m_timeAcum > TIMEOUT_PING)
		{
			m_timeAcum = 0;
			m_counterDisconnect++;
			if (m_counterDisconnect >= TIMES_TO_DISCONNECT)
			{
				if (ServerGame.EnableLogMessages) System.out.println("THE CONNECTION HAS TIMED OUT!!!!!!!!!!!11");
				return false;
			}
		}			
		return true;
	}
		
	public void CloseChannels()
	{
		try 
		{
			if (m_is!=null) m_is.close();
			if (m_os!=null) m_os.close();
			m_din=null;
			m_dout=null;
			m_is=null;
			m_os=null;
		} catch (Exception err) {}
	}
}