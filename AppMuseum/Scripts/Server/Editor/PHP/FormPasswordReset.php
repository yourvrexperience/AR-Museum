<?php

	include 'ConfigurationUserManagement.php';

	$language_user = isset($_GET["language"]) ? $_GET["language"] : "";
	$iduser_user   = isset($_GET["id"])       ? $_GET["id"]       : "";
	$code_user     = isset($_GET["code"])     ? $_GET["code"]     : "";

	$app_name = isset($GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL']) ? $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'] : "Your App";

	// -----------------------------------------------------------------
	//  Localizable UI text (swap these per $language_user if you wish)
	// -----------------------------------------------------------------
	$text_title               = "Reset Your Password";
	$text_subtitle            = "Choose a new password for your account below.";
	$text_enter_new_password  = "Enter Your New Password";
	$text_repeat_new_password = "Repeat Your New Password";
	$text_error_empty         = "Please fill in both password fields";
	$text_error_mismatch      = "Passwords do not match";
	$submit_text              = "Submit";
?>
<!DOCTYPE html>
<html lang="<?php echo htmlspecialchars($language_user); ?>">
<head>
	<meta charset="UTF-8">
	<meta name="viewport" content="width=device-width, initial-scale=1.0">
	<title><?php echo htmlspecialchars($text_title); ?> — <?php echo htmlspecialchars($app_name); ?></title>
	<style>
		body {
			font-family: Arial, Helvetica, sans-serif;
			background-color: #f4f6f8;
			margin: 0;
			padding: 0;
		}
		.container {
			max-width: 520px;
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
		.header h1 {
			margin: 0;
			font-size: 24px;
		}
		.content {
			padding: 30px 40px;
			text-align: center;
			color: #333333;
		}
		.content h2 {
			color: #007BFF;
			margin-bottom: 10px;
		}
		.content p.subtitle {
			font-size: 16px;
			line-height: 1.6;
			margin-top: 0;
		}
		.field {
			text-align: left;
			margin: 18px 0;
		}
		.field label {
			display: block;
			font-size: 14px;
			font-weight: 600;
			color: #333333;
			margin-bottom: 6px;
		}
		.field input {
			width: 100%;
			box-sizing: border-box;
			padding: 12px 14px;
			font-size: 15px;
			color: #333333;
			border: 1px solid #d0d7de;
			border-radius: 6px;
			outline: none;
			transition: border-color .15s ease, box-shadow .15s ease;
		}
		.field input:focus {
			border-color: #007BFF;
			box-shadow: 0 0 0 3px rgba(0,123,255,0.15);
		}
		.error-message {
			display: none;
			color: #E34234;
			font-size: 14px;
			font-weight: 600;
			text-align: left;
			margin: -6px 0 10px 0;
		}
		.button {
			display: inline-block;
			width: 100%;
			box-sizing: border-box;
			padding: 12px 24px;
			margin-top: 10px;
			background-color: #007BFF;
			color: #ffffff;
			border: none;
			border-radius: 6px;
			font-size: 16px;
			font-weight: bold;
			font-family: inherit;
			cursor: pointer;
			transition: background-color .15s ease;
		}
		.button:hover {
			background-color: #0056b3;
		}
		.footer {
			background-color: #f4f6f8;
			text-align: center;
			padding: 15px;
			font-size: 13px;
			color: #888;
		}
		@media (max-width: 560px) {
			.container { margin: 20px; }
			.content { padding: 24px 22px; }
		}
	</style>
</head>
<body>
	<div class="container">
		<div class="header">
			<h1><?php echo htmlspecialchars($app_name); ?></h1>
		</div>
		<div class="content">
			<h2><?php echo htmlspecialchars($text_title); ?></h2>
			<p class="subtitle"><?php echo htmlspecialchars($text_subtitle); ?></p>

			<form name="myForm" onsubmit="return CheckPasswords()" action="FormPasswordResetConfirmation.php" method="post" novalidate>
				<div class="field">
					<label for="user_new_password"><?php echo htmlspecialchars($text_enter_new_password); ?></label>
					<input type="password" id="user_new_password" name="user_new_password" autocomplete="new-password" />
				</div>
				<div class="field">
					<label for="user_repeat_password"><?php echo htmlspecialchars($text_repeat_new_password); ?></label>
					<input type="password" id="user_repeat_password" name="user_repeat_password" autocomplete="new-password" />
				</div>

				<p id="error_message" class="error-message"></p>

				<input type="hidden" name="language_user_reset" value="<?php echo htmlspecialchars($language_user); ?>">
				<input type="hidden" name="id_user_reset"       value="<?php echo htmlspecialchars($iduser_user); ?>">
				<input type="hidden" name="code_user_reset"     value="<?php echo htmlspecialchars($code_user); ?>">

				<button type="submit" class="button"><?php echo htmlspecialchars($submit_text); ?></button>
			</form>
		</div>
		<div class="footer">
			<p>&copy; <?php echo date('Y'); ?> <?php echo htmlspecialchars($app_name); ?> — All rights reserved.</p>
		</div>
	</div>

	<script>
		var MSG_EMPTY    = "<?php echo htmlspecialchars($text_error_empty, ENT_QUOTES); ?>";
		var MSG_MISMATCH = "<?php echo htmlspecialchars($text_error_mismatch, ENT_QUOTES); ?>";

		function CheckPasswords() {
			var pass1   = document.forms["myForm"]["user_new_password"];
			var pass2   = document.forms["myForm"]["user_repeat_password"];
			var errorEl = document.getElementById("error_message");

			function markError(msg) {
				errorEl.textContent = msg;
				errorEl.style.display = "block";
				pass1.style.borderColor = "#E34234";
				pass2.style.borderColor = "#E34234";
			}

			if (pass1.value === "" || pass2.value === "") {
				markError(MSG_EMPTY);
				return false;
			}
			if (pass1.value !== pass2.value) {
				markError(MSG_MISMATCH);
				return false;
			}

			errorEl.style.display = "none";
			pass1.style.borderColor = "";
			pass2.style.borderColor = "";
			return true;
		}
	</script>
</body>
</html>
