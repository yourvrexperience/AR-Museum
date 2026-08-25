<?php
	
	include 'ConfigurationUserManagement.php';

	$iduser = $_POST["iduser"];
	$password = $_POST["passworduser"];
	
	$all = $_POST["all"];
	$secret = $_POST["secret"];	$age = $_POST["age"];
	$floor = $_POST["floor"];
	$poi = $_POST["poi"];
	$segment = $_POST["segment"];

	$email_db_user = ExistsAdmin($iduser, $password);
	if (strlen($email_db_user) > 0)
	{
		if ($all == 1)
		{
			DeleteAllSpeeches($secret, $age, $floor);
		}
		else
		{
			DeleteSpeech($secret, $age, $floor, $poi, $segment);
		}
	}
	else
	{
		print "false";
	}

    // Closing connection
    mysqli_close($GLOBALS['LINK_DATABASE']);

	 //-------------------------------------------------------------
	 //  DeleteSpeech
	 //-------------------------------------------------------------
	 function DeleteSpeech($secret_par, $age_par, $floor_par, $poi_par, $segment_par)
	 {
		if ($segment_par == -1)
		{
			if ($poi_par == -1)
			{
				$query_string = "DELETE FROM speech_edition WHERE secret = ? AND age = ? AND floor = ?";
				$query_delete_speech = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
				mysqli_stmt_bind_param($query_delete_speech, 'iii', $secret_par, $age_par, $floor_par);
			}
			else
			{
				$query_string = "DELETE FROM speech_edition WHERE secret = ? AND age = ? AND floor = ? AND poi = ?";
				$query_delete_speech = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
				mysqli_stmt_bind_param($query_delete_speech, 'iiii', $secret_par, $age_par, $floor_par, $poi_par);
			}
		}
		else
		{
			$query_string = "DELETE FROM speech_edition WHERE secret = ? AND age = ? AND floor = ? AND poi = ? AND segment >= ?";
			$query_delete_speech = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
			mysqli_stmt_bind_param($query_delete_speech, 'iiiii', $secret_par, $age_par, $floor_par, $poi_par, $segment_par);
		}
		
		if (!mysqli_stmt_execute($query_delete_speech))
		{
			print "false";
		}
		else
		{
			print "true";
		}
		
		mysqli_stmt_close($query_delete_speech);			
	 }
	 
	 //-------------------------------------------------------------
	 //  DeleteAllSpeeches
	 //-------------------------------------------------------------
	 function DeleteAllSpeeches($secret_par, $age_par, $floor_par)
	 {
		if ($secret_par == -1)
		{
			$query_string = "DELETE FROM speech_edition WHERE secret = -1 AND age = ? AND floor = ?";
			$query_delete_speech = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
			mysqli_stmt_bind_param($query_delete_speech, 'ii', $age_par, $floor_par);
		}
		else
		{
			$query_string = "DELETE FROM speech_edition WHERE secret <> -1 AND age = ? AND floor = ?";
			$query_delete_speech = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
			mysqli_stmt_bind_param($query_delete_speech, 'ii', $age_par, $floor_par);
		}
		
		if (!mysqli_stmt_execute($query_delete_speech))
		{
			print "false";
		}
		else
		{
			print "true";
		}
		
		mysqli_stmt_close($query_delete_speech);			
	 }

?>
