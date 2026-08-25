<?php
	
	include 'ConfigurationUserManagement.php';
 
	$id = $_GET["id"];
	$age = $_GET["age"];
	$dev = $_GET["dev"];

	if (ExistsPOIs($id) == true)
	{
		ConsultPOIs($id, $dev);
	}
	else
	{
		print "false";
	}

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
     //  ConsultPOIs
     //-------------------------------------------------------------
     function ConsultPOIs($id_par, $dev_par)
     {
		$query_consult = "SELECT * FROM poimaps WHERE id = $id_par";
		if ($dev_par == 1)
		{
			$query_consult = "SELECT * FROM poimaps_edition WHERE id = $id_par";
		}
		$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'], $query_consult) or die("Query Error::UserConsult::Select POIs failed");

		if ($row_pois = mysqli_fetch_object($result_consult))
		{
			$positions = $row_pois->positions;
			$secrets = $row_pois->secrets;
			$narration = $row_pois->narration;

			print "true" . $GLOBALS['PARAM_SEPARATOR'] . $positions . $GLOBALS['PARAM_SEPARATOR'] . $secrets . $GLOBALS['PARAM_SEPARATOR'] . $narration;
		}
		else
		{
			print "false";
		}
		
		mysqli_free_result($result_consult);
    }	
	
?>
