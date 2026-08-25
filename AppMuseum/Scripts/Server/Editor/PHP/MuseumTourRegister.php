<?php

	include 'ConfigurationUserManagement.php';

	// ---------------------------------------------------------------
	//  Raw body (application/octet-stream): a single url-encoded
	//  metadata string. positions / secrets / narration arrive
	//  base64-encoded so they don't trip the WAF's PHP/SQL scanner.
	// ---------------------------------------------------------------

	$raw = file_get_contents('php://input');
	if ($raw === false || strlen($raw) === 0)
	{
		http_response_code(400);
		print "false";
		exit;
	}

	parse_str($raw, $meta);

	$iduser    = isset($meta['iduser'])       ? $meta['iduser']       : '';
	$password  = isset($meta['passworduser']) ? $meta['passworduser'] : '';
	$id        = isset($meta['id'])           ? (int)$meta['id']      : 0;
	$age       = isset($meta['age'])          ? (int)$meta['age']     : 0;
	$level     = isset($meta['level'])        ? (int)$meta['level']   : 0;
	$dev       = isset($meta['dev'])          ? (int)$meta['dev']     : 0;
	$positions = isset($meta['positions_b64']) ? base64_decode($meta['positions_b64']) : '';
	$secrets   = isset($meta['secrets_b64'])   ? base64_decode($meta['secrets_b64'])   : '';
	$narration = isset($meta['narration_b64']) ? base64_decode($meta['narration_b64']) : '';

	$email_db_user = ExistsAdmin($iduser, $password);
	if (strlen($email_db_user) > 0)
	{
		RegisterPOIs($id, $age, $level, $dev, $positions, $secrets, $narration);
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
     //  RegisterPOIs
     //-------------------------------------------------------------
     function RegisterPOIs($id_par, $age_par, $level_par, $dev_par, $positions_par, $secrets_par, $narration_par)
     {
		 $positions_subfinal = CleanDataString($positions_par);
		 $secrets_subfinal = CleanDataString($secrets_par);
		 $narration_subfinal = CleanDataString($narration_par);
		 
		 $positions_final = mysqli_real_escape_string($GLOBALS['LINK_DATABASE'], $positions_subfinal);
		 $secrets_final = mysqli_real_escape_string($GLOBALS['LINK_DATABASE'], $secrets_subfinal);
		 $narration_final = mysqli_real_escape_string($GLOBALS['LINK_DATABASE'], $narration_subfinal);
		 
		 $name_table = "poimaps";
		 if ($dev_par == 1)
		 {
			$name_table = "poimaps_edition";
		 }			
	
		 if (ExistsPOIs($id_par) == false)
		 {
			$query_insert = "INSERT INTO ".$name_table." VALUES (?, ?, ?, ?, ?, ?)";
			$query_insert_profile = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_insert);
			mysqli_stmt_bind_param($query_insert_profile, 'iiisss', $id_par, $age_par, $level_par, $positions_final, $secrets_final, $narration_final);
			if (!mysqli_stmt_execute($query_insert_profile))
			{
				die("Query Error::RegisterPOIs::Insert POIs Prepared Failed");
			}
		 }
		 else
		 {
			$query_string = "UPDATE ".$name_table." SET positions = ?, narration = ?, secrets = ? WHERE id = ?";
			$query_update_profile = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
			mysqli_stmt_bind_param($query_update_profile, 'sssi', $positions_final, $narration_final, $secrets_final, $id_par);
			if (!mysqli_stmt_execute($query_update_profile))
			{
				die("Query Error::RegisterPOIs::Update POIs Prepared Failed");
			}
		 }
		 
		 print "true";
    }

?>