
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
    public class CustomButton : Button
    {
		public Action<CustomButton> ClickedButton;
		public Action<CustomButton> PointerDownButton;
		public Action<CustomButton> PointerUpButton;
		public Action<CustomButton> PointerEnterButton;
		public Action<CustomButton> PointerExitButton;

		public override void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);
			ClickedButton?.Invoke(this);
		}
		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			PointerDownButton?.Invoke(this);
		}
        public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			PointerEnterButton?.Invoke(this);
		}
        public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
			PointerExitButton?.Invoke(this);
		}
        public override void OnPointerUp(PointerEventData eventData)
		{
			base.OnPointerUp(eventData);
			PointerUpButton?.Invoke(this);
		}
    }
}