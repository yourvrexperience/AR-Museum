using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class SlotManagerView: MonoBehaviour
	{
        public const string EventSlotManagerNewPageLoaded = "EventSlotManagerNewPageLoaded";

        public const int DefaultItemsEachPage = 4;

		[SerializeField] private GameObject ButtonNext;
		[SerializeField] private GameObject ButtonPrevious;

		[SerializeField] private GameObject LoadingIcon;
		[SerializeField] private GameObject LoadingText;

		private GameObject _content;	
		private List<GameObject> _gameObjects = new List<GameObject>();
		private List<ItemMultiObjectEntry> _data;
		private GameObject _slotPrefab;
		private GameObject _createNewPrefab;
		private int _currentPage = 0;
		private int _totalPages = 0;
		private int _itemsEachPage = 0;
		private Transform _imageLoading;
		private Transform _textLoading;

		private Transform _buttonNext;
		private Transform _buttonPrevious;

        private bool _addedListenerNext = false;
        private bool _addedListenerPrevious = false;

		private ScrollRect _scrollRectList = null;

        public List<ItemMultiObjectEntry> Data
        {
            get { return _data; }
        }

		private GameObject[] _exitingItems;

		public bool ScrollVertical 
		{
			get { return _scrollRectList.vertical; }
			set { _scrollRectList.vertical = value; }
		}

		public bool ScrollHorizontal
		{
			get { return _scrollRectList.horizontal; }
			set { _scrollRectList.horizontal = value; }
		}

		public GameObject Content 
		{
			get { return _content; }
		}

        public void InitializeExisting(int itemsEachPage, List<ItemMultiObjectEntry> data, GameObject[] exitingItems)
		{
			_exitingItems = exitingItems;
			Initialize(itemsEachPage, data, null);
		}

        public void Initialize(int itemsEachPage, List<ItemMultiObjectEntry> data, GameObject slotPrefab, GameObject createNewPrefab = null)
		{
            _currentPage = 0;
            _itemsEachPage = itemsEachPage;
			_data = data;
			_slotPrefab = slotPrefab;
			_createNewPrefab = createNewPrefab;

			_scrollRectList = this.gameObject.transform.Find("ScrollContent").GetComponent<ScrollRect>();

			_content = this.gameObject.transform.Find("ScrollContent/Entries").gameObject;
			if (ButtonNext!=null) _buttonNext = ButtonNext.transform;
			if (ButtonPrevious != null) _buttonPrevious = ButtonPrevious.transform;
			if (_buttonNext != null) _buttonNext.gameObject.SetActive(false);
			if (_buttonPrevious != null) _buttonPrevious.gameObject.SetActive(false);

			if (_buttonNext != null)
			{
                if (!_addedListenerNext)
                {
                    _addedListenerNext = true;
                    _buttonNext.GetComponent<Button>().onClick.AddListener(OnNextPressed);
                }
                
				_buttonNext.gameObject.SetActive(false);
			}
			if (_buttonPrevious != null)
			{
                if (!_addedListenerPrevious)
                {
                    _addedListenerPrevious = true;
                    _buttonPrevious.GetComponent<Button>().onClick.AddListener(OnPreviousPressed);
                }				
				_buttonPrevious.gameObject.SetActive(false);
			}

			if (LoadingIcon != null) _imageLoading = LoadingIcon.transform;
			if (LoadingText != null) _textLoading = LoadingText.transform;
			if (_textLoading!=null)
			{
                if (LanguageController.Instance != null)
                {
                    _textLoading.GetComponent<Text>().text = LanguageController.Instance.GetText("message.loading");
                }				
			}
			if (_imageLoading != null) _imageLoading.gameObject.SetActive(true);
			if (_textLoading != null) _textLoading.gameObject.SetActive(true);

			if (_itemsEachPage > 0)
			{
				_totalPages = _data.Count / _itemsEachPage;
			}			

			LoadCurrentPage();
		}

		public void Destroy()
		{
			ClearCurrentGameObject(true);
			_gameObjects = null;
		}

		public void SetVerticalScroll(float value)
        {
			_scrollRectList.verticalNormalizedPosition = value;
		}

		public void SetHorizontalScroll(float value)
		{
			_scrollRectList.horizontalNormalizedPosition = value;
		}

		public void ApplyGenericAction(params object[] parameters)
		{
			for (int i = 0; i < _gameObjects.Count; i++)
			{
				if (_gameObjects[i] != null)
				{
					ISlotView itemSlotView = _gameObjects[i].GetComponent<ISlotView>();
					itemSlotView.ApplyGenericAction(parameters);
				}
			}
		}

		public void ClearCurrentGameObject(bool resetPage)
		{
            for (int i = 0; i < _gameObjects.Count; i++)
			{
				if (_gameObjects[i] != null)
				{
					_gameObjects[i].GetComponent<ISlotView>().Destroy();
					GameObject.Destroy(_gameObjects[i]);
					_gameObjects[i] = null;
				}
			}
			_gameObjects.Clear();

            if (_imageLoading != null) _imageLoading.gameObject.SetActive(true);
			if (_textLoading != null)
			{
				if (LanguageController.Instance != null)
				{
					_textLoading.GetComponent<Text>().text = LanguageController.Instance.GetText("text.loading");
				}				
				_textLoading.gameObject.SetActive(true);
                if (_buttonNext != null) _buttonNext.gameObject.SetActive(false);
                if (_buttonPrevious != null) _buttonPrevious.gameObject.SetActive(false);
            }

            if (resetPage)
            {
                _currentPage = 0;
				if (_scrollRectList != null)
				{
					_scrollRectList.verticalNormalizedPosition = 1;
				}				
            }
        }

        public void LoadCurrentPage()
		{
			if ((_buttonNext != null) && (_buttonPrevious != null) && (_data.Count > _itemsEachPage))
			{
				ClearCurrentGameObject(false);
				_scrollRectList.verticalNormalizedPosition = 1;

				int initialItem = _currentPage * _itemsEachPage;
				int finalItem = initialItem + _itemsEachPage;

				int i = initialItem;
				for (i = initialItem; i < finalItem; i++)
				{
					if (i < _data.Count)
					{
						GameObject newSlot;
						if (_slotPrefab != null)
						{
							newSlot = yourvrexperience.Utils.Utilities.AddChild(_content.transform, _slotPrefab);
						}
						else
						{
							newSlot = _exitingItems[i];
						}						 
						newSlot.GetComponent<ISlotView>().Initialize(_data[i]);
						_gameObjects.Add(newSlot);
					}
				}
				bool endReached = (i >= _data.Count);

				if (_buttonNext != null) _buttonNext.gameObject.SetActive(false);
				if (_buttonPrevious != null) _buttonPrevious.gameObject.SetActive(false);

				if (initialItem == 0)
				{
					if (_data.Count > _itemsEachPage)
					{
						if (_buttonNext != null)
						{
							_buttonNext.gameObject.SetActive(true);
						}
					}
					if (_buttonPrevious != null)
					{
						_buttonPrevious.gameObject.SetActive(false);
					}
				}
				else
				{
					if (endReached)
					{
						if ((_buttonPrevious != null) && (initialItem != 0))
						{
							_buttonPrevious.gameObject.SetActive(true);
						}
					}
					else
					{
						if (_buttonNext != null) _buttonNext.gameObject.SetActive(true);
						if (_buttonPrevious != null) _buttonPrevious.gameObject.SetActive(true);
					}
				}
			}
			else
			{
				if (_buttonNext != null) _buttonNext.gameObject.SetActive(false);
				if (_buttonPrevious != null) _buttonPrevious.gameObject.SetActive(false);

				ClearCurrentGameObject(false);
				for (int i = 0; i < _data.Count; i++)
				{                
					GameObject newSlot;
					if (_slotPrefab != null)
					{
						newSlot = yourvrexperience.Utils.Utilities.AddChild(_content.transform, _slotPrefab);
					}
					else
					{
						newSlot = _exitingItems[i];
					}	
					newSlot.GetComponent<ISlotView>().Initialize(_data[i]);
					_gameObjects.Add(newSlot);
				}
			}

			if (_createNewPrefab != null)
			{
				if ((_currentPage + 1) * _itemsEachPage >= _data.Count)
				{
					GameObject newSlot = yourvrexperience.Utils.Utilities.AddChild(_content.transform, _createNewPrefab);
					newSlot.GetComponent<ISlotView>().Initialize();
					_gameObjects.Add(newSlot);
				}
			}
			if (_gameObjects.Count == 0)
			{
				DisplayNoRecords();
			}
			else
			{
				if (_imageLoading != null) _imageLoading.gameObject.SetActive(false);
                if (_textLoading != null) _textLoading.gameObject.SetActive(false);
			}

			UIEventController.Instance.DispatchUIEvent(EventSlotManagerNewPageLoaded, this.gameObject, _currentPage);
		}

		public void AddItem(ItemMultiObjectEntry item)
		{
			_data.Add(item);
			_itemsEachPage++;
			_totalPages = 1;
			GameObject newSlot = yourvrexperience.Utils.Utilities.AddChild(_content.transform, _slotPrefab);
			newSlot.GetComponent<ISlotView>().Initialize(item);
			_gameObjects.Add(newSlot);
		}

		public void RemoveItem(ItemMultiObjectEntry item)
		{
			if (_data.Remove(item))
            {
				_itemsEachPage--;
				_totalPages = 1;
				LoadCurrentPage();
			}
		}

		public void ClearData()
		{
			_currentPage = 0;
			_data.Clear();
		}

		public void DisplayNoRecords()
        {
			if (LoadingIcon != null) _imageLoading = LoadingIcon.transform;
			if (LoadingText != null) _textLoading = LoadingText.transform;
			if (ButtonNext != null) _buttonNext = ButtonNext.transform;
			if (ButtonPrevious != null) _buttonPrevious = ButtonPrevious.transform;

			if (_imageLoading != null) _imageLoading.gameObject.SetActive(true);
			if (_textLoading != null) _textLoading.GetComponent<Text>().text = LanguageController.Instance.GetText("message.no.records");
			if (_buttonNext != null) _buttonNext.gameObject.SetActive(false);
			if (_buttonPrevious != null) _buttonPrevious.gameObject.SetActive(false);
		}

		public void OnPreviousPressed()
		{
			_currentPage--;
			if (_currentPage < 0) _currentPage = 0;
			LoadCurrentPage();
		}

		public void OnNextPressed()
		{
			_currentPage++;
			if (_currentPage * _itemsEachPage >= _data.Count) _currentPage--;
			LoadCurrentPage();
		}

        public void HideAllItems(int indexException = -1)
        {
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                if (indexException != i)
                {
                    _gameObjects[i].SetActive(false);
                }
                else
                {
                    _gameObjects[i].SetActive(true);
                }
            }
        }

        public void ShowAllItems()
        {
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                _gameObjects[i].SetActive(true);
            }
        }

        public bool CheckSlotExisting(GameObject slot)
		{
			if (slot == null) return false;

			for (int i = 0; i < _gameObjects.Count; i++)
			{
				if (slot == _gameObjects[i])
				{
					return true;
				}
			}
			return false;
		}

		public void SelectItem(int indexItem, bool dispatchEvent)
		{
			if ((indexItem >= 0) && (indexItem < _gameObjects.Count))
			{
				_gameObjects[indexItem].GetComponent<ISlotView>().ItemSelected(dispatchEvent);
			}
		}
			
	}
}