package Common;

import java.awt.BorderLayout;
import java.awt.Color;
import java.awt.Component;

import javax.swing.JLabel;
import javax.swing.JList;
import javax.swing.JPanel;
import javax.swing.ListCellRenderer;

public class SlotDataConnectionCellRenderer extends JPanel implements ListCellRenderer {

	private JLabel labelItem = new JLabel();
	     
    public SlotDataConnectionCellRenderer() {
        setLayout(new BorderLayout());
         
        labelItem.setOpaque(true);
        labelItem.setHorizontalAlignment(JLabel.LEFT);
         
        add(labelItem);
    }

    @Override
    public Component getListCellRendererComponent(JList list, Object value,
            int index, boolean isSelected, boolean cellHasFocus) {
    	SlotDataConnection object = (SlotDataConnection) value;
		
    	if (object.GetTotalPlayers() == -1)
        {
    		labelItem.setText(object.GetNameItem() + " is CONNECTED");
        }
    	else
    	{
    		if (object.GetConnectedPlayers() < object.GetTotalPlayers())
    		{
    			labelItem.setText(object.GetNameItem() + " is WAITING FOR PLAYERS ("+ object.GetConnectedPlayers() +"/" + object.GetTotalPlayers()  + ")");
    		}
    		else
    		{
    			labelItem.setText("****" + object.GetNameItem() + " IS RUNNING (" + object.GetTotalPlayers() + ")");
    		}    			
    	}
    	
        labelItem.setForeground(Color.BLACK);
        labelItem.setBackground(Color.LIGHT_GRAY);
         
        return this;
    }
}
