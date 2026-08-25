Shader "Template6DOF/OcclusionTemplate"
{
	Properties
	{
	}
	
	SubShader
	{

		Tags
		{
			"Queue" = "Background"
		}
		
		Pass
		{
			Cull back
			Blend Off
			ColorMask [_Alpha]
			ZWrite On
			
		}
	}

}