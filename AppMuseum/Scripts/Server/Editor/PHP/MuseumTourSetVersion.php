<?php
	
	include 'ConfigurationUserManagement.php';
	
	$version = $_GET["version"];
	$secrets = $_GET["secrets"];

	SetVersion($version, $secrets);

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
     //  SetVersion
     //-------------------------------------------------------------
     function SetVersion($version_par, $secrets_par)
     {
		$query_update_version = "UPDATE version SET version_dev=$version_par, version_prod=$version_par, secrets_prod=$secrets_par, production=development WHERE id = 0";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_update_version) or die("Query Error::SetVersion::Update Version failed");

		// POIs UPDATE	
		$query_delete_backup_table = "DELETE FROM poimaps_backup";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_delete_backup_table) or die("Query Error::SetVersion::Delete POI Backup Table failed");

		$query_update_table = "INSERT INTO poimaps_backup SELECT * FROM poimaps";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_update_table) or die("Query Error::SetVersion::Fill POI Backup Table failed");
		
		$query_delete_table = "DELETE FROM poimaps";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_delete_table) or die("Query Error::SetVersion::Delete POI Production Table failed");

		$query_new_table = "INSERT INTO poimaps SELECT * FROM poimaps_edition";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_new_table) or die("Query Error::SetVersion::Fill POI Production Table failed");

		// Speechs UPDATE
		$query_delete_backup_speech = "DELETE FROM speech_backup";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_delete_backup_speech) or die("Query Error::SetVersion::Delete speech Backup Table failed");

		$query_update_speech = "INSERT INTO speech_backup SELECT * FROM speech";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_update_speech) or die("Query Error::SetVersion::Fill speech Backup Table failed");
		
		$query_delete_speech = "DELETE FROM speech";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_delete_speech) or die("Query Error::SetVersion::Delete speech Production Table failed");

		$query_new_speech = "INSERT INTO speech SELECT * FROM speech_edition";
		mysqli_query($GLOBALS['LINK_DATABASE'], $query_new_speech) or die("Query Error::SetVersion::Fill speech Production Table failed");
		
		print "true";
	 }	
	
?>
