using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.UserManagement;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using static yourvrexperience.Narration.NarrationCreator;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
	public class ScreenXMLGenericSelectionView : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenXMLGenericSelectionView";

		[SerializeField] private Button buttonExit;

		[SerializeField] private Button buttonSelect;

        [SerializeField] private GameObject ObjectItemPrefab;
		[SerializeField] private SlotManagerView SlotManager;

		private List<ItemMultiObjectEntry> _items;
		private string[] _itemsValues;
		private int _idSelected = -1;
		private string _valueSelected = "";
		private string _event;

		public override string NameScreen
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_itemsValues = (string[])parameters[0];
			_event = (string)parameters[1];

			UIEventController.Instance.Event += OnUIEvent;

			buttonExit.onClick.AddListener(OnButtonExit);
			buttonSelect.onClick.AddListener(OnButtonSelect);

			SlotManager.ClearCurrentGameObject(true);
            _items = new List<ItemMultiObjectEntry>();
			for (int i = 0; i < _itemsValues.Length; i++)
			{
				_items.Add(new ItemMultiObjectEntry(this.gameObject, i, _itemsValues[i]));
			}
            SlotManager.Initialize(_items.Count, _items, ObjectItemPrefab);
		}

        public override void Destroy()
		{
			base.Destroy();

			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;

			UIEventController.Instance.DelayUIEvent(_event, 0.1f, _valueSelected);
		}

        private void OnButtonExit()
        {
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);			
        }

        private void OnButtonSelect()
        {
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);			
        }

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ItemXMLString.EventItemXMLStringSelected))
			{
				 if ((GameObject)parameters[0] == this.gameObject)
                {
					_idSelected = (int)parameters[2];
					if (_idSelected == -1)
					{
						_valueSelected = "";
					}
					else
					{
						_valueSelected = _itemsValues[_idSelected];
					}
				}
			}
        }
	}
}