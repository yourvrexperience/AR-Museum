using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace yourvrexperience.VR
{
    /// <summary>
    /// Resolves the left/right controllers by InputDeviceCharacteristics instead of by XRNode,
    /// caches them, and re-resolves when a device connects, disconnects or goes invalid.
    ///
    /// Why: InputDevices.GetDeviceAtXRNode() returns an invalid device whenever the provider has
    /// not associated the controller with a node yet (very common for the first frames of a
    /// session, after taking the controllers out of sleep, and on some OpenXR runtimes always).
    /// TryGetFeatureValue() on an invalid device silently returns false, which reads exactly like
    /// "the user is not pressing anything".
    /// </summary>
    public class XRControllerDevices
    {
        private const float RefreshIntervalSeconds = 1f;

        private InputDevice _left;
        private InputDevice _right;
        private float _timer;
        private bool _subscribed;

        private static readonly List<InputDevice> Buffer = new List<InputDevice>();

        public InputDevice Left { get { return _left; } }
        public InputDevice Right { get { return _right; } }

        public bool LeftIsValid { get { return _left.isValid; } }
        public bool RightIsValid { get { return _right.isValid; } }

        public void Initialize()
        {
            if (!_subscribed)
            {
                InputDevices.deviceConnected += OnDeviceChanged;
                InputDevices.deviceDisconnected += OnDeviceChanged;
                _subscribed = true;
            }
            Resolve();
        }

        public void Dispose()
        {
            if (_subscribed)
            {
                InputDevices.deviceConnected -= OnDeviceChanged;
                InputDevices.deviceDisconnected -= OnDeviceChanged;
                _subscribed = false;
            }
        }

        private void OnDeviceChanged(InputDevice device)
        {
            Resolve();
        }

        /// <summary>Call once per frame, at the top of Update().</summary>
        public void Tick()
        {
            if (_left.isValid && _right.isValid)
            {
                _timer = 0f;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer < RefreshIntervalSeconds) return;
            _timer = 0f;
            Resolve();
        }

        public void Resolve()
        {
            _left = FindController(InputDeviceCharacteristics.Left);
            _right = FindController(InputDeviceCharacteristics.Right);
        }

        private static InputDevice FindController(InputDeviceCharacteristics side)
        {
            // Preferred: a real held controller on that side.
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand | side, Buffer);
            if (Buffer.Count > 0) return Buffer[0];

            // Some runtimes do not report HeldInHand.
            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | side, Buffer);
            if (Buffer.Count > 0) return Buffer[0];

            // Last resort: whatever the provider mapped to the node.
            return InputDevices.GetDeviceAtXRNode(
                side == InputDeviceCharacteristics.Left ? XRNode.LeftHand : XRNode.RightHand);
        }

        private InputDevice DeviceFor(XR_HAND hand)
        {
            return hand == XR_HAND.left ? _left : _right;
        }

        /// <summary>Returns false (and value=false) when the device or the feature is unavailable.</summary>
        public bool TryGetBool(XR_HAND hand, InputFeatureUsage<bool> usage, out bool value)
        {
            value = false;
            InputDevice device = DeviceFor(hand);
            if (!device.isValid) return false;
            return device.TryGetFeatureValue(usage, out value);
        }

        public bool TryGetVector2(XR_HAND hand, InputFeatureUsage<Vector2> usage, out Vector2 value)
        {
            value = Vector2.zero;
            InputDevice device = DeviceFor(hand);
            if (!device.isValid) return false;
            return device.TryGetFeatureValue(usage, out value);
        }

        public bool TryGetFloat(XR_HAND hand, InputFeatureUsage<float> usage, out float value)
        {
            value = 0f;
            InputDevice device = DeviceFor(hand);
            if (!device.isValid) return false;
            return device.TryGetFeatureValue(usage, out value);
        }
    }
}
