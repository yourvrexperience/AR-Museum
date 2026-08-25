package Main;

import java.awt.Desktop;
import java.awt.EventQueue;
import java.awt.Font;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.awt.image.BufferedImage;
import java.io.File;
import java.io.IOException;

import javax.imageio.ImageIO;
import javax.swing.DefaultListModel;
import javax.swing.ImageIcon;
import javax.swing.JButton;
import javax.swing.JFrame;
import javax.swing.JLabel;
import javax.swing.JList;
import javax.swing.JOptionPane;
import javax.swing.JSeparator;
import javax.swing.ListSelectionModel;
import javax.swing.SwingConstants;
import javax.swing.event.ListSelectionEvent;
import javax.swing.event.ListSelectionListener;

import Common.*;
import GameComms.*;

import java.lang.reflect.Method;
import java.net.Inet4Address;
import java.net.URL;

public class ServerFrame extends Thread {
	
	private JFrame frame;
	
	private JButton m_btnStartServer;
	private JButton m_btnStopServer;
	
	private JLabel m_localServerIP;
	private JLabel m_lblBasicHelpConnection;
	
	private JList m_listLocalClients;
	private DefaultListModel m_listLocalModelClients;

	private JList m_listLocalRooms;
	private DefaultListModel m_listLocalModelRooms;
	
	private String m_serverIP;
	private int m_serverPort;
	private Boolean m_enableLogMessages;
	
	public static void RunFrame(String _serverIP, int _port, Boolean _enableLogMessages) {
		EventQueue.invokeLater(new Runnable() {
			public void run() {
				try {
					ServerFrame window = new ServerFrame(_serverIP, _port, _enableLogMessages);
					window.frame.setVisible(true);
				} catch (Exception e) {
					e.printStackTrace();
				}
			}
		});
	}
	
	public ServerFrame(String _serverIP, int _port, Boolean _enableLogMessages) {
		m_serverIP = _serverIP;
		m_serverPort = _port;
		m_enableLogMessages = _enableLogMessages;
		initialize();
	}

	private void initialize() 
	{
		frame = new JFrame();
		frame.setBounds(100, 100, 604, 685);
		frame.setResizable(false);
		frame.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
		frame.getContentPane().setLayout(null);
		frame.setTitle("YourNetworkTools Server. Version " + Server.SERVER_VERSION);
				
		JLabel lblServerManager = new JLabel("Server YourNetworkTools");
		lblServerManager.setFont(new Font("Tahoma", Font.PLAIN, 17));
		lblServerManager.setBounds(22, 19, 264, 21);
		frame.getContentPane().add(lblServerManager);
		
		JLabel lblServerIp = new JLabel("Server IP: " + m_serverIP + ":" + m_serverPort);
		lblServerIp.setFont(new Font("Tahoma", Font.PLAIN, 16));
		lblServerIp.setBounds(22, 51, 585, 21);
		frame.getContentPane().add(lblServerIp);
		
		JLabel lblConnectedClients = new JLabel("Connected Clients");
		lblConnectedClients.setFont(new Font("Tahoma", Font.PLAIN, 14));
		lblConnectedClients.setBounds(30, 83, 136, 14);
		frame.getContentPane().add(lblConnectedClients);
		
		JSeparator separator = new JSeparator();
		separator.setBounds(22, 75, 531, 2);
		frame.getContentPane().add(separator);
		
		JSeparator separator_1 = new JSeparator();
		separator_1.setBounds(22, 43, 241, 2);
		frame.getContentPane().add(separator_1);
		
		JLabel lblFirstPressstart = new JLabel("1. First, press \"Start Server\" to run locally the server.");
		lblFirstPressstart.setVerticalAlignment(SwingConstants.TOP);
		lblFirstPressstart.setBounds(28, 542, 463, 14);
		frame.getContentPane().add(lblFirstPressstart);
		
		JLabel lblStartEach = new JLabel("2. Start each client of YourNetworkTools and set the local server IP to connect to this machine");
		lblStartEach.setBounds(28, 564, 543, 14);
		frame.getContentPane().add(lblStartEach);
		
		m_btnStartServer = new JButton("Start Server");
		m_btnStartServer.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent arg0) {
				StartServer();
			}
		});
		m_btnStartServer.setFont(new Font("Tahoma", Font.PLAIN, 18));
		m_btnStartServer.setBounds(71, 589, 215, 46);
		frame.getContentPane().add(m_btnStartServer);
		
		m_btnStopServer = new JButton("Stop Server");
		m_btnStopServer.addActionListener(new ActionListener() {
			public void actionPerformed(ActionEvent e) {
				StopServer();
			}
		});
		m_btnStopServer.setFont(new Font("Tahoma", Font.PLAIN, 18));
		m_btnStopServer.setBounds(313, 589, 215, 46);
		frame.getContentPane().add(m_btnStopServer);

		// LIST OF CLIENTS
		m_listLocalModelClients = new DefaultListModel();
		m_listLocalClients = new JList(m_listLocalModelClients);		
		m_listLocalClients.setBounds(28, 108, 534, 203);
		m_listLocalClients.setSelectionMode(ListSelectionModel.SINGLE_SELECTION);
		m_listLocalClients.setCellRenderer(new SlotDataConnectionCellRenderer());
		m_listLocalClients.addListSelectionListener(new ListSelectionListener()
		{
 		   public void valueChanged(ListSelectionEvent e) 
 		   {
 			   System.out.println("PLAYER SELECTED="+m_listLocalClients.getSelectedIndex()); 			   
 		   }
		});
		frame.getContentPane().add(m_listLocalClients);

		
		JLabel lblRooms = new JLabel("List of rooms");
		lblRooms.setBounds(28, 324, 543, 14);
		frame.getContentPane().add(lblRooms);

		// LIST OF ROOMS
		m_listLocalModelRooms = new DefaultListModel();
		m_listLocalRooms = new JList(m_listLocalModelRooms);		
		m_listLocalRooms.setBounds(28, 348, 534, 180);
		m_listLocalRooms.setSelectionMode(ListSelectionModel.SINGLE_SELECTION);
		m_listLocalRooms.setCellRenderer(new SlotDataConnectionCellRenderer());
		m_listLocalRooms.addListSelectionListener(new ListSelectionListener()
		{
 		   public void valueChanged(ListSelectionEvent e) 
 		   {
 			   System.out.println("ROOM SELECTED="+m_listLocalRooms.getSelectedIndex()); 			   
 		   }
		});
		frame.getContentPane().add(m_listLocalRooms);

		// REGISTER EVENTS
		BasicController.getInstance().AddMyEventListener(this, "OnBasicEvent");
		
		m_btnStopServer.setEnabled(false);
	}

	private void StartServer()
	{
		try {
			m_listLocalModelClients.removeAllElements();
			Server.StartServer();
			m_btnStartServer.setEnabled(false);
			m_btnStartServer.setText("SERVER RUNNING!!!");
			m_btnStopServer.setEnabled(true);
			m_localServerIP.setText(Inet4Address.getLocalHost().getHostAddress());
			JOptionPane.showMessageDialog(null, "The server has been started at IP: "+Inet4Address.getLocalHost().getHostAddress()+"\n and it's listening throught the port 6767 (allow it in your Firewall). \n You can run Your VR Experience to multiplayer party!!!", "INFO", JOptionPane.INFORMATION_MESSAGE);
		} catch (Exception e) {
		}
	}

	private void StopServer()
	{
		try {
			m_listLocalModelClients.removeAllElements();			
			Server.StopServer();
			m_btnStartServer.setEnabled(true);
			m_btnStartServer.setText("Start Server");
			m_btnStopServer.setEnabled(false);
			m_localServerIP.setText("");
			JOptionPane.showMessageDialog(null, "The server has been stopped. You have to start it again in order to multiplayer games!!!", "INFO", JOptionPane.INFORMATION_MESSAGE);
		} catch (Exception e1) {
			// TODO Auto-generated catch block
			e1.printStackTrace();
		}
	}
	
	private SlotDataConnection GetPlayerById(int _idUser)
	{
		for (int i = 0; i < m_listLocalModelClients.size(); i++)
		{
			SlotDataConnection sPlayer = (SlotDataConnection)m_listLocalModelClients.getElementAt(i);			
			if (sPlayer.GetUID() == _idUser)
			{
				return sPlayer;
			}
		}
		return null;
	}

	private boolean RemovePlayerById(int _idUser)
	{
		for (int i = 0; i < m_listLocalModelClients.size(); i++)
		{
			SlotDataConnection sPlayer = (SlotDataConnection)m_listLocalModelClients.getElementAt(i);			
			if (sPlayer.GetUID() == _idUser)
			{
				m_listLocalModelClients.removeElementAt(i);
				return true;
			}
		}
		return false;
	}
	
	private SlotDataConnection GetRoomByName(String _nameRoom)
	{
		for (int i = 0; i < m_listLocalModelRooms.size(); i++)
		{
			SlotDataConnection sRoom = (SlotDataConnection)m_listLocalModelRooms.getElementAt(i);			
			if (sRoom.GetNameItem() == _nameRoom)
			{
				return sRoom;
			}
		}
		return null;
	}

	private boolean RemoveRoomById(int _idRoom)
	{
		for (int i = 0; i < m_listLocalModelRooms.size(); i++)
		{
			SlotDataConnection sRoom = (SlotDataConnection)m_listLocalModelRooms.getElementAt(i);			
			if (sRoom.GetUID() == _idRoom)
			{
				m_listLocalModelRooms.removeElementAt(i);
				return true;
			}
		}
		return false;
	}
	
	public void OnBasicEvent(BasicEvent _event)
	{
		if (ServerGame.EnableLogMessages)
		{
			System.out.println("ServerFrame::OnBasicEvent::_event["+_event.GetType()+"]");
			for (int i = 0; i < _event.GetParams().length; i++)
			{
				System.out.println("\t\t PARAM["+i+"]="+_event.GetParams()[i]);	
			}
		}
		if (_event.GetType().equals(ServerClients.EVENT_CLIENT_TCP_ESTABLISH_NETWORK_ID))
		{
			int idNewUser = (int)_event.GetParams()[0];
			if (ServerGame.EnableLogMessages)
			{
				System.out.println("\t\t NEW PLAYER["+idNewUser+"] CONNECTED!!!");
			}
			String nameNewUser = (String)_event.GetParams()[1];
			SlotDataConnection newSlot = new SlotDataConnection(idNewUser, nameNewUser);
			m_listLocalModelClients.addElement(newSlot);
		}
		if (_event.GetType().equals(ServerClients.EVENT_CLIENT_TCP_ROOM_ID))
		{
			int idUser = (int)_event.GetParams()[0];
			String roomName = (String)_event.GetParams()[1];
			int numberPlayers = (int)_event.GetParams()[2];
			SlotDataConnection sRoomSlot = GetRoomByName(roomName);
			if (sRoomSlot == null)
			{
				if (ServerGame.EnableLogMessages)
				{
					System.out.println("\t\t NEW ROOM[" + roomName + "]");
				}
				sRoomSlot = new SlotDataConnection(idUser, roomName, numberPlayers);
				m_listLocalModelRooms.addElement(sRoomSlot);
			}
			sRoomSlot.SetConnectedPlayers(sRoomSlot.GetConnectedPlayers() + 1);
			if (ServerGame.EnableLogMessages)
			{
				System.out.println("\t\t USER["+idUser+"] CONNECTED TO ROOM[" + roomName + "]::PLAYERS CONNECTED["+sRoomSlot.GetConnectedPlayers()+"]");
			}
			RemovePlayerById(idUser);
			m_listLocalRooms.revalidate();
			m_listLocalClients.revalidate();
		}		
		if (_event.GetType().equals(ServerClients.EVENT_SERVERCLIENTS_DELETE_CLIENT))
		{
			int idUser = (int)_event.GetParams()[0];
			RemovePlayerById(idUser);
			if (ServerGame.EnableLogMessages)
			{
				System.out.println("\t\t ServerClients.EVENT_SERVERCLIENTS_DELETE_CLIENT["+idUser+"]");
			}
		}
		if (_event.GetType().equals(ServerRoom.EVENT_SERVERROOM_DESTROYED))
		{
			int idRoom = (int)_event.GetParams()[0];
			RemoveRoomById(idRoom);
			if (ServerGame.EnableLogMessages)
			{
				System.out.println("\t\t ROOM["+idRoom+"] DESTROYED");
			}
			m_listLocalRooms.revalidate();
			m_listLocalClients.revalidate();
		}
	}
		
	

}
