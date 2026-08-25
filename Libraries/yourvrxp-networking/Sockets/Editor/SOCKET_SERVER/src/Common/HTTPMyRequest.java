package Common;

import java.io.BufferedReader;
import java.io.DataOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URL;

public class HTTPMyRequest {

	public static final String URL_ORGANIZE_CREATE 	= "http://www.yourescaperoomvr.com/holodeck/php/server/ServerGameOrganizationCreate.php";
	public static final String URL_ORGANIZE_JOIN 	= "http://www.yourescaperoomvr.com/holodeck/php/server/ServerGameOrganizationJoin.php";
	public static final String URL_ORGANIZE_DELETE 	= "http://www.yourescaperoomvr.com/holodeck/php/server/ServerGameOrganizationDelete.php";
    
	public static final String URL_GAME_CREATE 		= "http://www.yourescaperoomvr.com/holodeck/php/server/ServerGameRunningCreate.php";
    public static final String URL_GAME_JOIN 		= "http://www.yourescaperoomvr.com/holodeck/php/server/ServerGameRunningJoin.php";
    public static final String URL_GAME_DELETE 		= "http://www.yourescaperoomvr.com/holodeck/php/server/ServerGameRunningDelete.php";

	
	public static String ExecutePost(String _targetURL, String _urlParameters) 
	{
		return "NOT ENABLED";
	}
}
