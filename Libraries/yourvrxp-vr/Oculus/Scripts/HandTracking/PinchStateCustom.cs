#if ENABLE_OCULUS
using Oculus.Interaction;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace yourvrexperience.VR
{
    /// <summary>
    /// Manages pinch state, including if an object is being focused via something
    /// like a ray (or not).
    /// </summary>
    public class PinchStateCustom
    {
        private const float PINCH_STRENGTH_THRESHOLD = 1.0f;

        private enum MyPinchState
        {
            None = 0,
            PinchDown,
            PinchStay,
            PinchUp
        }

#if ENABLE_OCULUS
        private MyPinchState _currPinchState;

        /// <summary>
        /// We want a pinch up and down gesture to be done **while** an object is focused.
        /// We don't want someone to pinch, unfocus an object, then refocus before doing
        /// pinch up. We also want to avoid focusing a different interactable during this process.
        /// While the latter is difficult to do since a person might focus nothing before
        /// focusing on another interactable, it's theoretically possible.
        /// </summary>
        public bool PinchUpAndDownOnFocusedObject
        {
            get
            {
                return _currPinchState == MyPinchState.PinchUp;
            }
        }

        public bool PinchSteadyOnFocusedObject
        {
            get
            {
                return _currPinchState == MyPinchState.PinchStay;
            }
        }

        public bool PinchDownOnFocusedObject
        {
            get
            {
                return _currPinchState == MyPinchState.PinchDown;
            }
        }

        public PinchStateCustom()
        {
            _currPinchState = MyPinchState.None;
        }

        public void UpdateState(OVRHand hand, bool _displayLog)
        {
            float pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
            bool isPinching = Mathf.Abs(PINCH_STRENGTH_THRESHOLD - pinchStrength) < Mathf.Epsilon;
            var oldPinchState = _currPinchState;

            // if (_displayLog) UIEventController.Instance.DelayUIEvent(ScreenDebugLogView.EVENT_SCREEN_DEBUGLOG_NEW_TEXT, 3, true, "old[" + oldPinchState.ToString() + "]::strength[" + pinchStrength + "]::isPinching[" + isPinching + "]::IsDataValid[" + hand.IsDataValid + "]");

            switch (oldPinchState)
            {
                case MyPinchState.PinchUp:
                    // can only be in pinch up for a single frame, so consider
                    // next frame carefully
                    if (isPinching)
                    {
                        _currPinchState = MyPinchState.PinchDown;
                    }
                    else
                    {
                        _currPinchState = MyPinchState.None;
                    }
                    break;
                case MyPinchState.PinchStay:
                    if (!isPinching)
                    {
                        _currPinchState = MyPinchState.PinchUp;
                    }
                    break;
                // pinch down lasts for a max of 1 frame. either go to pinch stay or up
                case MyPinchState.PinchDown:
                    _currPinchState = isPinching ? MyPinchState.PinchStay : MyPinchState.PinchUp;
                    break;
                default:
                    if (isPinching)
                    {
                        _currPinchState = MyPinchState.PinchDown;
                        // this is the interactable that must be focused through out the pinch up and down
                        // gesture.
                    }
                    break;
            }
        }
#endif
    }
}