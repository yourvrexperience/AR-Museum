<?php
	
	include 'ConfigurationUserManagement.php';

	$language = $_GET["language"];
	$iduser = $_GET["id"];
	$codeuser = $_GET["code"];

	ValidateEmail($language, $iduser, $codeuser);
	
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
     //  LoginEmail
     //-------------------------------------------------------------
     function ValidateEmail($language_par, $iduser_par, $codeuser_par)
     {
		// Performing SQL Consult
		$query_user = "SELECT email FROM users WHERE id = $iduser_par AND code = '$codeuser_par'";
		$result_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_user) or die("Query Error::ValidateEmail::Select email failed");
				
		if ($row_user = mysqli_fetch_object($result_user))
		{
			// SET THE TIMESTAMP AND THE CODE TO RESET
			$query_update_user = "UPDATE users SET validated=1, code='' WHERE id = $iduser_par";
			$result_update_user = mysqli_query($GLOBALS['LINK_DATABASE'],$query_update_user) or die("Query Error::ValidateEmail::Update users failed");

			// Common styles for the email
			$app_name = $GLOBALS['OFFICIAL_NAME_APPLICATION_GLOBAL'];
			$styles = "
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
					.content {
						padding: 30px 40px;
						text-align: center;
						color: #333333;
					}
					.content h2 {
						color: #007BFF;
						margin-bottom: 10px;
					}
					.content p {
						font-size: 16px;
						line-height: 1.6;
					}
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
					.button:hover {
						background-color: #0056b3;
					}
				</style>
			";

			if ($language_par == "es") {
				$content = "
					<div class='container'>
						<div class='header'>
							<h1>¡Bienvenido a $app_name!</h1>
						</div>
						<div class='content'>
							<h2>Registro Exitoso 🎉</h2>
							<p>Tu cuenta ha sido verificada correctamente.</p>
							<p>Ya puedes comenzar a utilizar la aplicación.</p>
							<a href='" . $GLOBALS['OFFICIAL_URL_APPLICATION_GLOBAL'] . "' class='button'>Entrar a la App</a>
						</div>
						<div class='footer'>
							<p>© " . date('Y') . " $app_name — Todos los derechos reservados.</p>
						</div>
					</div>
				";
			} else {
				$content = "
					<div class='container'>
						<div class='header'>
							<h1>Welcome to $app_name!</h1>
						</div>
						<div class='content'>
							<h2>Registration Successful 🎉</h2>
							<p>Your account has been successfully verified.</p>
							<p>You can now start using the app.</p>
							<a href='" . $GLOBALS['OFFICIAL_URL_APPLICATION_GLOBAL'] . "' class='button'>Go to App</a>
						</div>
						<div class='footer'>
							<p>© " . date('Y') . " $app_name — All rights reserved.</p>
						</div>
					</div>
				";
			}

			echo $styles . $content;
		}
		else
		{
			print "false";
		}
	 
		// Free resultset
		mysqli_free_result($result_user);
    }
	
?>
