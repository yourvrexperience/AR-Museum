<?php
	
	include 'ConfigurationUserManagement.php';
    
	$user_new_password = $_POST["user_new_password"];
	$user_repeat_password = $_POST["user_repeat_password"];
	$language = $_POST["language_user_reset"];
	$id_user_reset = $_POST["id_user_reset"];
	$code_user_reset = $_POST["code_user_reset"];

	if (($user_new_password == $user_repeat_password) && (strlen($user_new_password) != 0)&& (strlen($user_repeat_password) != 0))
	{
		ConfirmationResetPassword($language, $user_new_password, $id_user_reset, $code_user_reset);
	}
	else
	{
		if ($language_par == "es")
		{
			print "<html><body><h2>Las contraseñas no son identicas o estan vacias</h2></body></html>";		
		}
		else
		{
			print "<html><body><h2>Passwords are not the same or they are empty</h2></body></html>";		
		}
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
     //  MailPassword
     //-------------------------------------------------------------
	 function MailPassword($language_par, $email_real_par, $password_new_par)
	 {
		 $message = "";
		 if ($language_par == "en")
		 {
			 // EMAIL LEVEL
			 $to      = $email_real_par;
			 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Password Changed successfully';
			 $message = $message . "<p>";
			 $message = 'Your password has been changed successfully.'. "<p>"; 
			 $message = $message . "<p>";
			 $message = $message . 'Your new password is: ' . $password_new_par; 
			 $message = $message . "<p>";
			 $message = $message . "<p>";
			 $message = $message . 'Best';
			 $headers = 'From: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
			 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
			 'X-Mailer: PHP/' . phpversion();
			 
			 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
		 }
		 if ($language_par == "es")
		 {
			 $to      = $email_real_par;
			 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Contraseña cambiada exitosamente';
			 $message = 'Tu contraseña ha sido cambiada exitosamente'. "<p>"; 
			 $message = $message . "<p>";
			 $message = $message . 'Tu nueva contraseña es: ' . $password_new_par; 
			 $message = $message . "<p>";
			 $message = $message . "<p>";
			 $message = $message . 'Gracias';
			 $headers = 'From: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
			 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
			 'X-Mailer: PHP/' . phpversion();
			 
			 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
		 }		 
	 }		 
	 
     //-------------------------------------------------------------
     //  ConfirmationResetPassword
     //-------------------------------------------------------------
     function ConfirmationResetPassword($language_par, $user_new_password_par, $id_user_reset_par, $code_user_reset_par)
     {
		// UPDATE THE PASSWORD
		$password_encrypted = HashPassword($user_new_password_par);
		$query_update_password = "UPDATE users SET password = '$password_encrypted', code = ''  WHERE id = $id_user_reset_par AND code = '$code_user_reset_par'";
		$result_update_password = mysqli_query($GLOBALS['LINK_DATABASE'],$query_update_password) or die("Query Error::PasswordResetConfirmation::ConfirmationResetPassword::Update user failed");
				
		if (mysqli_affected_rows($GLOBALS['LINK_DATABASE']) == 1)
		{
			// GET USER EMAIL AND SEND CONFIRMATION MESSAGE
			$query_consult = "SELECT email FROM users WHERE id = $id_user_reset_par";
			$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'],$query_consult) or die("Query Error::PasswordResetConfirmation::ConfirmationResetPassword::Select email failed");

			if ($row_user = mysqli_fetch_object($result_consult))
			{
				$email_user = $row_user->email;
				MailPassword($language_par, $email_user, $user_new_password_par);
			}
			
			$output_message = "<html><body><h2>Password changed successfully!!</h2></body></html>";
			if ($language_par == "es")
			{
				$output_message = "<html><body><h2>Contraseña cambiada exitosamente!!</h2></body></html>";
			}
			print $output_message;
		}
		else
		{
			$output_error_message = "<html><body><h2>Failure to change password!!</h2></body></html>";
			if ($language_par == "es")
			{
				$output_error_message = "<html><body><h2>Fallo al cambiar contraseña!!</h2></body></html>";
			}
			print $output_error_message;
		}
    }
	
?>
