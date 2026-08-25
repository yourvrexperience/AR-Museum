<?php
	
	include 'ConfigurationUserManagement.php';

	$language = $_GET["language"];
	$email = $_GET["email"];
	$password = $_GET["password"];
	$platform = $_GET["platform"];
	$ipaddress = GetUserIPAddress();

	RegisterEmail($language, $email, $password, $platform, $ipaddress);

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
	 function MailPassword($email_real_par, $password_par, $id_par, $random_code_validate_par)
	 {
		 // EMAIL LEVEL
		 $to      = $email_real_par;
		 $subject = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' - Confirm Your Email';
// Email styles and structure
$message = '
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Email Confirmation</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }
        .container {
            max-width: 600px;
            margin: 20px auto;
            background: #ffffff;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 0 10px rgba(0, 0, 0, 0.1);
        }
        h1 {
            color: #333333;
            text-align: center;
        }
        p {
            font-size: 16px;
            color: #666666;
            line-height: 1.5;
        }
        .button {
            display: block;
            width: 200px;
            margin: 20px auto;
            padding: 12px;
            background: #007BFF;
            color: #ffffff;
            text-align: center;
            font-size: 16px;
            font-weight: bold;
            text-decoration: none;
            border-radius: 5px;
        }
        .footer {
            text-align: center;
            font-size: 14px;
            color: #999999;
            margin-top: 20px;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>Welcome to ' . $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . '!</h1>
        <p>Thank you for registering with us. We are excited to have you onboard.</p>
        <p><strong>Your password:</strong> <span style="color:#007BFF;">' . htmlspecialchars($password_par) . '</span></p>
        <p>To complete your registration, please confirm your email by clicking the button below:</p>
        <a href="' . htmlspecialchars($GLOBALS['URL_BASE_SERVER'] . 'ValidateEmail.php?id=' . $id_par . '&code=' . $random_code_validate_par) . '" class="button">Confirm My Email</a>
        <p>If you cannot click the button, copy and paste the following link into your browser:</p>
        <p><a href="' . htmlspecialchars($GLOBALS['URL_BASE_SERVER'] . 'ValidateEmail.php?id=' . $id_par . '&code=' . $random_code_validate_par) . '">' . htmlspecialchars($GLOBALS['URL_BASE_SERVER'] . 'ValidateEmail.php?id=' . $id_par . '&code=' . $random_code_validate_par) . '</a></p>
        <p>Best regards,</p>
        <p><strong>' . $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . ' Team</strong></p>
        <div class="footer">
            <p>&copy; ' . date("Y") . ' ' . $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] . '. All rights reserved.</p>
        </div>
    </div>
</body>
</html>';
		 $headers = 'From: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
		 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "<p>" .
		 'X-Mailer: PHP/' . phpversion();

		 SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
	 }		 
	
     //-------------------------------------------------------------
     //  RegisterEmail
     //-------------------------------------------------------------
     function RegisterEmail($language_par, $email_par, $password_par, $platform_par, $ipadress_par)
     {
		if (ExistsEmail($email_par) || !AllowIPToRegister($ipadress_par, $email_par))
		{
			// USER ALREADY EXISTS
			print "false";
		}
		else
		{
			if (!InsertIPAdress($ipadress_par, $email_par))
			{
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
		
				$currentTimestamp = time();
				$futureTimestamp = strtotime("+30 days", $currentTimestamp);

				// UNCOMMENT THIS LIKE WHEN YOU ARE WORKING WITH A VALID MAIL SERVER ABLE TO DISPATCH EMAILS
				if ($GLOBALS['ENABLE_EMAIL_SERVER'] == 1)
				{
					$query_insert = "INSERT INTO users VALUES ($user_id_new, '$email_par', '$name_temp_user_new', '$password_encrypted', '$platform_par', $current_time_registered, $current_time_registered, 0, 1, '$random_code_validate', -1, '$ipadress_new_user')";	
				}
				else
				{
					$query_insert = "INSERT INTO users VALUES ($user_id_new, '$email_par', '$name_temp_user_new', '$password_encrypted', '$platform_par', $current_time_registered, $current_time_registered, 0, 1, '$random_code_validate', 1, '$ipadress_new_user')";
				}
				
				$result_insert = mysqli_query($GLOBALS['LINK_DATABASE'],$query_insert) or die("Query Error::UserRegisterByEmail::Insert users failed");
				
				if (mysqli_affected_rows($GLOBALS['LINK_DATABASE']) == 1)
				{
					if (InsertOrUpdateProfileData($user_id_new, $name_temp_user_new, '', '', '', '', '', '', ''))
					{
						MailPassword($email_par, $password_par, $user_id_new, $random_code_validate);
						
						$name_user = $email_par;
						$registerdate_user = $current_time_registered;
						$lastlogin_user = $current_time_registered;
						$level_user = 0;
						$admin_user = 0;

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
    }

?>
