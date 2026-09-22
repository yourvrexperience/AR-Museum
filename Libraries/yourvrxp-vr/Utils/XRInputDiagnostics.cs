using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace yourvrexperience.VR
{
    /// <summary>
    /// Drop this on any GameObject in the Quest scene, build, and read the output with:
    ///     adb logcat -s Unity:V
    ///
    /// It answers the only question that matters right now: does the XR input subsystem
    /// see the Touch controllers at all, and which feature usages do they expose?
    ///
    ///  - "devices=1" (only the HMD)      -> XR Plug-in Management / OpenXR settings problem
    ///                                       (no interaction profile bound). Nothing in C# will fix it.
    ///  - devices present but node.RightHand valid=False
    ///                                    -> the devices are not mapped to XRNode, so
    ///                                       GetDeviceAtXRNode() fails. Query by characteristics instead.
    ///  - devices valid but no "primaryButton"/"gripButton" in the feature list
    ///                                    -> the runtime fell back to a minimal profile
    ///                                       (e.g. KHR Simple Controller: only select + menu).
    /// </summary>
    public class XRInputDiagnostics : MonoBehaviour
    {
        public float IntervalSeconds = 2f;

        private float _timer;
        private readonly List<InputDevice> _devices = new List<InputDevice>();

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < IntervalSeconds) return;
            _timer = 0f;

            // NOTE: Unity truncates every Debug.Log message at 1024 characters on Android,
            // so each line is logged separately. A single multi-line StringBuilder loses
            // whatever falls past that limit (which is what hid the node lines earlier).
            InputDevices.GetDevices(_devices);
            Debug.Log("[XRDIAG] total devices = " + _devices.Count);

            foreach (InputDevice device in _devices)
            {
                Debug.Log("[XRDIAG]  name='" + device.name +
                          "' valid=" + device.isValid +
                          " characteristics=" + device.characteristics);
            }

            LogNode(XRNode.LeftHand);
            LogNode(XRNode.RightHand);
        }

        private void LogNode(XRNode node)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            Debug.Log("[XRDIAG]  node " + node + " -> valid=" + device.isValid + " name='" + device.name + "'");

            if (!device.isValid) return;

            bool trigger, grip, primary, secondary;
            bool hasTrigger = device.TryGetFeatureValue(CommonUsages.triggerButton, out trigger);
            bool hasGrip = device.TryGetFeatureValue(CommonUsages.gripButton, out grip);
            bool hasPrimary = device.TryGetFeatureValue(CommonUsages.primaryButton, out primary);
            bool hasSecondary = device.TryGetFeatureValue(CommonUsages.secondaryButton, out secondary);

            Debug.Log("[XRDIAG]   " + node +
                      " trigger(" + hasTrigger + "," + trigger + ")" +
                      " grip(" + hasGrip + "," + grip + ")" +
                      " primary(" + hasPrimary + "," + primary + ")" +
                      " secondary(" + hasSecondary + "," + secondary + ")");
        }
    }
}