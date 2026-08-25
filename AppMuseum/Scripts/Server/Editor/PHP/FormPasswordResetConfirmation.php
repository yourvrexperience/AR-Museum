<?php

	include 'ConfigurationUserManagement.php';

	$user_new_password    = isset($_POST["user_new_password"])    ? $_POST["user_new_password"]    : "";
	$user_repeat_password = isset($_POST["user_repeat_password"]) ? $_POST["user_repeat_password"] : "";
	$language             = isset($_POST["language_user_reset"])  ? $_POST["language_user_reset"]  : "";
	$id_user_reset        = isset($_POST["id_user_reset"])        ? $_POST["id_user_reset"]        : "";
	$code_user_reset      = isset($_POST["code_user_reset"])      ? $_POST["code_user_reset"]      : "";

	// ++ RESET PASSWORD ++
	if (($user_new_password == $user_repeat_password) && (strlen($user_new_password) != 0) && (strlen($user_repeat_password) != 0))
	{
		ConfirmationResetPassword($language, $user_new_password, $id_user_reset, $code_user_reset);
	}
	else
	{
		RenderResultPage(false, "Password Reset Failed", "The passwords did not match, or one of the fields was left empty. Please go back and try again.");
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
	//  MailPassword — branded HTML confirmation email
	//-------------------------------------------------------------
	function MailPassword($language_par, $email_real_par, $password_new_par)
	{
		$app_name = isset($GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL']) ? $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] : "Your App";
		$app_url  = isset($GLOBALS['OFFICIAL_URL_APPLICATION_GLOBAL'])  ? $GLOBALS['OFFICIAL_URL_APPLICATION_GLOBAL']  : "#";
		$year     = date('Y');

		$app_name_esc = htmlspecialchars($app_name);
		$app_url_esc  = htmlspecialchars($app_url);
		$password_esc = htmlspecialchars($password_new_par);

		$to      = $email_real_par;
		$subject = $app_name . ' Password Changed Successfully';

		// Email clients are unreliable with <style> blocks, so the card uses inline styles.
		$message =
			"<div style=\"font-family:Arial,Helvetica,sans-serif; background-color:#f4f6f8; margin:0; padding:24px;\">
				<div style=\"max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 12px rgba(0,0,0,0.1); overflow:hidden;\">
					<div style=\"background:linear-gradient(135deg,#007BFF,#00C6FF); color:#ffffff; text-align:center; padding:30px 20px;\">
						<h1 style=\"margin:0; font-size:24px;\">{$app_name_esc}</h1>
					</div>
					<div style=\"padding:30px 40px; text-align:center; color:#333333;\">
						<h2 style=\"color:#007BFF; margin-bottom:10px;\">Password Changed &#9989;</h2>
						<p style=\"font-size:16px; line-height:1.6; margin:0 0 12px;\">Your password has been changed successfully.</p>
						<p style=\"font-size:16px; line-height:1.6; margin:0 0 4px;\">Your new password is:</p>
						<p style=\"font-size:16px; line-height:1.6; font-weight:bold; letter-spacing:0.5px; margin:0;\">{$password_esc}</p>
						<a href=\"{$app_url_esc}\" style=\"display:inline-block; padding:12px 24px; margin-top:20px; background-color:#007BFF; color:#ffffff; border-radius:6px; text-decoration:none; font-weight:bold;\">Go to App</a>
					</div>
					<div style=\"background-color:#f4f6f8; text-align:center; padding:15px; font-size:13px; color:#888888;\">
						<p style=\"margin:0;\">&copy; {$year} {$app_name_esc} &mdash; All rights reserved.</p>
					</div>
				</div>
			</div>";

		// NOTE: proper CRLF-separated headers + a text/html Content-Type so the
		// card renders as HTML (the previous "<p>" separators were not valid email
		// headers). If SendGlobalEmail() sets its own Content-Type, drop it here.
		$headers  = "MIME-Version: 1.0\r\n";
		$headers .= "Content-Type: text/html; charset=UTF-8\r\n";
		$headers .= 'From: '     . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "\r\n";
		$headers .= 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "\r\n";
		$headers .= 'X-Mailer: PHP/' . phpversion();

		SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
	}

	//-------------------------------------------------------------
	//  ConfirmationResetPassword
	//-------------------------------------------------------------
	function ConfirmationResetPassword($language_par, $user_new_password_par, $id_user_reset_par, $code_user_reset_par)
	{
		// UPDATE THE PASSWORD
		$current_time_registered = GetCurrentTimestamp();
		$password_encrypted = HashPasswordWithSalt($user_new_password_par, strval($current_time_registered + $current_time_registered));
		$query_update_password = "UPDATE users SET password = '$password_encrypted', code = '', registerdate = $current_time_registered, lastlogin = $current_time_registered  WHERE id = $id_user_reset_par AND code = '$code_user_reset_par'";
		$result_update_password = mysqli_query($GLOBALS['LINK_DATABASE'], $query_update_password) or die("Query Error::PasswordResetConfirmation::ConfirmationResetPassword::Update user failed");

		if (mysqli_affected_rows($GLOBALS['LINK_DATABASE']) == 1)
		{
			// GET USER EMAIL AND SEND CONFIRMATION MESSAGE
			$query_consult = "SELECT email FROM users WHERE id = $id_user_reset_par";
			$result_consult = mysqli_query($GLOBALS['LINK_DATABASE'], $query_consult) or die("Query Error::PasswordResetConfirmation::ConfirmationResetPassword::Select email failed");

			if ($row_user = mysqli_fetch_object($result_consult))
			{
				$email_user = $row_user->email;
				MailPassword($language_par, $email_user, $user_new_password_par);
			}

			RenderResultPage(true, "Password Changed Successfully", "Your password has been updated. A confirmation email is on its way, and you can now sign in with your new password.");
		}
		else
		{
			RenderResultPage(false, "Password Reset Failed", "We couldn't update your password. This reset link may be invalid or may have already been used.");
		}
	}

	//-------------------------------------------------------------
	//  RenderResultPage — branded success / failure card (browser page)
	//-------------------------------------------------------------
	function RenderResultPage($success, $title, $message)
	{
		$app_name = isset($GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL']) ? $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] : "Your App";
		$app_url  = isset($GLOBALS['OFFICIAL_URL_APPLICATION_GLOBAL'])  ? $GLOBALS['OFFICIAL_URL_APPLICATION_GLOBAL']  : "#";
		$year     = date('Y');

		$app_name_esc = htmlspecialchars($app_name);
		$app_url_esc  = htmlspecialchars($app_url);
		$title_esc    = htmlspecialchars($title);
		$message_esc  = htmlspecialchars($message);

		$icon        = $success ? "&#9989;" : "&#9888;&#65039;"; // check mark or warning sign
		$heading_cls = $success ? "success" : "error";

		echo "<!DOCTYPE html>
<html lang=\"en\">
<head>
	<meta charset=\"UTF-8\">
	<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">
	<title>{$title_esc} &mdash; {$app_name_esc}</title>
	<style>
		body {
			font-family: Arial, Helvetica, sans-serif;
			background-color: #f4f6f8;
			margin: 0;
			padding: 0;
		}
		.container {
			max-width: 600px;
			margin: 50px auto;
			background: #ffffff;
			border-radius: 12px;
			box-shadow: 0 4px 12px rgba(0,0,0,0.1);
			overflow: hidden;
		}
		.header {
			background: linear-gradient(135deg, #007BFF, #00C6FF);
			color: white;
			text-align: center;
			padding: 30px 20px;
		}
		.header h1 { margin: 0; font-size: 24px; }
		.content {
			padding: 30px 40px;
			text-align: center;
			color: #333333;
		}
		.content h2.success { color: #007BFF; margin-bottom: 10px; }
		.content h2.error   { color: #E34234; margin-bottom: 10px; }
		.content p { font-size: 16px; line-height: 1.6; }
		.footer {
			background-color: #f4f6f8;
			text-align: center;
			padding: 15px;
			font-size: 13px;
			color: #888;
		}
		.button {
			display: inline-block;
			padding: 12px 24px;
			margin-top: 20px;
			background-color: #007BFF;
			color: #ffffff;
			border-radius: 6px;
			text-decoration: none;
			font-weight: bold;
		}
		.button:hover { background-color: #0056b3; }
		@media (max-width: 640px) {
			.container { margin: 20px; }
			.content { padding: 24px 22px; }
		}
	</style>
</head>
<body>
	<div class=\"container\">
		<div class=\"header\">
			<h1>{$app_name_esc}</h1>
		</div>
		<div class=\"content\">
			<h2 class=\"{$heading_cls}\">{$title_esc} {$icon}</h2>
			<p>{$message_esc}</p>";

		if ($success)
		{
			echo "
			<a href=\"{$app_url_esc}\" class=\"button\">Go to App</a>";
		}

		echo "
		</div>
		<div class=\"footer\">
			<p>&copy; {$year} {$app_name_esc} &mdash; All rights reserved.</p>
		</div>
	</div>
</body>
</html>";
	}

?>
