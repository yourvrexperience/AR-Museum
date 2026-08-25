<?php
	
	include 'ConfigurationUserManagement.php';
	
	$email = $_GET["email"];
	$password = $_GET["password"];

	LoginEmail($email, $password);

    // Closing connection
    mysqli_close($GLOBALS['LINK_DATABASE']);

?>
