<?php
	
	include 'ConfigurationUserManagement.php';
 
	$iduser = $_GET["id"];
	$password = $_GET["password"];
	$usertodelete = $_GET["delete"];

    $email_db_user = ExistsAdmin($iduser, $password);
	if ((strlen($email_db_user) > 0) || ($iduser == $usertodelete))
	{
		RemoveUser($usertodelete);
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
     //  RemoveProfile
     //-------------------------------------------------------------
     function RemoveProfile($user_par)
     {
		$query_delete = "DELETE FROM profile WHERE user = $user_par";
		mysqli_query($GLOBALS['LINK_DATABASE'],$query_delete) or die("Query Error::RemoveProfile::Remove profile");

		if (mysqli_affected_rows($GLOBALS['LINK_DATABASE']) == 1)
		{
			return true;
		}
		else
		{
			return false;
		}
	 }
	 
     //-------------------------------------------------------------
     //  RemoveUser
     //-------------------------------------------------------------
     function RemoveUser($user_par)
     {
		$query_delete = "DELETE FROM users WHERE id = $user_par";
		mysqli_query($GLOBALS['LINK_DATABASE'],$query_delete) or die("Query Error::RemoveUser::Remove user");

		if (mysqli_affected_rows($GLOBALS['LINK_DATABASE']) == 1)
		{
			RemoveProfile($user_par);
			
			print "true" . $GLOBALS['PARAM_SEPARATOR'] . $user_par;
		}
		else
		{
			print "false";
		}
    }
	
?>
