<?php
	
	include 'ConfigurationUserManagement.php';

	$language = $_GET["language"];
	$iduser = $_GET["id"];
	$codeuser = $_GET["code"];

	ValidateEmail($language, $iduser, $codeuser);
	
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
     //  LoginEmail
     //-------------------------------------------------------------
     function ValidateEmail($language_par, $iduser_par, $codeuser_par)
     {
		// Performing SQL Consult
		$query_user = "SELECT email FROM users WHERE id = $iduser_par AND code = '$codeuser_par'";
		$result_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_user) or die("Query Error::ValidateEmail::Select email failed");
				
		if ($row_user = mysqli_fetch_object($result_user))
		{
			// SET THE TIMESTAMP AND THE CODE TO RESET
			$query_update_user = "UPDATE users SET validated=1, code='' WHERE id = $iduser_par";
			$result_update_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_update_user) or die("Query Error::ValidateEmail::Update users failed");

			if ($result_update_user)
			{
				if ($language_par == "en")
				{
					print "<h2>Registration success!!</h2><h3><p>Welcome to ". $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . "!!!<p>You can start using the app<p>Thank You!!.</h3>";
				}
				if ($language_par == "es")
				{
					print "<h2>Registro Existoso!!</h2><h3><p>Bienvenido a ". $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . "!!!<p>Ya puedes utilizar la app<p>Muchas Gracias!!.</h3>";
				}
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
