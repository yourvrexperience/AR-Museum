using UnityEngine;

namespace yourvrexperience.Utils
{
    public interface ICameraPlayer
    {
		Vector3 PositionBase { get; }
        Vector3 PositionCamera { get; set; }
        Vector3 ForwardCamera { get; set; }
        GameObject GetGameObject();
        bool IsOwner();
    }
}