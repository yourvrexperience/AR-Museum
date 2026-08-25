
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace yourvrexperience.Utils
{
    public class CustomInput : TMP_InputField
    {
        public delegate void FocusEvent();
        public event FocusEvent OnFocusEvent;
        public event FocusEvent OnFocusDownEvent;
        public event FocusEvent OnFocusOverEvent;
        public event FocusEvent OnFocusOutEvent;

        public void DispatchFocusEvent()
        {
            if (OnFocusEvent != null) OnFocusEvent();
        }
        public void DispatchFocusDownEvent()
        {
            if (OnFocusDownEvent != null) OnFocusDownEvent();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            DispatchFocusDownEvent();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            DispatchFocusEvent();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (OnFocusOverEvent != null) OnFocusOverEvent();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (OnFocusOutEvent != null) OnFocusOutEvent();
        }

    }
}