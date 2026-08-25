using System;
using UnityEngine;

namespace yourvrexperience.Utils
{
    [Serializable]
    public class Waypoint
    {
		public const string SeparatorWaypoint = "<w>";

        public enum ActionsPatrol { GO = 0, STAY, LOOK }
        public ActionsPatrol Action;
        public GameObject Target;
        public Vector3 Position;
        public float Duration;

		public Waypoint()
		{

		}

		public Waypoint(string data)
		{
			UnPack(data);
		}

        public Waypoint Clone()
        {
            Waypoint output = new Waypoint();
            output.Target = new GameObject();
            if (Target != null)
            {
                output.Target.transform.position = Target.transform.position;
                GameObject.Destroy(output.Target, 2);
            }
            output.Position = Position;
            output.Duration = Duration;
            output.Action = Action;
            return output;
        }

        public void Set(Waypoint _waypoint)
        {
            Target = _waypoint.Target;
            Position = _waypoint.Position;
            Duration = _waypoint.Duration;
            Action = _waypoint.Action;
        }

		public override string ToString()
		{
			if (Target != null)
			{
				Position = Target.transform.position;
			}			
			return (int)Action + SeparatorWaypoint + yourvrexperience.Utils.Utilities.SerializeVector3(Position) + SeparatorWaypoint + Duration;
		}

		public void UnPack(string data)
		{
			string[] dataArray = data.Split(new String[]{SeparatorWaypoint}, data.Length, StringSplitOptions.None);
			Action = (ActionsPatrol)int.Parse(dataArray[0]);
			Position = yourvrexperience.Utils.Utilities.DeserializeVector3(dataArray[1]);
			Duration = float.Parse(dataArray[2]);
		}

    }
}