package Common;

import javax.swing.*;
import javax.swing.event.*; 

import java.awt.*;
import java.util.*;
import java.io.*;
import java.awt.event.*;
import java.net.*;
import java.math.*;


public class SlotDataConnection
{
	public static final String STATE_PLAYER_CONNECTED 	= "CONNECTED";
	public static final String STATE_PLAYER_INGAME 		= "IN-GAME";
	
	private int m_uid;
	private String m_nameItem;
	private int m_totalPlayers = -1;
	private int m_connectedPlayers = 0;
	
	public int GetUID() { return m_uid; }
	public String GetNameItem() { return m_nameItem; }
	public int GetTotalPlayers() { return m_totalPlayers; }
	public int GetConnectedPlayers() { return m_connectedPlayers; }
	public void SetConnectedPlayers(int _value) { m_connectedPlayers = _value; }
	
    public SlotDataConnection(int _uid, String _nameItem)
	{
    	m_uid = _uid;
    	m_nameItem = _nameItem; 
    	m_totalPlayers = -1;
	}

    public SlotDataConnection(int _uid, String _nameItem, int _totalPlayers)
	{
    	m_uid = _uid;
    	m_nameItem = _nameItem; 
    	m_totalPlayers = _totalPlayers;
	}
    
}
