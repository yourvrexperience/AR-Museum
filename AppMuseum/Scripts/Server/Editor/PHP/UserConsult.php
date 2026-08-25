<?php
	
	include 'ConfigurationUserManagement.php';
 
	$iduser = $_GET["id"];
	$password = $_GET["password"];
	$user = $_GET["user"];

    $email_db_user = ExistsUser($iduser, $password);
	if (strlen($email_db_user) > 0)
	{
		ConsultUser($user, true, true);
	}
	else
	{
		print "false";
	}

    // Closing connection
    mysqli_close($GLOBALS['LINK_DATABASE']);
	
?>
