<?php
	
	include 'ConfigurationUserManagement.php';

	$iduser = $_POST["id"];
	$password = $_POST["password"];
	$user_profile = $_POST["user"];
	$name_profile = $_POST["name"];
	$address_profile = $_POST["address"];
	$description_profile = $_POST["description"];
	$data_profile = $_POST["data"];
	$data_2 = $_POST["data2"];
	$data_3 = $_POST["data3"];
	$data_4 = $_POST["data4"];
	$data_5 = $_POST["data5"];

	$email_db_user = ExistsUser($iduser, $password);
	if (strlen($email_db_user) > 0)
	{	
		UpdateProfile($user_profile, $name_profile, $address_profile, $description_profile, $data_profile, $data_2, $data_3, $data_4, $data_5);
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
     function UpdateProfile($iduser_par, $name_par, $address_par, $description_par, $data_par, $data_par2, $data_par3, $data_par4, $data_par5)
     {
		// Performing SQL Consult
		$query_update_profile = "UPDATE profile SET name = '$name_par', address = '$address_par', description = '$description_par', data = '$data_par', data2 = '$data_par2', data3 = '$data_par3', data4 = '$data_par4', data5 = '$data_par5' WHERE user = $iduser_par";
		mysqli_query($GLOBALS['LINK_DATABASE'],$query_update_profile) or die("Query Error::UpdateProfile::Update profile failed");
				
		// RETURN THE DATA IN THE DATABASE
		ConsultUser($iduser_par, true, true);				
    }
	
?>
