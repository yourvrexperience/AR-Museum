<?php
	
	include 'ConfigurationUserManagement.php';
    
	$iduser = $_GET["id"];

	CheckValidation($iduser);

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
     //  CheckValidation
     //-------------------------------------------------------------
     function CheckValidation($iduser_par)
     {
		// Performing SQL Consult
		$query_user = "SELECT validated FROM users WHERE id = $iduser_par";
		$result_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_user) or die("Query Error::UsersCheckValidation::Select users failed");
				
		if ($row_user = mysqli_fetch_object($result_user))
		{
			$validated_user = $row_user->validated;
			
			if ($validated_user == 1)
			{
				print "true";
			}
			else
			{
				print "false";
			}
		}
		else
		{
			print "false";
		}
	 
		// Free resultset
		mysqli_free_result($result_user);
    }
	
?>
