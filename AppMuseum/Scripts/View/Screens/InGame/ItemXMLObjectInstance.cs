using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.GameLevelData;
using static yourvrexperience.Narration.NarrationController;
using static yourvrexperience.Narration.NarrationCreator;

namespace yourvrexperience.template6dof
{
    public class ItemXMLObjectInstance : MonoBehaviour, ISlotView
    {
        public const string EventItemXMLObjectInstanceSelected = "EventItemXMLObjectInstanceSelected";

        private GameObject _parent;
        private int _index;
        private ItemMultiObjectEntry _data;
        private Image _background;
        private bool _selected = false;
        private  NarrationObject _narrationObject;
        
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
            _narrationObject = (NarrationObject)((ItemMultiObjectEntry)parameters[0]).Objects[3];

            transform.Find("Name").GetComponent<TextMeshProUGUI>().text = (string)((ItemMultiObjectEntry)parameters[0]).Objects[2];

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
                _narrationObject = null;
                if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ButtonPressed()
        {
            ItemSelected();
        }


        public void ApplyGenericAction(params object[] parameters)
        {
            ItemSelected();
        }

        public void ItemSelected(bool dispatchEvent = true)
        {
            Selected = !Selected;
			UIEventController.Instance.DispatchUIEvent(EventItemXMLObjectInstanceSelected, _parent, this.gameObject, (Selected ? _index : -1), _narrationObject);
        }

		private void OnUIEvent(string nameEvent, object[] parameters)
		{
            if (nameEvent.Equals(EventItemXMLObjectInstanceSelected))
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