<?php
	
	include 'ConfigurationUserManagement.php';

	$secret = $_POST["secret"];
	$age = $_POST["age"];
	$floor = $_POST["floor"];	
	$operation = $_POST["operation"];
	
	ReorderSecrets(($operation == 1), $secret, $age, $floor);

    // Closing connection
    mysqli_close($GLOBALS['LINK_DATABASE']);

	 //-------------------------------------------------------------
	 //  ReorderSecrets
	 //-------------------------------------------------------------
	 function ReorderSecrets($order_up_par, $secret_par, $age_par, $floor_par)
	 {
		 $query_string = "";
		 if ($order_up_par)
		 {
			$query_string = "UPDATE speech_edition SET secret = secret + 1 WHERE age = ? AND floor = ? AND secret > ?";
		 }
		 else
		 {
			$query_string = "UPDATE speech_edition SET secret = secret - 1 WHERE age = ? AND floor = ? AND secret >= ?";
		 }
		
		$query_reorder_speeches = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
		mysqli_stmt_bind_param($query_reorder_speeches, 'iii', $age_par, $floor_par, $secret_par);
		if (!mysqli_stmt_execute($query_reorder_speeches))
		{
			die("Query Error::ReorderSecrets::Reorder speech Failed");
		}

		print "true";
		mysqli_stmt_close($query_reorder_speeches);
	 }

?>
