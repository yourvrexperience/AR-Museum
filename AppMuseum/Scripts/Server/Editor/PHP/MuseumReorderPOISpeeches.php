<?php
	
	include 'ConfigurationUserManagement.php';

	$age = $_POST["age"];
	$floor = $_POST["floor"];
	$poi = $_POST["poi"];
	$operation = $_POST["operation"];
	
	ReorderPois(($operation == 1), $age, $floor, $poi);

    // Closing connection
    mysqli_close($GLOBALS['LINK_DATABASE']);

	 //-------------------------------------------------------------
	 //  ReorderPois
	 //-------------------------------------------------------------
	 function ReorderPois($order_up_par, $age_par, $floor_par, $poi_par)
	 {
		 $query_string = "";
		 if ($order_up_par)
		 {
			$query_string = "UPDATE speech_edition SET poi = poi + 1 WHERE secret = -1 AND age = ? AND floor = ? AND poi > ?";
		 }
		 else
		 {
			$query_string = "UPDATE speech_edition SET poi = poi - 1 WHERE secret = -1 AND age = ? AND floor = ? AND poi >= ?";
		 }
		
		$query_reorder_speeches = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
		mysqli_stmt_bind_param($query_reorder_speeches, 'iii', $age_par, $floor_par, $poi_par);
		if (!mysqli_stmt_execute($query_reorder_speeches))
		{
			die("Query Error::UploadSpeechData::Reorder speech Failed");
		}

		print "true";
		mysqli_stmt_close($query_reorder_speeches);
	 }

?>
