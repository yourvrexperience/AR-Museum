<?php
	
	include 'ConfigurationUserManagement.php';
 
	ConsultAllNarrations();

    // Closing connection
    mysqli_close($GLOBALS['LINK_DATABASE']);
          
     //**************************************************************************************
     //**************************************************************************************
     //**************************************************************************************
     // FUNCTIONS
     //**************************************************************************************
     //**************************************************************************************
     //**************************************************************************************     
	
	   //-------------------------------------------------------------
     //  ConsultAllNarrations
     //-------------------------------------------------------------
     function ConsultAllNarrations()
     {
        $query_consult = "SELECT positions FROM poimaps";
        $result_consult = mysqli_query($GLOBALS['LINK_DATABASE'], $query_consult) or die("Query Error::ConsultAllNarrations::Select POIs failed");

        $output_packet = "";
        while ($row_pois = mysqli_fetch_object($result_consult))
        {
          $positions = $row_pois->positions;
          
          $output_packet = $output_packet . $GLOBALS['LINE_SEPARATOR'] . $positions;
        }
        
        print $output_packet; 

        mysqli_free_result($result_consult);
    }	
	