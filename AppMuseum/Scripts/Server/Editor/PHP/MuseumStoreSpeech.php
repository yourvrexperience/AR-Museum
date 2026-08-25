<?php

	include 'ConfigurationUserManagement.php';

	// ---------------------------------------------------------------
	//  Raw body format (application/octet-stream):
	//    [4 bytes: little-endian length of metadata]
	//    [metadata: url-encoded key=value string, "text" is base64]
	//    [remaining bytes: raw audio, NOT base64, NOT inflated]
	// ---------------------------------------------------------------

	$raw = file_get_contents('php://input');

	if ($raw === false || strlen($raw) < 4)
	{
		http_response_code(400);
		print "false";
		exit;
	}

	// First 4 bytes = metadata length (unsigned 32-bit little-endian -> 'V')
	$metaLen = unpack('V', substr($raw, 0, 4))[1];

	if ($metaLen <= 0 || $metaLen > strlen($raw) - 4)
	{
		http_response_code(400);
		print "false";
		exit;
	}

	$metaStr    = substr($raw, 4, $metaLen);
	$data_sound = substr($raw, 4 + $metaLen);   // raw audio bytes
	$data_size  = strlen($data_sound);          // size in bytes

	parse_str($metaStr, $meta);

	$iduser     = isset($meta['iduser'])       ? $meta['iduser']                  : '';
	$password   = isset($meta['passworduser']) ? $meta['passworduser']            : '';
	$secret     = isset($meta['secret'])       ? (int)$meta['secret']             : 0;
	$age        = isset($meta['age'])          ? (int)$meta['age']                : 0;
	$floor      = isset($meta['floor'])        ? (int)$meta['floor']              : 0;
	$poi        = isset($meta['poi'])          ? (int)$meta['poi']                : 0;
	$segment    = isset($meta['segment'])      ? (int)$meta['segment']            : 0;
	$language   = isset($meta['language'])     ? $meta['language']                : '';
	$textspeech = isset($meta['text_b64'])     ? base64_decode($meta['text_b64']) : '';

	$email_db_user = ExistsAdmin($iduser, $password);
	if (strlen($email_db_user) > 0)
	{
		UploadSpeechData($secret, $age, $floor, $poi, $segment, $language, $textspeech, $data_sound, $data_size);
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
 	 //  ExistsSpeechIndex
     //-------------------------------------------------------------
	 function ExistsSpeechIndex($secret_par, $age_par, $floor_par, $poi_par, $segment_par, $language_par)
     {
		// Performing SQL Consult
		$query_story = "SELECT * FROM speech_edition WHERE age = $age_par AND floor = $floor_par AND poi = $poi_par AND segment = $segment_par AND language = '$language_par' AND secret = $secret_par";
		$result_story = mysqli_query($GLOBALS['LINK_DATABASE'],$query_story) or die("Query Error::MuseumStoreSpeech::ExistsSpeechIndex");
		
		if ($row_story = mysqli_fetch_object($result_story))
		{
			return $row_story->id;
		}
		else
		{
			return -1;			
		}
	 }

     //-------------------------------------------------------------
     //  UploadSpeechData
     //-------------------------------------------------------------
     function UploadSpeechData($secret_par, $age_par, $floor_par, $poi_par, $segment_par, $language_par, $text_par, $data_par, $data_size_par)
     {
		 $id_output = ExistsSpeechIndex($secret_par, $age_par,  $floor_par, $poi_par, $segment_par, $language_par);
		 
		 if ($id_output == -1)
		 {
			// New Data ID
			$query_maxdata = "SELECT max(id) as maximumId FROM speech_edition";
			$result_maxdata = mysqli_query($GLOBALS['LINK_DATABASE'],$query_maxdata) or die("Query Error::UploadSpeechData::Select max speech id failed");
			$row_maxdata = mysqli_fetch_object($result_maxdata);
			$dataid_output =  $row_maxdata->maximumId;
			if ($dataid_output == null) $dataid_output = 0;
			$dataid_output = $dataid_output + 1;
			$id_output = $dataid_output;
			mysqli_free_result($result_maxdata);

			$query_insert = "INSERT INTO speech_edition VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
			$query_insert_speech = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_insert);
			mysqli_stmt_bind_param($query_insert_speech, 'iisiiiisis', $dataid_output, $secret_par, $text_par, $age_par, $floor_par, $poi_par, $segment_par, $language_par, $data_size_par, $data_par);
			if (!mysqli_stmt_execute($query_insert_speech))
			{
				die("Query Error::UploadSpeechData::Insert speech Failed::$dataid_output, $text_par, $floor_par, $poi_par, $segment_par, $language_par");
			}
		 }
		 else
		 {
			// Consult Data ID
			$query_string = "UPDATE speech_edition SET size = ?, data = ?, text = ? WHERE id = ? AND secret = ?";
			$query_update_speech = mysqli_prepare($GLOBALS['LINK_DATABASE'], $query_string);
			mysqli_stmt_bind_param($query_update_speech, 'issii', $data_size_par, $data_par, $text_par, $id_output, $secret_par);
			if (!mysqli_stmt_execute($query_update_speech))
			{
				die("Query Error::UploadSpeechData::Update speech Failed");
			}
		 }
		 
		 print "true" . $GLOBALS['PARAM_SEPARATOR'] . $id_output;
    }

?>