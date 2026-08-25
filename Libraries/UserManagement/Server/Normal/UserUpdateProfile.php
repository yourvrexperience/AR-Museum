<?php
	
	include 'ConfigurationUserManagement.php';

	$iduser = $_POST["id"];
	$password = $_POST["password"];
	$user_profile = $_POST["user"];
	$name_profile = $_POST["name"];
	$address_profile = $_POST["address"];
	$description_profile = $_POST["description"];
	$data_profile = $_POST["data"];

	$email_db_user = ExistsUser($iduser, $password);
	if (strlen($email_db_user) > 0)
	{	
		UpdateProfile($user_profile, $name_profile, $address_profile, $description_profile, $data_profile);
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
     //  UpdateProfile
     //-------------------------------------------------------------
     function UpdateProfile($iduser_par, $name_par, $address_par, $description_par, $data_par)
     {
		// Performing SQL Consult
		$query_update_profile = "UPDATE profile SET name = '$name_par', address = '$address_par', description = '$description_par', data = '$data_par' WHERE user = $iduser_par";
		mysqli_query($GLOBALS['LINK_DATABASE'],$query_update_profile) or die("Query Error::UpdateProfile::Update profile failed");
				
		// RETURN THE DATA IN THE DATABASE
		ConsultUser($iduser_par, true, true);				
    }
	
?>
