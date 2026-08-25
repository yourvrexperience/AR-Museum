<?php
	include 'ConfigurationUserManagement.php';

	$emailAddress = $_POST["email"];
	$formTimestamp = $_POST["registered"];
	
	$data_size = $_POST["size"];
	$data_temporal = $_FILES["data"];
	$data_file = file_get_contents($data_temporal['tmp_name']);
	
	InsertForm($GLOBALS['LINK_DATABASE'], $emailAddress, $formTimestamp, $data_size, $data_file);

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
     //  InsertForm
     //-------------------------------------------------------------
     function InsertForm($link_par, $emailAddress_par, $formTimestamp_par, $data_size_par, $data_file_par)
     {
		// ++ GET MAX ID ++
		$query_consult = "SELECT max(id) as maximumId FROM forms";
		$result_consult = mysqli_query($link_par, $query_consult) or die("Query Error::InsertForm::Get max id");
		$row_consult = mysqli_fetch_object($result_consult);
		$maxIdentifier = $row_consult->maximumId;
		$maxIdentifier = $maxIdentifier + 1;
		mysqli_free_result($result_consult);

		$timestamp_local_calculated = time();
		$query_insert = "INSERT INTO forms VALUES ($maxIdentifier, '$emailAddress_par', $formTimestamp_par, $data_size_par,'".mysqli_real_escape_string($link_par, $data_file_par)."')";
		$result_insert = mysqli_query($link_par, $query_insert) or die("Query Error::InsertForm::Failed to insert new form");
		
		// Finalizacion de registro
		if (mysqli_affected_rows($link_par) == 1)
		{
			print "true" . $GLOBALS['PARAM_SEPARATOR'] . $maxIdentifier;
		}
		else
		{
			print "false";
		}
     }
?>
