package GameComms;

import java.awt.Point;
import java.net.Socket;
import java.net.URL;
import java.net.HttpURLConnection;
import java.nio.ByteBuffer;
import java.io.*;
import java.util.*;
import java.nio.ByteOrder;

import Common.BasicController;
import Main.*;

public class ServerRoom extends Thread {

	public static String EVENT_CLIENT_TCP_CONNECTED_ROOM 			= "EVENT_CLIENT_TCP_CONNECTED_ROOM";
	public static String EVENT_CLIENT_TCP_PING_ALIVE 				= "EVENT_CLIENT_TCP_PING_ALIVE";
	public static String EVENT_CLIENT_TCP_REPONSE_ALIVE 			= "EVENT_CLIENT_TCP_REPONSE_ALIVE";
	public static String EVENT_STREAMSERVER_REPORT_CLOSED_STREAM 	= "EVENT_STREAMSERVER_REPORT_CLOSED_STREAM";
	public static String EVENT_SYSTEM_PLAYER_HAS_BEEN_DESTROYED		= "EVENT_SYSTEM_PLAYER_HAS_BEEN_DESTROYED";
	public static String EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM		= "EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM";
	public static String EVENT_CLIENT_TCP_UPDATE_EXTRA_ROOM_DATA	= "EVENT_CLIENT_TCP_UPDATE_EXTRA_ROOM_DATA";

	public static String EVENT_SERVERROOM_DESTROYED 	= "EVENT_SERVERROOM_DESTROYED";
	
	public Boolean IsLobby = true;
	public int IdRoom = -2;
	public String NameRoom = "";
	public String[] PlayersIDs;
	public Boolean IsRoomOpen = true;	
	
	private Boolean m_runServer = true;
    private Vector<ClientConnection> m_listClients = new Vector<ClientConnection>();
	private ServerClients m_parent;
	private Boolean m_isInited = false;	
	private int m_counterIDs = 1;
	private ClientConnection m_currentConnection;
	private int m_totalPlayers = -1;
	private int m_hostRoomID = -1;
	private String m_extraData = "";
	private boolean m_destroyedResources = false;
	
	public int GetTotalNumberPlayers()
	{
		return m_totalPlayers;
	}
		
    public ServerRoom(String _nameRoom, int _numberOfPlayers, ServerClients _parent)
    {
		NameRoom = _nameRoom;
		m_totalPlayers = _numberOfPlayers;	
		if (ServerGame.EnableLogMessages) System.out.println("+++++++++++++++++++ServerRoom["+NameRoom+"]::WAITING FOR PLAYERS["+m_totalPlayers+"]");		
		m_parent = _parent;		
    }
    
    public void Destroy()
    {
    	if (m_runServer)
    	{
    		if (!m_destroyedResources)
    		{
        		m_destroyedResources = true;
        		CloseAllClients();
        		DeleteRoom();
            	m_runServer = false;
    		}
    	}
    }
	
	public String PackRoomPlayersIDs()
	{
		return NameRoom + ServerGame.TOKEN_SEPARATOR_PLAYERS_IDS + m_totalPlayers;
	}
    
    public boolean ExistClient(Socket _newClient) 
	{
		for (int i = 0; i < m_listClients.size(); i++)
        {
			ClientConnection clientSocket = m_listClients.elementAt(i);
			if (clientSocket.GetSocket() == _newClient) return true;
        }
		return false;
	}

    public boolean ExistPlayersID(String _facebookID)
	{
		if (ServerGame.EnableLogMessages) System.out.println("+++++++++++++++++++ServerRoom::ExistFacebookID["+_facebookID+"]");		
		for (int i = 0; i < PlayersIDs.length; i++)
        {
			if (PlayersIDs[i].equals(_facebookID))
			{
				if (ServerGame.EnableLogMessages) System.out.println("+++++++++++++++++++ServerRoom::FOUND INVITATION!!!!");		
				return true;
			}				
        }
		if (ServerGame.EnableLogMessages) System.out.println("+++++++++++++++++++ServerRoom::NOT FOUND INVITATION");		
		return false;
	}
	
	private void TestClientsAlive()
	{
		for (int i = 0; i < m_listClients.size(); i++)
        {
			ClientConnection clientSocket = m_listClients.elementAt(i);
			if (clientSocket != null)
			{
				if (!clientSocket.TestAlive())
				{
					int disconnectedNetworkID = clientSocket.GetNetworkID();
					if (DeleteClient(clientSocket))
					{
						i--;
						if (clientSocket.GetNetworkID() == 1)
						{
							BroadCastEvent(EVENT_STREAMSERVER_REPORT_CLOSED_STREAM + ServerGame.TOKEN_SEPARATOR_EVENTS 
											+ disconnectedNetworkID  + ServerGame.TOKEN_SEPARATOR_EVENTS
											+ disconnectedNetworkID + ServerGame.TOKEN_SEPARATOR_EVENTS
											+ "-1" + ServerGame.TOKEN_SEPARATOR_EVENTS 
											+ disconnectedNetworkID);
							m_runServer = false;
						}
						else
						{
							BroadCastEvent(EVENT_SYSTEM_PLAYER_HAS_BEEN_DESTROYED  + ServerGame.TOKEN_SEPARATOR_EVENTS 
											+ disconnectedNetworkID  + ServerGame.TOKEN_SEPARATOR_EVENTS 
											+ disconnectedNetworkID + ServerGame.TOKEN_SEPARATOR_EVENTS 
											+ "-1" + ServerGame.TOKEN_SEPARATOR_EVENTS 
											+ disconnectedNetworkID);											
						}						
					}
				}
			}
        }		
		if (m_listClients.size() == 0)
		{
			m_runServer = false;
		}
	}
	

	private boolean DeleteClient(ClientConnection _clientClosed) 
	{
		for (int i = 0; i < m_listClients.size(); i++)
        {
			ClientConnection clientSocket = m_listClients.elementAt(i);
			if (clientSocket == _clientClosed)
			{
				clientSocket.CloseChannels();
				m_listClients.removeElementAt(i);
				return true;
			}
        }
		return false;
	}
	
	private void CloseAllClients() 
	{
		BasicController.getInstance().DispatchMyEvent(EVENT_SERVERROOM_DESTROYED, IdRoom);		
		for (int i = 0; i < m_listClients.size(); i++)
        {
			ClientConnection clientSocket = m_listClients.elementAt(i);
			if (clientSocket != null)
			{
				clientSocket.CloseChannels();
			}
        }		
		m_listClients.clear();
	}
	
	public void AddNewClient(Socket _newClient) throws Exception
	{
		if (ServerGame.EnableLogMessages) System.out.println("ServerRoom::AddNewClient");
		if ((IdRoom == -1) || (!ExistClient(_newClient)))
    	{			
			ClientConnection clientConnection = new ClientConnection(m_listClients.size(), _newClient);
			m_listClients.add(clientConnection);
			clientConnection.SetNetworkID(m_counterIDs);
			m_counterIDs++;
			if (m_listClients.size() >= m_totalPlayers)
			{
				if (IsRoomOpen)
				{
					CompletedRoom();	
				}
				IsRoomOpen = false;				
			}
			BroadCastEvent(EVENT_CLIENT_TCP_CONNECTED_ROOM + ServerGame.TOKEN_SEPARATOR_EVENTS 
							+ clientConnection.GetNetworkID() + ServerGame.TOKEN_SEPARATOR_EVENTS 
							+ clientConnection.GetNetworkID() + ServerGame.TOKEN_SEPARATOR_EVENTS 
							+ "-1" + ServerGame.TOKEN_SEPARATOR_EVENTS 
							+ clientConnection.GetNetworkID() + ServerGame.TOKEN_SEPARATOR_EVENTS 
							+ "1" + ServerGame.TOKEN_SEPARATOR_EVENTS
							+ m_totalPlayers);
        	if (ServerGame.EnableLogMessages) System.out.println("++++++New client in room::clientConnection.NetworkID="+clientConnection.GetNetworkID());
			m_isInited = true;
    	}
        else
        {
        	if (ServerGame.EnableLogMessages) System.out.println("Existing client in room");
        }
	}

	private void BroadCastEvent(String _message)
	{
		try 
		{
			if (_message != null)
			{
				int i = 0;
				for (i = 0; i < m_listClients.size(); i++)
				{
					ClientConnection clientConnection = m_listClients.elementAt(i);
					clientConnection.SendEvent(_message);
				}
			}
		} catch (Exception err) {
		};
	}
	
	private void BroadCastTransform(byte[] _transform)
	{
		try 
		{
			if (_transform != null)
			{
				for (int i = 0; i < m_listClients.size(); i++)
				{
					ClientConnection clientConnection = m_listClients.elementAt(i);
					if (m_currentConnection != clientConnection)
					{
						clientConnection.SendTransform(_transform);	
					}					
				}
			}
		} catch (Exception err) {};
	}

	private void BroadCastData(byte[] _data)
	{
		try 
		{
			if (_data != null)
			{
				for (int i = 0; i < m_listClients.size(); i++)
				{
					ClientConnection clientConnection = m_listClients.elementAt(i);
					if (m_currentConnection != clientConnection)
					{
						clientConnection.SendData(_data);	
					}					
				}
			}
		} catch (Exception err) {};
	}
	
	private void ProcessEvent(String _event) throws Exception
	{
		if (_event.indexOf(EVENT_STREAMSERVER_REPORT_CLOSED_STREAM)!=-1)
		{
			BroadCastEvent(_event);
			m_runServer = false;
		}
		else
		{
			if (IsRoomOpen)
			{
				if (_event.indexOf(EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM)!=-1)
				{
					IsRoomOpen = false;
					m_totalPlayers = m_listClients.size();
					if (ServerGame.EnableLogMessages) System.out.println("ServerRoom::ProcessEvent:CLOSING ROOM WITH "+ m_totalPlayers + " PLAYERS");
				}
				else
				{
					BroadCastEvent(_event);	
				}
			}
			else
			{
				BroadCastEvent(_event);	
			}
		}
	}
	
	private void ReadSocket()
	{
		for (int i = 0; i < m_listClients.size(); i++)
		{
			ClientConnection clientConnection = m_listClients.elementAt(i);
			try 
			{
				if (clientConnection.IsThereDataAvailable())
				{
					ByteArrayOutputStream packet = new ByteArrayOutputStream();
					switch (clientConnection.ReadPacket(packet))
					{
						case ClientConnection.MESSAGE_EVENT:
							String messageBroadcast = clientConnection.ReadEvent(packet.toByteArray());
							if (messageBroadcast != null)
							{
								ProcessEvent(messageBroadcast);	
							}						
							break;
							
						case ClientConnection.MESSAGE_TRANSFORM:
							byte[] packetTransform = packet.toByteArray();
							if (clientConnection.ReadTransform(packetTransform))
							{
								m_currentConnection = clientConnection;
								BroadCastTransform(packetTransform);	
							}							
							break;
							
						case ClientConnection.MESSAGE_DATA:
							byte[] packetData = packet.toByteArray();
							m_currentConnection = clientConnection;
							BroadCastData(packetData);	
							break;		

						default:
							break;
					}
				}				
			} catch (Exception err)
			{
				clientConnection.ClearAllData();
				if (ServerGame.EnableLogMessages) System.out.println("ServerRoom::ReadMessage:Exception="+err.getMessage());
			}
		}
	}

	private void CompletedRoom()
	{
		try 
		{
			String urlPath = ServerGame.URL_COMPLETED_GAME;
			urlPath += "room="+IdRoom;
			URL url = new URL(urlPath);
			HttpURLConnection urlConnection = (HttpURLConnection)url.openConnection();
			urlConnection.connect();
			
			StringBuilder sb = new StringBuilder();
            BufferedReader reader = new BufferedReader(new InputStreamReader(urlConnection.getInputStream()));
            String line;
            while ((line = reader.readLine())!= null){
                sb.append(line);
                sb.append("\n");
            }
			if (ServerGame.EnableLogMessages)  System.out.println("HTTP RESPONSE="+line);
		} catch (Exception err) {};
	}
	
	private void DeleteRoom()
	{
		if (m_parent.DeleteRoom(this))
		{
			if (ServerGame.EnableLogMessages) System.out.println("ServerRoom::run:DELETE ROOM SUCCESS!!!!!!");
		}
		else
		{
			if (ServerGame.EnableLogMessages) System.out.println("ServerRoom::run:DELETE ROOM FAILURE!!!!!!");
		}				
	}

	@Override
	public void run() 
	{
		while (m_runServer)
		{
			try 
        	{
				wait(2);
        	} catch (Exception err) {};
			
			try 
        	{
				if (m_isInited) TestClientsAlive();
				ReadSocket();
			} catch (Exception err) {}
		}
		if (!m_runServer)
		{
    		if (!m_destroyedResources)
    		{
        		m_destroyedResources = true;
				CloseAllClients();
				DeleteRoom();
    		}
		}
	}	
}
