package Main;

import java.awt.BorderLayout;
import java.io.BufferedReader;

import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.net.Socket;
import java.net.ServerSocket;
import java.util.Vector;

import javax.swing.JFrame;
import javax.swing.JLabel;

import GameComms.*;
import Common.*;

public class Server 
{
	public static final boolean ACTIVATE_FRAME_INTERFACE =  true;
	public static final String SERVER_VERSION =  "1.00";
	
	private static ServerGame m_serverGame;
	
	private static String m_ipAddress;
	private static int m_portAddress;
	private static boolean m_graphicInterface;
	private static boolean m_enableLogMessages;	
	
    public static void StartServer() throws Exception 
    {
    	if (m_serverGame != null) return;
    	
        m_serverGame = new ServerGame();
        m_serverGame.StartServerGame(m_ipAddress, m_portAddress, m_enableLogMessages);
        m_serverGame.start();
    }
    
    public static void StopServer() throws Exception 
    {
    	m_serverGame.StopServerGame();
    	m_serverGame = null;
    }
	
	
    public static void main(String[] args) throws Exception 
    {
		System.out.println("IP ADDRESS[0]="+args[0]);
		System.out.println("PORT[1]="+args[1]);
		System.out.println("GRAPHIC INTERFACE[2]="+args[2]);
		System.out.println("ENABLE LOG MESSAGE[3]="+args[3]);
		m_ipAddress = args[0];
		m_portAddress = Integer.parseInt(args[1]);
		m_graphicInterface = Boolean.parseBoolean(args[2]);
		m_enableLogMessages = Boolean.parseBoolean(args[3]);
				
    	if (m_graphicInterface)
    	{
    		ServerFrame.RunFrame(m_ipAddress, m_portAddress, m_enableLogMessages);
    	}
    	else
    	{
    		StartServer();
    	}
    }
}




