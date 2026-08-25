<?php
	
	include 'ConfigurationUserManagement.php';

	$nameevent = $_POST["nameevent"];
	$dataevent = $_POST["data"];

	InsertNewLog($nameevent, $dataevent);

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
     //  InsertNewLog
     //-------------------------------------------------------------
     function InsertNewLog($nameevent_par, $dataevent_par)
     {
		$query_consult = "SELECT max(id) as maximumId FROM analytics";
		$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'],$query_consult) or die("Query Error::AnalyticsLogEvent::Select max analytics failed");
		$row_consult = mysqli_fetch_object($result_consult);
		$maxIdentifier = $row_consult->maximumId;
		mysqli_free_result($result_consult);
			
		$log_id_new = $maxIdentifier + 1;
		$current_time_registered = GetCurrentTimestamp();
	
		$query_insert = "INSERT INTO analytics VALUES ($log_id_new, '$nameevent_par', $current_time_registered, '$dataevent_par')";	
		$result_insert = mysqli_query($GLOBALS['LINK_DATABASE'],$query_insert) or die("Query Error::AnalyticsLogEvent::Insert log failed");
			
		if (mysqli_affected_rows($GLOBALS['LINK_DATABASE']) == 1)
		{
			print "true";
		}
		else			
		{
			print "false";
		}			
     }
	
?>
