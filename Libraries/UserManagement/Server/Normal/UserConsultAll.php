<?php
	
	include 'ConfigurationUserManagement.php';
 
	$iduser = $_GET["id"];
	$password = $_GET["password"];
	$profile = $_GET["profile"] == "true";

    $email_db_user = ExistsUser($iduser, $password);
	if (strlen($email_db_user) > 0)
	{
		ConsultAllUsers($profile);
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
     //  ConsultAllUsers
     //-------------------------------------------------------------
     function ConsultAllUsers($profile_par)
     {
		$query_consult = "SELECT * FROM users";
		$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'],$query_consult) or die("Query Error::UserConsult::Select users failed");

		$output_packet = "";
		while ($row_user = mysqli_fetch_object($result_consult))
		{
			$id_user = $row_user->id;
			$email_user = $row_user->email;
			$name_user = $row_user->nickname;
			$registerdate_user = $row_user->registerdate;
			$lastlogin_user = $row_user->lastlogin;
			$admin_user = $row_user->admin;
			$level_user = $row_user->level;
			$code_user = $row_user->code;
			$validated_user = $row_user->validated;
			$platform_user = $row_user->platform;

			$line_user = $id_user . $GLOBALS['PARAM_SEPARATOR'] . $email_user . $GLOBALS['PARAM_SEPARATOR'] . $name_user . $GLOBALS['PARAM_SEPARATOR'] . $registerdate_user . $GLOBALS['PARAM_SEPARATOR'] . $lastlogin_user . $GLOBALS['PARAM_SEPARATOR'] . $admin_user . $GLOBALS['PARAM_SEPARATOR'] . $level_user . $GLOBALS['PARAM_SEPARATOR'] . $validated_user . $GLOBALS['PARAM_SEPARATOR'] . $platform_user;
			if ($profile_par) $line_user = $line_user . GetUserProfileData($id_user);
			
			$output_packet = $output_packet . $GLOBALS['LINE_SEPARATOR'] . $line_user;
		}
		
		print  "true" . $GLOBALS['BLOCK_SEPARATOR'] . $output_packet;

		
		mysqli_free_result($result_consult);
    }
	
?>
