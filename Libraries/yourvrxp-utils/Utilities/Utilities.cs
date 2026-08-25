using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_OPENXR
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace yourvrexperience.Utils
{
	public static class Utilities
	{
		public const string SeparatorBasicTypes = ";";

		public static string ConvertTimestampToDate(long timestamp)
		{
			DateTime date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
			return date.ToString("MM/dd/yyyy");
		}

		public static long GetCurrentTimestamp()
		{
			return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		}

		public static long AddDaysToTimestamp(long timestamp, int days)
		{
			DateTime date = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
			date = date.AddDays(days);
			return new DateTimeOffset(date).ToUnixTimeSeconds();
		}

		public static string ScreenShotName(int width, int height)
		{
			return string.Format("screen{0}x{1}{2}.png",
								 width, height,
								 System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
		}

		public static string ReplaceNonAlphanumericWithSpace(string input)
		{
			if (input == null) throw new ArgumentNullException(nameof(input));
			return Regex.Replace(input, "[^a-zA-Z0-9]", " ");
		}

		public static string RemoveSpaces(string input)
		{
			if (input == null) throw new ArgumentNullException(nameof(input));
			return input.Replace(" ", string.Empty);
		}

		public static string CleanXMLText(string text)
		{
			string output = text;
			output = output.Replace('"','\t');
			output = output.Replace('>','\t');
			output = output.Replace('<','\t');
			output = output.Replace('/','\t');
			output = output.Replace('\\','\t');
			output = output.Replace("\t", "");
			output = output.Replace("\n", "");
			return output;
		}

		public static string RemoveXmlTags(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			string pattern = "<.*?>";
			return Regex.Replace(input, pattern, string.Empty);
		}

		public static string RemoveBrokenTags(string input)
		{
			// Regex to find unclosed or improperly closed tags
			string pattern = @"<[^>]+?(?=<|$)";
			
			// Replace broken tags with an empty string
			string result = Regex.Replace(input, pattern, string.Empty);
			
			return result;
		}

		public static string ColorToHexString(Color color)
		{
			int r = Mathf.RoundToInt(color.r * 255);
			int g = Mathf.RoundToInt(color.g * 255);
			int b = Mathf.RoundToInt(color.b * 255);
			return $"#{r:X2}{g:X2}{b:X2}";
		}

		public static Vector3 GetDirection(Vector3 target, Vector3 origin)
		{
			return (target - origin).normalized;
		}

		public static Vector2 ClonePoint(Vector2 point)
		{
			return new Vector2(point.x, point.y);
		}

		public static Vector3 ClonePoint(Vector3 point)
		{
			return new Vector3(point.x, point.y, point.z);
		}

		public static Vector2 GetLocalCoordsUIObject(Vector2 coords, GameObject gameObj)
		{
			return gameObj.transform.InverseTransformPoint(coords);
		}

		public static bool AreCoordsWithinUiObject(Vector2 coords, GameObject gameObj)
		{
			Vector2 localPos = gameObj.transform.InverseTransformPoint(coords);
			return ((RectTransform)gameObj.transform).rect.Contains(localPos);
		}		
		
		public static float IsInsideCone(GameObject source, float angle, GameObject objective, float rangeDetection, float angleDetection)
		{
			float distance = Vector3.Distance(new Vector3(source.transform.position.x, 0, source.transform.position.z),
											 new Vector3(objective.transform.position.x, 0, objective.transform.position.z));
			if (distance < rangeDetection)
			{
				float yaw = angle * Mathf.Deg2Rad;
				Vector2 pos = new Vector2(source.transform.position.x, source.transform.position.z);

				Vector2 v1 = new Vector2((float)Mathf.Cos(yaw), (float)Mathf.Sin(yaw));
				Vector2 v2 = new Vector2(objective.transform.position.x - pos.x, objective.transform.position.z - pos.y);

				// Angle detection
				float moduloV2 = v2.magnitude;
				if (moduloV2 == 0)
				{
					v2.x = 0.0f;
					v2.y = 0.0f;
				}
				else
				{
					v2.x = v2.x / moduloV2;
					v2.y = v2.y / moduloV2;
				}
				float angleCreated = (v1.x * v2.x) + (v1.y * v2.y);
				float angleResult = Mathf.Cos(angleDetection * Mathf.Deg2Rad);

				if (angleCreated > angleResult)
				{
					return (distance);
				}
				else
				{
					return (-1);
				}
			}
			else
			{
				return (-1);
			}
		}

		public static void ActivatePhysics(GameObject target, bool activated)
		{
			if (target.GetComponent<Rigidbody>() != null)
			{
				target.GetComponent<Rigidbody>().isKinematic = !activated;
				target.GetComponent<Rigidbody>().useGravity = activated;
			}
			if (target.GetComponent<Collider>() != null)
			{
				target.GetComponent<Collider>().isTrigger = !activated;
			}
		}


		public static GameObject GetCollisionMouseWithLayers(params string[] masksToConsider)
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			int layerMask = 0;
			if (masksToConsider != null)
			{
				for (int i = 0; i < masksToConsider.Length; i++)
				{
					layerMask |= (1 << LayerMask.NameToLayer(masksToConsider[i]));
				}
			}
			if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
			{
				return hit.collider.gameObject;
			}
			else
			{
				return null;
			}
		}

		public static float DistanceXZ(Vector3 one, Vector3 two)
		{
			float x = (one.x - two.x);
			float z = (one.z - two.z);
			return Mathf.Sqrt((x * x) + (z * z));
		}

		public static float DistanceXY(Vector3 one, Vector3 two)
		{
			float x = (one.x - two.x);
			float y = (one.y - two.y);
			return Mathf.Sqrt((x * x) + (y * y));
		}

		public static float DistanceXZ(Vector2 one, Vector2 two)
		{
			float x = (one.x - two.x);
			float y = (one.y - two.y);
			return Mathf.Sqrt((x * x) + (y * y));
		}		

		public static List<Vector3> GetBoundaryPoints(Vector3 origin, Vector3 target)
		{
			List<Vector3> pointsPlane = new List<Vector3>();

			float topX = ((origin.x < target.x) ? target.x : origin.x);
			float topY = ((origin.z < target.z) ? target.z : origin.z);

			float bottomX = ((origin.x > target.x) ? target.x : origin.x);
			float bottomY = ((origin.z > target.z) ? target.z : origin.z);

			pointsPlane.Add(new Vector3(bottomX, 0, bottomY));
			pointsPlane.Add(new Vector3(bottomX, 0, topY));
			pointsPlane.Add(new Vector3(topX, 0, topY));
			pointsPlane.Add(new Vector3(topX, 0, bottomY));

			return pointsPlane;
		}

		public static void DebugLogColor(string message, Color color)
		{
			if (color == Color.red)
			{
				Debug.Log("<color=red>" + message + "</color>");
			}
			if (color == Color.blue)
			{
				Debug.Log("<color=blue>" + message + "</color>");
			}
			if (color == Color.green)
			{
				Debug.Log("<color=green>" + message + "</color>");
			}
			if (color == Color.yellow)
			{
				Debug.Log("<color=yellow>" + message + "</color>");
			}
		}

		public static string RandomCodeGeneration(int length)
		{
			string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var stringChars = new char[length];
			var random = new System.Random();

			for (int i = 0; i < length; i++)
			{
				stringChars[i] = chars[random.Next(chars.Length)];
			}

			string finalString = new String(stringChars);
			return finalString;
		}

		public static string SerializeVector3(Vector3 data)
		{
			return data.x + SeparatorBasicTypes + data.y + SeparatorBasicTypes + data.z;
		}

		public static Vector3 DeserializeVector3(string data)
		{
			string[] buffer = data.Split(';');
			return new Vector3(float.Parse(buffer[0]), float.Parse(buffer[1]), float.Parse(buffer[2]));
		}

		public static string SerializeVector2(Vector2 data)
		{
			return data.x + SeparatorBasicTypes + data.y;
		}

		public static Vector2 DeserializeVector2(string data)
		{
			string[] buffer = data.Split(new String[]{SeparatorBasicTypes}, data.Length, StringSplitOptions.None);
			return new Vector2(float.Parse(buffer[0]), float.Parse(buffer[1]));
		}

		public static string PackColor(Color color)
		{
			return color.r + SeparatorBasicTypes + color.g + SeparatorBasicTypes + color.b + SeparatorBasicTypes + color.a;
		}

		public static string SerializeQuaternion(Quaternion data)
		{
			return data.x + SeparatorBasicTypes + data.y + SeparatorBasicTypes + data.z + SeparatorBasicTypes + data.w;
		}

		public static Quaternion DeserializeQuaternion(string data)
		{
			string[] buffer = data.Split(new String[]{SeparatorBasicTypes}, data.Length, StringSplitOptions.None);
			return new Quaternion(float.Parse(buffer[0]), float.Parse(buffer[1]), float.Parse(buffer[2]), float.Parse(buffer[3]));
		}

		public static Color UnpackColor(string color)
		{
			string[] colorData = color.Split(new String[]{SeparatorBasicTypes}, color.Length, StringSplitOptions.None);
			return new Color(float.Parse(colorData[0]),float.Parse(colorData[1]),float.Parse(colorData[2]),float.Parse(colorData[3]));
		}

		public static string SerializeIntList(List<int> data)
		{
			string output = "";
			for (int i = 0; i < data.Count; i++)
			{
				output += data[i].ToString();
				if (i < data.Count - 1) output += SeparatorBasicTypes;
			}
			return output;
		}
		public static void DeserializeIntList(string data, ref List<int> list)
		{
			string[] listData = data.Split(new String[]{SeparatorBasicTypes}, data.Length, StringSplitOptions.None);
			list = new List<int>();
			for (int i = 0; i < listData.Length; i++)
			{
				list.Add(int.Parse(listData[i]));
			}
		}

		private static readonly DateTime Jan1St1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long GetTimestamp()
		{
			return (long)((DateTime.UtcNow - Jan1St1970).TotalMilliseconds);
		}
		public static long GetTimestampSeconds()
		{
			return (long)((DateTime.UtcNow - Jan1St1970).TotalSeconds);
		}
		public static Vector2 RotatePoint(Vector2 pointToRotate, Vector2 centerPoint, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Vector2
            {
                x = (float)
                    (cosTheta * (pointToRotate.x - centerPoint.x) -
                    sinTheta * (pointToRotate.y - centerPoint.y) + centerPoint.x),
                y = (float)
                    (sinTheta * (pointToRotate.x - centerPoint.x) +
                    cosTheta * (pointToRotate.y - centerPoint.y) + centerPoint.y)
            };
        }
		
		public static float GetAngleFromNormal(Vector2 normal)
		{	
   			return (Mathf.Atan2(normal.x, normal.y) / Mathf.PI) * 180;
		}

		public static void ApplyColor(Transform target, Color color)
		{
			if (target.GetComponent<MeshRenderer>() != null)
			{
				target.GetComponent<MeshRenderer>().material.color = color;
			}
			foreach (Transform item in target)
			{
				ApplyColor(item, color);
			}
		}

		public static void ApplyMaterialOnImages(Transform target, Material material)
		{
			if (target.GetComponent<Image>() != null)
			{
				target.GetComponent<Image>().material = material;
			}
			foreach (Transform item in target)
			{
				ApplyMaterialOnImages(item, material);
			}
		}

		public static void ApplyInteractivity(Transform target, bool interaction)
		{
			if (target.GetComponent<Selectable>() != null)
			{
				target.GetComponent<Selectable>().interactable = interaction;
			}
			foreach (Transform item in target)
			{
				ApplyInteractivity(item, interaction);
			}
		}

		public static void ApplyEnabledInteraction(Transform target, bool interaction)
		{
			if (target.GetComponent<Selectable>() != null)
			{
				target.GetComponent<Selectable>().enabled = interaction;
			}
			foreach (Transform item in target)
			{
				ApplyEnabledInteraction(item, interaction);
			}
		}

		public static void SetIsTrigger(Transform target, bool isTrigger)
		{
			if (target.GetComponent<Collider>() != null)
			{
				target.GetComponent<Collider>().isTrigger = isTrigger;
			}
			foreach (Transform item in target)
			{
				SetIsTrigger(item, isTrigger);
			}
		}

		public static void EnableRenderers(Transform target, bool enabled)
		{
			if (target.GetComponent<Renderer>() != null)
			{
				target.GetComponent<Renderer>().enabled = enabled;
			}
			foreach (Transform item in target)
			{
				EnableRenderers(item, enabled);
			}
		}

		public static void EnablerRenderers(Transform target, bool enabled)
		{
			if (target.GetComponent<EnablerRenderer>() != null)
			{
				target.GetComponent<EnablerRenderer>().Activate(enabled);
			}
			foreach (Transform item in target)
			{
				EnablerRenderers(item, enabled);
			}
		}
		public static void ApplyLayer(Transform target, int layer)
		{
			if (target != null)
			{
				target.gameObject.layer = layer;
			}
			foreach (Transform item in target)
			{
				ApplyLayer(item, layer);
			}
		}

		public static void DisableGraphicRaycaster(Transform target)
		{
			if (target.GetComponent<GraphicRaycaster>() != null)
			{
				target.GetComponent<GraphicRaycaster>().enabled = false;
			}
#if ENABLE_OCULUS
			if (target.GetComponent<OVRRaycaster>() != null)
			{
				target.GetComponent<OVRRaycaster>().enabled = false;
			}
#elif ENABLE_OPENXR
			if (target.GetComponent<TrackedDeviceGraphicRaycaster>() != null)
			{
				target.GetComponent<TrackedDeviceGraphicRaycaster>().enabled = false;
			}
#elif ENABLE_ULTIMATEXR
			if (target.GetComponent<UxrCanvas>() != null)
			{
				target.GetComponent<UxrCanvas>().enabled = false;
			}
			if (target.GetComponent<UxrLaserPointerRaycaster>() != null)
			{
				target.GetComponent<UxrLaserPointerRaycaster>().enabled = false;
			}
#elif ENABLE_NREAL
			if (target.GetComponent<CanvasRaycastTarget>() != null)
			{
				target.GetComponent<CanvasRaycastTarget>().enabled = false;
			}
#endif

			foreach (Transform item in target)
			{
				DisableGraphicRaycaster(item);
			}
		}


		public static string GetFullPathNameGO(GameObject go)
		{
			if (go == null || go.transform == null)
			{
				return "";
			}
			if (go.transform.parent == null)
			{
				return go.name;
			}

			return GetFullPathNameGO(go.transform.parent.gameObject) + "," + go.name;
		}

		public static GameObject AddChild(Transform parent, GameObject prefab)
		{
			GameObject newObj = GameObject.Instantiate(prefab);
			newObj.transform.SetParent(parent, false);
			return newObj;
		}

        public static GameObject AttachChild(Transform parent, GameObject prefab)
        {
            prefab.transform.SetParent(parent, false);
            return prefab;
        }

		public static void FixObject(GameObject objectToFix)
        {
			if (objectToFix != null)
			{
				if (objectToFix.GetComponent<Collider>() != null)
				{
					objectToFix.GetComponent<Collider>().isTrigger = true;
				}
				if (objectToFix.GetComponent<Rigidbody>() != null)
				{
					objectToFix.GetComponent<Rigidbody>().isKinematic = true;
					objectToFix.GetComponent<Rigidbody>().useGravity = false;
				}
			}
        }

        public static bool FindGameObjectInChilds(GameObject go, GameObject target)
        {
            if (go == target)
            {
                return true;
            }
            bool output = false;
            foreach (Transform child in go.transform)
            {
                output = output || FindGameObjectInChilds(child.gameObject, target);
            }
            return output;
        }

		public static void Shuffle<T>(this IList<T> list, System.Random rnd)
		{
			for(var i=list.Count; i > 0; i--)
				list.Swap(0, rnd.Next(0, i));
		}

		public static void Swap<T>(this IList<T> list, int i, int j)
		{
			var temp = list[i];
			list[i] = list[j];
			list[j] = temp;
		}

		public static void ReverseNormals(GameObject gameObject)
		{
			// Renders interior of the overlay instead of exterior.
			MeshFilter filter = gameObject.GetComponent(typeof(MeshFilter)) as MeshFilter;
			if(filter != null)
			{
				Mesh mesh = filter.mesh;
				Vector3[] normals = mesh.normals;
				for(int i = 0; i < normals.Length; i++)
					normals[i] = -normals[i];
				mesh.normals = normals;

				for(int m = 0; m < mesh.subMeshCount; m++)
				{
					int[] triangles = mesh.GetTriangles(m);
					for(int i = 0; i < triangles.Length; i += 3)
					{
						int temp = triangles[i + 0];
						triangles[i + 0] = triangles[i + 1];
						triangles[i + 1] = temp;
					}
					mesh.SetTriangles(triangles, m);
				}
			}
		}

		public static string GetFormattedTimeMinutes(int time)
		{
			int minutes = (int)time / 60;
			int seconds = (int)time % 60;

			// SECONDS
			String secondsBuf;
			if (seconds < 10)
			{
				secondsBuf = "0" + seconds;
			}
			else
			{
				secondsBuf = "" + seconds;
			}

			// MINUTES
			String minutesBuf;
			if (minutes < 10)
			{
				minutesBuf = "0" + minutes;
			}
			else
			{
				minutesBuf = "" + minutes;
			}

			return (minutesBuf + ":" + secondsBuf);
		}

		public static bool IsVisibleFrom(Bounds _bounds, Camera _camera)
		{
			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_camera);
			return GeometryUtility.TestPlanesAABB(planes, _bounds);
		}

		public static bool IsVisibleFrom(Vector3 worldPosition, Camera camera)
		{
			Vector3 viewportPoint = camera.WorldToViewportPoint(worldPosition);

			return (viewportPoint.z > 0 && viewportPoint.x >= 0 && viewportPoint.x <= 1 && viewportPoint.y >= 0 && viewportPoint.y <= 1);
		}		

		public static bool IsInsideFrustrum(Vector3 worldPosition, Vector3 size, Camera camera)
		{
			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
			Bounds bounds = new Bounds(worldPosition, size);
			return GeometryUtility.TestPlanesAABB(planes, bounds);
		}
        
		public static string RandomCodeIV(int _size)
		{
			string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789#!@+=-*";
			var stringChars = new char[_size];
			var random = new System.Random();

			for (int i = 0; i < _size; i++)
			{
				stringChars[i] = chars[random.Next(chars.Length)];
			}

			string finalString = new String(stringChars);
			return finalString;
		}

		public static Bounds GetMaxBounds(GameObject g) 
		{
			var renderers = g.GetComponentsInChildren<Renderer>();
			if (renderers.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
			var b = renderers[0].bounds;
			foreach (Renderer r in renderers) 
			{
				b.Encapsulate(r.bounds);
			}
			return b;
		}

		public static List<string> GetAnimationNames(GameObject target, string separator = "")
		{
			List<string> animations = new List<string>();
			Animator animatorContainer = target.GetComponentInChildren<Animator>();
			if (animatorContainer != null)
			{
				if (animatorContainer.runtimeAnimatorController != null)
				{
					RuntimeAnimatorController runtimeAnimator = animatorContainer.runtimeAnimatorController;
					AnimationClip[] animationClips = runtimeAnimator.animationClips;
					foreach (AnimationClip item in animationClips)
					{
						string triggerAnimation = item.name;
						int indexSpecialAnimation = triggerAnimation.IndexOf(separator);
						if (indexSpecialAnimation != -1)
						{
							triggerAnimation = triggerAnimation.Substring(indexSpecialAnimation + 1, triggerAnimation.Length - (indexSpecialAnimation + 1)).ToLower();
						}
						else
						{
							triggerAnimation = triggerAnimation.ToLower();
						}
						animations.Add(triggerAnimation);
					}
				}
			}
			return animations;
		}

		public static void TrimArray(string[] array)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = array[i].Trim();
			}
		}
		
		public static void TrimArray(List<string> array)
		{
			for (int i = 0; i < array.Count; i++)
			{
				array[i] = array[i].Trim();
			}
		}

		public static string ReplaceTextPosition(string original, int start, int end, string replace)
		{
			int index = Mathf.Min(start, end);
			int size = Mathf.Abs(end - start);
			return original.Substring(0, index) + replace + original.Substring(index + size, original.Length - (index + size));
		}

 		public static string ReplaceWord(string text, string wordToReplace, string wordToReplaceWith)
    	{
			string pattern = string.Format(@"\b{0}\b", wordToReplace);
			string replacedText = Regex.Replace(text, pattern, wordToReplaceWith, RegexOptions.IgnoreCase);
			return replacedText;
	    }

		public static string RemoveNonStandardCharacters(string original)
		{
			  return Regex.Replace(original, @"[^a-zA-Z0-9]+", string.Empty);
		}

		public static string ExtractXML(string tag, string data)
		{
			string startTag = "<" + tag + " ";
			string endTag = "</" + tag + ">";
			string buf = data;
			string output = "";
			while (buf.IndexOf(startTag) != -1)
			{
				output += buf.Substring(buf.IndexOf(startTag), (buf.IndexOf(endTag) + endTag.Length) - buf.IndexOf(startTag));
				buf = buf.Substring(buf.IndexOf(endTag) + endTag.Length, buf.Length - (buf.IndexOf(endTag) + endTag.Length));
			}
			return output;
		}

		public static int FindStartingTagXML(string tag, string data)
		{
			string startTag = "<" + tag;
			return data.IndexOf(startTag);
		}

		public static string ExtractColorTagXML(string data)
		{
			string startTag = "<color=";
			int index = data.IndexOf(startTag) + startTag.Length;
			return data.Substring(index, 7);
		}

		public static int FindEndingTagXML(string tag, string data)
		{
			string endTag = "</" + tag + ">";
			return data.IndexOf(endTag);
		}

		public static string CalculateChecksum(string input)
		{
			using (SHA256 sha256Hash = SHA256.Create())
			{
				byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

				StringBuilder builder = new StringBuilder();
				for (int i = 0; i < bytes.Length; i++)
				{
					builder.Append(bytes[i].ToString("x2"));
				}
				return builder.ToString();
			}
		}		

		public static bool CheckInsideFrustrum(Collider targetObject, Camera cam)
		{
			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);
			return GeometryUtility.TestPlanesAABB(planes, targetObject.bounds);
		}

		public static Vector3 ConvertToTargetSystem(Transform originalSystem, Transform targetSystem, Vector3 originalPoint)
		{
			Vector3 pointInWorldSpace = originalSystem.TransformPoint(originalPoint);
			Vector3 pointInTargetSpace = targetSystem.InverseTransformPoint(pointInWorldSpace);
			return pointInTargetSpace;
		}

		public static Vector3 ConvertToOriginalSystem(Transform originalSystem, Transform targetSystem, Vector3 targetPoint)
		{
			Vector3 pointInWorldSpace = targetSystem.TransformPoint(targetPoint);
			Vector3 pointInOriginalSpace = originalSystem.InverseTransformPoint(pointInWorldSpace);
			return pointInOriginalSpace;
		}

		public static Vector3 ProjectPointOntoPlane(Transform originalSystem, Transform targetSystem, Vector3 originalPoint)
		{
			Vector3 pointInTargetSystem = ConvertToTargetSystem(originalSystem, targetSystem, originalPoint);

			pointInTargetSystem.y = 0;

			Vector3 projectedPointInOriginalSystem = ConvertToOriginalSystem(originalSystem, targetSystem, pointInTargetSystem);

			return projectedPointInOriginalSystem;
		}

		public static Vector3 ApplyTransformToPoint(Vector3 projectedPoint, Vector3 translation, Quaternion rotation)
		{
			GameObject tempContainer = new GameObject();

			tempContainer.transform.position = projectedPoint;

			tempContainer.transform.Translate(translation);

			tempContainer.transform.Rotate(rotation.eulerAngles);

			Vector3 finalPosition = tempContainer.transform.position;

			GameObject.Destroy(tempContainer);

			return finalPosition;
		}

		public static Vector3 SwapYZ(Vector3 v)
		{
			return new Vector3(v.x, v.z, v.y);
		}

		public static Vector3 ConvertToTargetSystemSwap(Transform originalSystem, Transform targetSystem, Vector3 originalPoint)
		{
			Vector3 pointInWorldSpace = originalSystem.TransformPoint(originalPoint);
			Vector3 pointInWorldSpaceSwapped = SwapYZ(pointInWorldSpace);
			Vector3 pointInTargetSpace = targetSystem.InverseTransformPoint(pointInWorldSpaceSwapped);

			return pointInTargetSpace;
		}

		public static Vector3 ConvertToOriginalSystemSwap(Transform originalSystem, Transform targetSystem, Vector3 targetPoint)
		{
			Vector3 pointInWorldSpace = targetSystem.TransformPoint(targetPoint);
			Vector3 pointInWorldSpaceSwapped = SwapYZ(pointInWorldSpace);
			Vector3 pointInOriginalSpace = originalSystem.InverseTransformPoint(pointInWorldSpaceSwapped);

			return pointInOriginalSpace;
		}

		public static Vector3 ProjectPointOntoPlaneSwap(Transform originalSystem, Transform targetSystem, Vector3 originalPoint)
		{
			Vector3 pointInTargetSystem = ConvertToTargetSystemSwap(originalSystem, targetSystem, originalPoint);

			pointInTargetSystem.y = 0;

			Vector3 projectedPointInOriginalSystem = ConvertToOriginalSystemSwap(originalSystem, targetSystem, pointInTargetSystem);

			return projectedPointInOriginalSystem;
		}

		public static Transform FindNameInChildren(Transform target, string nameChild)
		{
			if (target.name.Equals(nameChild))
			{
				return target;
			}
			Transform output = null;
			foreach (Transform item in target)
			{
				Transform result = FindNameInChildren(item, nameChild);
				if (result != null)
				{
					output = result;
				}
			}
			return output;
		}

		public static int StringToHash(string value, int range)
		{
			return (value.GetHashCode() & 0x7FFFFFFF) % range; 
		}

		public static void DebugLogError(string _message)
		{
			Debug.Log("<color=red>" + _message + "</color>");
		}


		public static void DebugLogWarning(string _message)
		{
			Debug.Log("<color=yellow>" + _message + "</color>");
		}

        public static int CastAsInteger(object _data)
        {
            if (_data is string)
            {
                return int.Parse(_data.ToString());
            }
            else if (_data is float)
            {
                return (int)((float)_data);
            }
            else if (_data is double)
            {
                return (int)((double)_data);
            }
            else if (_data is int)
            {
                return (int)_data;
            }
            else if (_data is long)
            {
                return (int)((long)_data);
            }
            return -1;
        }

        public static float CastAsFloat(object _data)
        {
            if (_data is string)
            {
                return float.Parse(_data.ToString());
            }
            else if (_data is float)
            {
                return (float)_data;
            }
            else if (_data is double)
            {
                return (float)((double)_data);
            }
            else if (_data is int)
            {
                return (float)_data;
            }
            else if (_data is long)
            {
                return (float)((long)_data);
            }
            return -1;
        }

        public static long CastAsLong(object _data)
        {
            if (_data is string)
            {
                return long.Parse(_data.ToString());
            }
            else if (_data is float)
            {
                return (long)((float)_data);
            }
            else if (_data is double)
            {
                return (long)((double)_data);
            }
            else if (_data is int)
            {
                return (long)((int)_data);
            }
            else if (_data is long)
            {
                return (long)_data;
            }
            return -1;
        }

		public static void ResetMaterials(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                foreach(var m in renderer.materials)
                {
                    m.shader = Shader.Find(m.shader.name);
                }
            }
        }

		public static bool ApplySingleZTestTop(Transform target)
		{
			Renderer rendererGraphic = null;
			Graphic canvasGraphic = target.GetComponent<Graphic>();
			bool applyNewComparison = false;
			Material materialGraphic = null;
			if (canvasGraphic == null)
			{
				applyNewComparison = false;
				rendererGraphic = target.GetComponent<Renderer>();
				if (rendererGraphic != null)
				{
					materialGraphic = rendererGraphic.sharedMaterial;
					if (materialGraphic == null)
					{
						applyNewComparison = false;
					}
					else
					{
						applyNewComparison = true;
					}
				}
			}
			else
			{
				materialGraphic = canvasGraphic.materialForRendering;
				if (materialGraphic == null)
				{
					applyNewComparison = false;
				}
				else
				{
					applyNewComparison = true;
				}
			}

			if (applyNewComparison)
			{				
				Material materialCopy = new Material(materialGraphic);
				if (canvasGraphic != null)
				{
					materialCopy.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
					canvasGraphic.material = materialCopy;
				}
				else
				{
					if (rendererGraphic != null)
					{
						materialCopy.SetFloat("_ZTest", 0);						
						rendererGraphic.sharedMaterial  = materialCopy;
					}
				}				
			}
			return applyNewComparison;
		}

		public static void ApplyZTestTop(Transform parent)
		{
			if (parent.GetComponent<DontApplyZTestIgnoring>() != null) return;
			
			ApplySingleZTestTop(parent);

			foreach (Transform child in parent)
			{
				ApplySingleZTestTop(child);

				if (child.childCount > 0)
				{
					ApplyZTestTop(child);
				}
			}
		}		

		public static bool IsClickedOverUI()
		{
			bool worldInteraction = true;
#if UNITY_ANDROID && !UNITY_EDITOR && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			worldInteraction = EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
#else
			worldInteraction = EventSystem.current.IsPointerOverGameObject();
#endif
			return worldInteraction;
		}

        public static void SaveStringToFile(string filename, string dataToSave)
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            System.IO.File.WriteAllText(path, dataToSave, Encoding.UTF8);
        }

        public static string LoadStringFromFile(string filename)
        {
            string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            
            if (System.IO.File.Exists(path))
            {
                string buffer = System.IO.File.ReadAllText(path, Encoding.UTF8);
                return buffer;
            }
            else
            {
                return "";
            }
        }

		public static float[] ConvertByteToFloat(byte[] array)
		{
			float[] floatArr = new float[array.Length / 4];
			for (int i = 0; i < floatArr.Length; i++)
			{
				if (BitConverter.IsLittleEndian)
					Array.Reverse(array, i * 4, 4);
				floatArr[i] = BitConverter.ToSingle(array, i*4) / 0x80000000;
			}
			return floatArr;
		}
		
		public static byte[] ConvertFloatToByte(float[] array)
		{
			byte[] byteArr = new byte[array.Length * 4];
			for (int i = 0; i < array.Length; i++)
			{
				var bytes = BitConverter.GetBytes(array[i] * 0x80000000);
				Array.Copy(bytes, 0, byteArr, i * 4, bytes.Length);
				if (BitConverter.IsLittleEndian)
					Array.Reverse(byteArr, i * 4, 4);
			}
			return byteArr;
		}

		public static int CountCharacterOccurrences(string input, char character)
		{
			return input.Count(c => c == character);
		}

		public static string RemoveSingleNewlines(string text)
		{
			string filteredText = text.Replace("\r", "");
			string[] textParagraphs = filteredText.Split('\n');

			string output = "";
			for (int i = 0; i < textParagraphs.Length; i++)
			{
				string token = textParagraphs[i] + " ";
				output += token;
				textParagraphs[i] = textParagraphs[i].Trim();
				if (textParagraphs[i].Length == 0)
				{
					output += "\n\n";
				}
			}

			return output;
		}

		public static bool IsNameContained(string source, string target)
        {
			if ((source.Length > 0) && (target.Length > 0))
            {
				if (source.ToUpperInvariant().IndexOf(target.ToUpperInvariant()) != -1) return true;
				if (target.ToUpperInvariant().IndexOf(source.ToUpperInvariant()) != -1) return true;
				return false;
			}
			else
            {
				return false;
            }
        }

		public static List<int> FindPatternPositions(string text, string pattern)
		{
			List<int> positions = new List<int>();
			foreach (Match match in Regex.Matches(text, Regex.Escape(pattern)))
			{
				positions.Add(match.Index);
			}
			return positions;
		}

		public static List<int> IndexPatternPositions(string text, string pattern)
		{
			List<int> positions = new List<int>();
			int index = text.IndexOf(pattern);
			while (index != -1)
			{
				positions.Add(index);
				index = text.IndexOf(pattern, index + 1);
			}
			return positions;
		}

		public static void MatchSize(GameObject objectA, GameObject objectB)
		{
			Collider colliderA = objectA.GetComponent<Collider>();
			Collider colliderB = objectB.GetComponent<Collider>();

			if (colliderA != null && colliderB != null)
			{
				Vector3 sizeA = colliderA.bounds.size;
				Vector3 sizeB = colliderB.bounds.size;

				Vector3 currentScale = objectA.transform.localScale;

				Vector3 scaleFactor = new Vector3(
					sizeB.x / sizeA.x,
					sizeB.y / sizeA.y,
					sizeB.z / sizeA.z
				);

				objectA.transform.localScale = Vector3.Scale(currentScale, scaleFactor);
			}
		}

		public static string CeilDecimal(float value, int decimals)
        {
			string output = value.ToString();
			if (output.IndexOf(",") != -1)
            {
				output = output.Replace(",", ".");
			}
			if (output.IndexOf(".") != -1)
            {
				string[] segments = output.Split(".");
				string decimalPart = segments[1];
				if (decimalPart.Length > decimals)
                {
					decimalPart = decimalPart.Substring(0, decimals);
				}
				return segments[0] + "." + decimalPart;
			}
			return output;
        }

		public static string ShortenText(string text, int limit)
        {
			if (text.Length > limit)
            {
				return text.Substring(0, limit) + "...";
            }
			else
            {
				return text;
            }
        }

		public static string RemoveLinesContainingText(string source, string target)
		{
			return string.Join("\n", source.Split('\n')
				.Where(line => !line.Contains(target, StringComparison.OrdinalIgnoreCase))
				.ToArray());
		}

	}
}
