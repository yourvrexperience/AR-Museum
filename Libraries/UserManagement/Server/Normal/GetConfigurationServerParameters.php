<?php

	include 'ConfigurationUserManagement.php';

	$message = "";
	$message = $message . "true" . $GLOBALS['PARAM_SEPARATOR'];
	$message = $message . $GLOBALS['NON_REPLY_EMAIL_ADDRESS']. $GLOBALS['PARAM_SEPARATOR'];	
	$message = $message . $GLOBALS['SERVER_SERVICE_ACTIVATED'];
	
	print  $message;	
?>