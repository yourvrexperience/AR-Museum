<?php
	
	include 'ConfigurationUserManagement.php';
    
	$language = $_GET["language"];
	$iduser = $_GET["id"];

	RequestResetPassword($language, $iduser);

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
			 $message = $message . "<p>";
			 $message = 'Follow the link to reset your password. Beware that the link would be only working for 1 hour.'. "<p>"; 
			 $message = $message . "<p>";
			 $url_link = $GLOBALS['URL_BASE_SERVER'] .'FormPasswordReset.php?language=' . $language_par . '&id=' . $iduser_par . '&code=' . $code_par; 
			 $a_url_link = "<a href=\"" . $url_link ."\">". $url_link . "</a>";
			 $message = $message . 'Link to reset the password: '. $a_url_link;
			 $message = $message . "<p>";
			 $message = $message . "<p>";
			 $message = $message . 'Best';
			 $headers = 'From: no-reply@yourdressmakers.com' . "<p>" .
			 'Reply-To: no-reply@yourdressmakers.com' . "<p>" .
			 'X-Mailer: PHP/' . phpversion();
			 
			 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message);
		 }
		 if ($language_par == "es")
		 {
			 $to      = $email_real_par;
			 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Resetear contraseña';
			 $message = 'Sigue el enlace para resetear tu contraseña. Atencion que el enlace solo funcionará por 1 hora'. "<p>"; 
			 $message = $message . "<p>";
			 $url_link = $GLOBALS['URL_BASE_SERVER'] .'FormPasswordReset.php?language=' . $language_par . '&id=' . $iduser_par . '&code=' . $code_par; 
			 $a_url_link = "<a href=\"" . $url_link ."\">". $url_link . "</a>";
			 $message = $message . 'Clickar en este enlace: '. $a_url_link;
			 $message = $message . "<p>";
			 $message = $message . "<p>";
			 $message = $message . 'Gracias';
			 $headers = 'From: no-reply@yourdressmakers.com' . "<p>" .
			 'Reply-To: no-reply@yourdressmakers.com' . "<p>" .
			 'X-Mailer: PHP/' . phpversion();
			 
			 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message);
		 }		 
	 }		 
	 
     //-------------------------------------------------------------
     //  LoginEmail
     //-------------------------------------------------------------
     function RequestResetPassword($language_par, $iduser_par)
     {
		// Performing SQL Consult
		$query_user = "SELECT email FROM users WHERE id = $iduser_par";
		$result_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_user) or die("Query Error::RequestResetPassword::Select email failed");
				
		if ($row_user = mysqli_fetch_object($result_user))
		{
			$email_user = DecryptText($row_user->email);
			$resetpassword_timestamp = GetCurrentTimestamp();
			$random_code_reset = rand_string(6);
			
			// SET THE TIMESTAMP AND THE CODE TO RESET
			$query_update_user = "UPDATE users SET resetcode='$random_code_reset', resetpassword=$resetpassword_timestamp WHERE id = $iduser_par";
			$result_update_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_update_user) or die("Query Error::RequestResetPassword::Update users failed");

			if ($result_update_user)
			{
				MailPassword($language_par, $email_user, $iduser_par, $random_code_reset);
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
