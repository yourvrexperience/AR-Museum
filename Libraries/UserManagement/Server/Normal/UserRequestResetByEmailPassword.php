<?php


	include 'ConfigurationUserManagement.php';
    
	$language = $_GET["language"];
	$emailuser = $_GET["email"];

	RequestResetByEmailPassword($language, $emailuser);

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
     //  MailEncryptedPassword: Send a code valid for 1 hour
     //-------------------------------------------------------------
	 function MailPassword($language_par, $email_real_par, $iduser_par, $code_par)
	 {
		 if ($language_par == "en")
		 {
			 // EMAIL LEVEL
			 $to      = $email_real_par;
			 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Reset password';
			 $message = $message . "<html><body>";
			 $message = $message . "<p>";
			 $message = 'Follow the link to reset your password'. "<p>"; 
			 $message = $message . "<p>";
			 $url_link = $GLOBALS['URL_BASE_SERVER'] .'FormPasswordReset.php?language=' . $language_par . '&id=' . $iduser_par . '&code=' . $code_par; 
			 $a_url_link = "<a href=\"" . $url_link ."\">". $url_link . "</a>";
			 $message = $message . 'Link to reset the password: '. $a_url_link;
			 $message = $message . "<p>";
			 $message = $message . "<p>";
			 $message = $message . 'Best';
			 $message = $message . "</body></html>";
			 $headers = 'From: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . '\r\n' .
			 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . '\r\n' .
			 'MIME-Version: 1.0\r\n' . '\r\n' .
			 'Content-Type: text/html; charset=ISO-8859-1\r\n';
			 
			 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
		 }
		 if ($language_par == "es")
		 {
			 $to      = $email_real_par;
			 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Resetear contraseña';
			 $message = $message . "<html><body>";
			 $message = $message . "<p>";
			 $message = 'Sigue el enlace para resetear tu contraseña'. "<p>"; 
			 $message = $message . "<p>";
			 $url_link = $GLOBALS['URL_BASE_SERVER'] .'FormPasswordReset.php?language=' . $language_par . '&id=' . $iduser_par . '&code=' . $code_par; 
			 $a_url_link = "<a href=\"" . $url_link ."\">". $url_link . "</a>";
			 $message = $message . 'Clickar en este enlace: '. $a_url_link;
			 $message = $message . "<p>";
			 $message = $message . "<p>";
			 $message = $message . 'Gracias';
			 $message = $message . "</body></html>";
			 $headers = 'From: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . '\r\n' .
			 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . '\r\n' .
			 'MIME-Version: 1.0\r\n' . '\r\n' .
			 'Content-Type: text/html; charset=ISO-8859-1\r\n';
			 
			 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
		 }		 
	 }		 

     //-------------------------------------------------------------
     //  RequestResetByEmailPassword
     //-------------------------------------------------------------
     function RequestResetByEmailPassword($language_par, $emailuser_par)
     {
		// Performing SQL Consult
		$query_user = "SELECT id FROM users WHERE email = '$emailuser_par'";
		$result_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_user) or die("Query Error::RequestResetByEmailPassword::Select users failed");
				
		if ($row_user = mysqli_fetch_object($result_user))
		{
			$id_user = $row_user->id;
			$random_code_reset = rand_string(6);
			
			// SET THE TIMESTAMP AND THE CODE TO RESET
			$query_update_user = "UPDATE users SET code='$random_code_reset' WHERE id = $id_user";
			$result_update_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_update_user) or die("Query Error::RequestResetByEmailPassword::Update users failed");

			if ($result_update_user)
			{
				MailPassword($language_par, $emailuser_par, $id_user, $random_code_reset);
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
