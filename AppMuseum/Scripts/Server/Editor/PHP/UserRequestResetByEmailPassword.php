<?php

	include 'ConfigurationUserManagement.php';

	$language   = isset($_GET["language"]) ? $_GET["language"] : "";
	$emailuser  = isset($_GET["email"])    ? $_GET["email"]    : "";

	// ++ REQUEST RESET BY EMAIL ++
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
	//  MailPassword: Send a reset link (branded HTML email)
	//-------------------------------------------------------------
	function MailPassword($language_par, $email_real_par, $iduser_par, $code_par)
	{
		$app_name = isset($GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL']) ? $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] : "Your App";
		$year     = date('Y');

		$app_name_esc = htmlspecialchars($app_name);

		$to      = $email_real_par;
		$subject = $app_name . ' Reset Password';

		// Build the reset link (params url-encoded for safety).
		$url_link = $GLOBALS['URL_BASE_SERVER'] . 'FormPasswordReset.php?language=' . urlencode($language_par) . '&id=' . urlencode($iduser_par) . '&code=' . urlencode($code_par);
		$url_link_esc = htmlspecialchars($url_link);

		// Email clients are unreliable with <style> blocks, so the card uses inline styles.
		$message =
			"<div style=\"font-family:Arial,Helvetica,sans-serif; background-color:#f4f6f8; margin:0; padding:24px;\">
				<div style=\"max-width:600px; margin:0 auto; background:#ffffff; border-radius:12px; box-shadow:0 4px 12px rgba(0,0,0,0.1); overflow:hidden;\">
					<div style=\"background:linear-gradient(135deg,#007BFF,#00C6FF); color:#ffffff; text-align:center; padding:30px 20px;\">
						<h1 style=\"margin:0; font-size:24px;\">{$app_name_esc}</h1>
					</div>
					<div style=\"padding:30px 40px; text-align:center; color:#333333;\">
						<h2 style=\"color:#007BFF; margin-bottom:10px;\">Reset Your Password</h2>
						<p style=\"font-size:16px; line-height:1.6; margin:0 0 8px;\">We received a request to reset your password. Click the button below to choose a new one.</p>
						<a href=\"{$url_link_esc}\" style=\"display:inline-block; padding:12px 24px; margin-top:20px; background-color:#007BFF; color:#ffffff; border-radius:6px; text-decoration:none; font-weight:bold;\">Reset Password</a>
						<p style=\"font-size:13px; line-height:1.6; color:#888888; margin:24px 0 4px;\">If the button doesn't work, copy and paste this link into your browser:</p>
						<p style=\"font-size:13px; line-height:1.6; word-break:break-all; margin:0;\"><a href=\"{$url_link_esc}\" style=\"color:#007BFF;\">{$url_link_esc}</a></p>
						<p style=\"font-size:13px; line-height:1.6; color:#888888; margin:24px 0 0;\">If you didn't request a password reset, you can safely ignore this email.</p>
					</div>
					<div style=\"background-color:#f4f6f8; text-align:center; padding:15px; font-size:13px; color:#888888;\">
						<p style=\"margin:0;\">&copy; {$year} {$app_name_esc} &mdash; All rights reserved.</p>
					</div>
				</div>
			</div>";

		// NOTE: real CRLF-separated headers (the previous version used single quotes,
		// so '\r\n' was literal text, not line breaks). If SendGlobalEmail() sets its
		// own Content-Type, drop the one below to avoid a duplicate header.
		$headers  = "MIME-Version: 1.0\r\n";
		$headers .= "Content-Type: text/html; charset=UTF-8\r\n";
		$headers .= 'From: '     . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "\r\n";
		$headers .= 'Reply-To: ' . $GLOBALS['NON_REPLY_EMAIL_ADDRESS'] . "\r\n";
		$headers .= 'X-Mailer: PHP/' . phpversion();

		SendGlobalEmail($GLOBALS['NON_REPLY_EMAIL_ADDRESS'], $to, $subject, $message, $headers);
	}

	//-------------------------------------------------------------
	//  RequestResetByEmailPassword
	//-------------------------------------------------------------
	function RequestResetByEmailPassword($language_par, $emailuser_par)
	{
		// Performing SQL Consult
		$query_user = "SELECT id FROM users WHERE email = '$emailuser_par'";
		$result_user = mysqli_query($GLOBALS['LINK_DATABASE'], $query_user) or die("Query Error::RequestResetByEmailPassword::Select users failed");

		if ($row_user = mysqli_fetch_object($result_user))
		{
			$id_user = $row_user->id;
			$random_code_reset = rand_string(6);

			// SET THE CODE TO RESET
			$query_update_user = "UPDATE users SET code='$random_code_reset' WHERE id = $id_user";
			$result_update_user = mysqli_query($GLOBALS['LINK_DATABASE'], $query_update_user) or die("Query Error::RequestResetByEmailPassword::Update users failed");

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