<?php
	
	include 'ConfigurationUserManagement.php';

	$nameevent = $_POST["nameevent"];
	$email = $_POST["email"];
	$age = $_POST["age"];
	$language = $_POST["language"];
	$level = $_POST["level"];
	$dataevent = $_POST["data"];

    	InsertNewLog($nameevent, $email, $age, $language, $level, $dataevent);

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
     function InsertNewLog($nameevent_par, $email_par, $age_par, $language_par, $level_par, $dataevent_par)
     {
		$query_consult = "SELECT max(id) as maximumId FROM analytics";
		$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'],$query_consult) or die("Query Error::AnalyticsLogEvent::Select max analytics failed");
		$row_consult = mysqli_fetch_object($result_consult);
		$maxIdentifier = $row_consult->maximumId;
		mysqli_free_result($result_consult);
			
		$log_id_new = $maxIdentifier + 1;
		$current_time_registered = GetCurrentTimestamp();
	
		$dataevent_final = $dataevent_par;
		
		$query_insert = "INSERT INTO analytics VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
		$query_insert_event = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_insert);
		mysqli_stmt_bind_param($query_insert_event, 'issisiss', $log_id_new, $nameevent_par, $email_par, $age_par, $language_par, $level_par, $current_time_registered, $dataevent_final);
		if (!mysqli_stmt_execute($query_insert_event))
		{
			die("Query Error::InsertNewLog::Insert log event Failed");
		}
			
		print "true";
     }
	
?>
