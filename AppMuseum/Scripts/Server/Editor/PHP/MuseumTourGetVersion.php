<?php
	
    include 'ConfigurationUserManagement.php';
 
    ConsultVersion();

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
     //  ConsultVersion
     //-------------------------------------------------------------
     function ConsultVersion()
     {
        $query_consult = "SELECT * FROM version WHERE id = 0";
        $result_consult = mysqli_query($GLOBALS['LINK_DATABASE'], $query_consult) or die("Query Error::ConsultVersion::Select Version failed");

        if ($row_version = mysqli_fetch_object($result_consult))
        {
          print "true" . $GLOBALS['PARAM_SEPARATOR'] . $row_version->version_dev  . $GLOBALS['PARAM_SEPARATOR'] . $row_version->version_prod . $GLOBALS['PARAM_SEPARATOR'] . $row_version->levels . $GLOBALS['PARAM_SEPARATOR'] . $row_version->secrets_dev  . $GLOBALS['PARAM_SEPARATOR'] . $row_version->secrets_prod . $GLOBALS['PARAM_SEPARATOR'] . $row_version->development . $GLOBALS['PARAM_SEPARATOR'] . $row_version->production;
        }
        else
        {
          print "false";
        }
        
        mysqli_free_result($result_consult);
    }	
	
?>
