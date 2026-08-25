<?php
	
	include 'ConfigurationUserManagement.php';
 
	$secret = $_GET["secret"];
	$age = $_GET["age"];
	$floor = $_GET["floor"];
	$poi = $_GET["poi"];
	$segment = $_GET["segment"];
	$language = $_GET["language"];
	$direct = $_GET["direct"];
	$dev = $_GET["dev"];
	
	DownloadSpeechData($secret, $age, $floor, $poi, $segment, $language, $direct == 1, $dev == 1);

    // Closing connection
    mysqli_close($GLOBALS['LINK_DATABASE']);
          
     //**************************************************************************************
     //**************************************************************************************
     //**************************************************************************************
     // FUNCTIONS
     //**************************************************************************************
     //**************************************************************************************
     //**************************************************************************************     
	
	function removeSpecialCharacters($input) {
		// This will keep only letters and numbers
		return preg_replace('/[^a-zA-Z0-9]/', '', $input);
	}
	
	 //-------------------------------------------------------------
     //  DownloadSpeechData
     //-------------------------------------------------------------
     function DownloadSpeechData($secret_par, $age_par, $floor_par, $poi_par, $segment_par, $language_par, $direct_par, $dev_par)
     {
		$query_consult = "";
		if ($dev_par)
		{
			$query_consult = "SELECT * FROM speech_edition WHERE age = $age_par AND floor = $floor_par AND poi = $poi_par AND segment = $segment_par AND language = '$language_par' AND secret = $secret_par";
		}
		else
		{
			$query_consult = "SELECT * FROM speech WHERE age = $age_par AND floor = $floor_par AND poi = $poi_par AND segment = $segment_par AND language = '$language_par' AND secret = $secret_par";
		}
		$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'], $query_consult) or die("Query Error::DownloadSpeechData::Select speech data failed");

		if ($row_data = mysqli_fetch_object($result_consult))
		{
			$data = $row_data->data;
			$size = strlen($data);

			if ($direct_par)
			{
				$name = "age". $age_par . "_floor" . $floor_par . "_poi" . $poi_par . "_segment" . $segment_par . "_language" . $language_par . ".ogg";
				
				// Set headers to force download
				header('Content-Description: File Transfer');
				header('Content-Type: application/octet-stream');
				header('Content-Disposition: attachment; filename="' . basename($name) . '"');
				header('Content-Length: ' . $size);
				header('Cache-Control: must-revalidate');
				header('Pragma: public');
				header('Expires: 0');

				// Print the binary data
				echo $data;
				exit;				
			}
			else
			{
				header('Content-Type: application/octet-stream');
				header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');
				header('Pragma: no-cache');
				header('Expires: 0');
				header('Content-Length: ' . $size);
				echo $data;
				exit;          // <-- important, see the binary note below
			}			
		}
		else
		{
			print "";
		}
		
		mysqli_free_result($result_consult);
    }	
	
?>
