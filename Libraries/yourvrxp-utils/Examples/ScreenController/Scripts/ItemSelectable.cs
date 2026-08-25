using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
    public class ItemSelectable : MonoBehaviour, ISlotView
    {
        public const string EventItemObjectSelected = "EventItemObjectSelected";

        private GameObject _parent;
        private int _index;
        private ItemMultiObjectEntry _data;
        private Image _background;
        private bool _selected = false;

        public int Index
        {
            get { return _index; }
        }
        public ItemMultiObjectEntry Data
        {
            get { return _data; }
        }
        public virtual bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                if (_selected)
                {
                    _background.color = Color.magenta;
                }
                else
                {
                    _background.color = Color.white;
                }
            }
        }

        public void Initialize(params object[] parameters)
        {
            _parent = (GameObject)((ItemMultiObjectEntry)parameters[0]).Objects[0];
            _index = (int)((ItemMultiObjectEntry)parameters[0]).Objects[1];
            _data = (ItemMultiObjectEntry)((ItemMultiObjectEntry)parameters[0]).Objects[2];

            transform.Find("Name").GetComponent<TextMeshProUGUI>().text = (string)_data.Objects[0];
            transform.Find("Logo").GetComponent<Image>().sprite = (Sprite)_data.Objects[1];

            _background = transform.GetComponent<Image>();
            transform.GetComponent<Button>().onClick.AddListener(ButtonPressed);

            UIEventController.Instance.Event += OnUIEvent;
        }

        void OnDestroy()
        {
            Destroy();
        }

        public bool Destroy()
        {
            if (_parent != null)
            {
                _parent = null;
                if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ApplyGenericAction(params object[] parameters)
        {
        }

        public void ButtonPressed()
        {
            ItemSelected();
        }

        public void ItemSelected(bool dispatchEvent = true)
        {
            Selected = !Selected;
			UIEventController.Instance.DispatchUIEvent(EventItemObjectSelected, _parent, this.gameObject, (Selected ? _index : -1), _data, dispatchEvent);
        }

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
            if (nameEvent.Equals(EventItemObjectSelected))
            {
                if ((GameObject)parameters[0] == _parent)
                {
                    if ((GameObject)parameters[1] != this.gameObject)
                    {
                        Selected = false;
                    }
                }
            }
		}
	}
}