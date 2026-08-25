/*
Copyright 2021 Heroic Labs

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
#if ENABLE_NAKAMA
using System.Collections.Generic;
using Nakama.TinyJson;
using UnityEngine;
using yourvrexperience.Utils;

namespace yourvrexperience.Networking
{
	public static class MatchDataJson
	{
		public const string EventNameKey = "eventName";
		public const string OriginKey = "origin";
		public const string TargetKey = "target";
		public const string DataKey = "data";

		public const string OwnerKey = "owner";
		public const string UidKey = "uid";
		public const string IndexKey = "index";
		public const string PositionKey = "position";
		public const string RotationKey = "rotation";
		public const string ScaleKey = "scale";

		public static string AssignUIDS(string[] uids)
		{
			var values = new Dictionary<string, string>();

			for (int i = 0; i < uids.Length; i++)
            {
				values.Add(uids[i], i.ToString());
			}

			return values.ToJson();
		}

		public static string Message(string eventName, int origin, int target, params object[] data)
		{
			string finalData = "";
			for (int i = 0; i < data.Length; i++)
            {
				if (finalData.Length > 0)
                {
					finalData += NetworkController.TokenSeparatorEvents;
				}

				finalData += (string)data[i];
			}

			var values = new Dictionary<string, string>()
			{
				{ EventNameKey, eventName },
				{ OriginKey, origin.ToString() },
				{ TargetKey, target.ToString() },
				{ DataKey, finalData }
			};

			return values.ToJson();
		}

		public static string Transform(int owner, int uid, int index, Vector3 position, Quaternion rotation, Vector3 scale)
		{
			var values = new Dictionary<string, string>()
			{
				{ OwnerKey, owner.ToString() },
				{ UidKey, uid.ToString() },
				{ IndexKey, index.ToString() },
				{ PositionKey, yourvrexperience.Utils.Utilities.SerializeVector3(position) },
				{ RotationKey, yourvrexperience.Utils.Utilities.SerializeQuaternion(rotation) },
				{ ScaleKey, yourvrexperience.Utils.Utilities.SerializeVector3(scale) }
			};

			return values.ToJson();
		}
	}
}
#endif