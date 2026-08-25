<!DOCTYPE html>
<html>
<body>
<?php
	$language_user = $_GET["language"];
	$iduser_user = $_GET["id"];
	$code_user = $_GET["code"];

	$text_enter_new_password = "Enter Your New Password";
	$text_repeat_new_password = "Repeat Your New Password";
	$submit_text = "Submit";
	if ($language_user == "es")
	{
		$text_enter_new_password = "Introduce nueva contraseña";
		$text_repeat_new_password = "Repite nueva contraseña";
		$submit_text = "Cambiar";
	}
?>
<script>
    function CheckPasswords() {
		var language_var = "<?php echo $language_user; ?>";
		var pass1 = document.forms["myForm"]["user_new_password"].value;
		var pass2 = document.forms["myForm"]["user_repeat_password"].value;
        if (pass1 != pass2) {
			var alert_msg = "Passwords Do not match";
			if (language_var == "es")
			{
				alert_msg = "Las contraseñas no coinciden";
			}
            alert(alert_msg);
            document.forms["myForm"]["user_new_password"].style.borderColor = "#E34234";
            document.forms["myForm"]["user_repeat_password"].style.borderColor = "#E34234";
			return false;
        }
        else {
            return true;
        }
    }
</script>

<form name="myForm" onsubmit="return CheckPasswords()" action="FormPasswordResetConfirmation.php" method="post">
<?php echo $text_enter_new_password; ?> : <input type="password" name="user_new_password" placeholder="" /><br/>
<?php echo $text_repeat_new_password; ?>: <input type="password" name="user_repeat_password" placeholder="" /><br/>
<input type="hidden" name="language_user_reset" value="<?php echo $language_user; ?>">
<input type="hidden" name="id_user_reset" value="<?php echo $iduser_user; ?>">
<input type="hidden" name="code_user_reset" value="<?php echo $code_user; ?>">
<input type="submit" value="<?php echo $submit_text; ?>">
</form>	

</body>
</html>