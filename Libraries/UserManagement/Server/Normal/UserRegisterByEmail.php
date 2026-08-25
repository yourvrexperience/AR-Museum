<?php
	
	include 'ConfigurationUserManagement.php';

	$language = $_GET["language"];
	$email = $_GET["email"];
	$password = $_GET["password"];
	$platform = $_GET["platform"];

	RegisterEmail($language, $email, $password, $platform);

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
	 function MailPassword($language_par, $email_real_par, $random_password_generated_par, $id_par, $random_code_validate_par)
	 {
		 if ($language_par == "en")
		 {
			 // EMAIL LEVEL
			 $to      = $email_real_par;
			 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Access password';
			 $message = 'Thanks a lot for using '. $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . "<p>";
			 $message = $message . "<p>";
			 $message = $message . 'This is your password: ' . $random_password_generated_par . "<p>"; 
			 $message = $message . "<p>";
			 $url_link = $GLOBALS['URL_BASE_SERVER'] .'ValidateEmail.php?language=' . $language_par . '&id=' .$id_par .'&code=' . $random_code_validate_par;
			 $a_url_link = "<a href=\"" . $url_link ."\">". $url_link . "</a>";
			 $message = $message . 'Please click the next link to validate your account: ' . $a_url_link; 
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
			 // EMAIL LEVEL
			 $to      = $email_real_par;
			 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Contraseña de acceso';
			 $message = 'Muchas gracias por utilizar la app '. $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . "<p>"; 
			 $message = $message . "<p>";
			 $message = $message . 'Esta es tu contraseña: ' . $random_password_generated_par . "<p>"; 
			 $message = $message . "<p>";
			 $url_link = $GLOBALS['URL_BASE_SERVER'] .'ValidateEmail.php?language=' . $language_par . '&id=' .$id_par .'&code=' . $random_code_validate_par;
			 $a_url_link = "<a href=\"" . $url_link ."\">". $url_link . "</a>";
			 $message = $message . 'Por favor pulsa en el enlace para confirmar tu cuenta: ' . $a_url_link; 
			 $message = $message . "<p>";
			 $message = $message . 'Gracias';
			 $headers = 'From: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
			 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
			 'X-Mailer: PHP/' . phpversion();

			 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
		 }
	 }		 
	
     //-------------------------------------------------------------
     //  RegisterEmail
     //-------------------------------------------------------------
     function RegisterEmail($language_par, $email_par, $password_par, $platform_par)
     {
		if (ExistsEmail($email_par))
		{
			// USER ALREADY EXISTS
			print "false";
		}
		else
		{
			$query_consult = "SELECT max(id) as maximumId FROM users";
			$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'],$query_consult) or die("Query Error::UserRegisterByEmail::Select max users failed");
			$row_consult = mysqli_fetch_object($result_consult);
			$maxIdentifier = $row_consult->maximumId;
			mysqli_free_result($result_consult);
			
			$user_id_new = $maxIdentifier + 1;
			$current_time_registered = GetCurrentTimestamp();
			$password_encrypted = HashPasswordWithSalt($password_par, strval($current_time_registered + $current_time_registered));			
			$name_temp_user_new = substr($email_par,0,strrpos($email_par, '@'));
			$random_code_validate = rand_string(6);
			$ipadress_new_user = $_SERVER['REMOTE_ADDR'];
	
			// UNCOMMENT THIS LIKE WHEN YOU ARE WORKING WITH A VALID MAIL SERVER ABLE TO DISPATCH EMAILS
			if ($GLOBALS['ENABLE_EMAIL_SERVER'] == 1)
			{
				$query_insert = "INSERT INTO users VALUES ($user_id_new, '$email_par', '$name_temp_user_new', '$password_encrypted', '$platform_par', $current_time_registered, $current_time_registered, 0, 0, '$random_code_validate', -1, '$ipadress_new_user')";	
			}
			else
			{
				$query_insert = "INSERT INTO users VALUES ($user_id_new, '$email_par', '$name_temp_user_new', '$password_encrypted', '$platform_par', $current_time_registered, $current_time_registered, 0, 0, '$random_code_validate', 1, '$ipadress_new_user')";
			}
			
			$result_insert = mysqli_query($GLOBALS['LINK_DATABASE'],$query_insert) or die("Query Error::UserRegisterByEmail::Insert users failed");
			
			if (mysqli_affected_rows($GLOBALS['LINK_DATABASE']) == 1)
			{
				if (InsertOrUpdateProfileData($user_id_new, $name_temp_user_new, '', '', '', '', '', '', ''))
				{
					// MailPassword($language_par, $email_par, $password_par, $user_id_new, $random_code_validate);
					
					$name_user = $email_par;
					$registerdate_user = $current_time_registered;
					$lastlogin_user = $current_time_registered;
					$admin_user = 0;
					$level_user = 0;

					$validated_user = -1;
					if ($GLOBALS['ENABLE_EMAIL_SERVER'] == 0) $validated_user = 1;
						
					$output_register_by_mail = "true" . $GLOBALS['USER_DATA_SEPARATOR'] .  $user_id_new . $GLOBALS['PARAM_SEPARATOR'] .  $email_par . $GLOBALS['PARAM_SEPARATOR'] .  $name_user. $GLOBALS['PARAM_SEPARATOR'] .  $registerdate_user . $GLOBALS['PARAM_SEPARATOR'] . $lastlogin_user . $GLOBALS['PARAM_SEPARATOR'] . $admin_user . $GLOBALS['PARAM_SEPARATOR'] . $level_user . $GLOBALS['PARAM_SEPARATOR'] . $validated_user . $GLOBALS['PARAM_SEPARATOR'] . $platform_par;
					$output_register_by_mail = $output_register_by_mail . GetUserProfileData($user_id_new);
					
					print $output_register_by_mail;
				}
				else
				{
					print "false";
				}
			}
		}
    }

?>
