<?php
	
	include 'ConfigurationUserManagement.php';
 
	$language = $_GET["language"];

	ConsultLanguage($language);

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
     //  ConsultLanguage
     //-------------------------------------------------------------
     function ConsultLanguage($language_par)
     {
		$query_consult = "SELECT * FROM ai_data WHERE code = '$language_par'";
		$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'], $query_consult) or die("Query Error::AI Instruction::Select AI instructions failed");

		if ($row_ai = mysqli_fetch_object($result_consult))
		{
			$instructions = $row_ai->instructions;

			print "true" . $GLOBALS['PARAM_SEPARATOR'] . $instructions;
		}
		else
		{
			print "false";
		}
		
		mysqli_free_result($result_consult);
    }	
	
?>
