<?php
	
	include 'ConfigurationUserManagement.php';
 
	ConsultAllSecrets();

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
     //  ConsultAllSecrets
     //-------------------------------------------------------------
     function ConsultAllSecrets()
     {
        $query_consult = "SELECT secrets FROM poimaps";
        $result_consult = mysqli_query($GLOBALS['LINK_DATABASE'], $query_consult) or die("Query Error::ConsultAllSecrets::Select secrets failed");

        $output_packet = "";
        while ($row_secrets = mysqli_fetch_object($result_consult))
        {
          $secrets = $row_secrets->secrets;
          
          $output_packet = $output_packet . $GLOBALS['LINE_SEPARATOR'] . $secrets;
        }
        
        print $output_packet; 

        mysqli_free_result($result_consult);
    }	
	