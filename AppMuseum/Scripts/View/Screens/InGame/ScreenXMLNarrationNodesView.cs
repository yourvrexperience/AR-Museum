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
	public class ScreenXMLNarrationNodesView : BaseScreenView, IScreenView
	{
		public const int MAXIMUM_NUMBER_TEXT_SEGMENTS = 6;

		public const string EventScreenXMLNarrationNodesViewRefresh = "EventScreenXMLNarrationNodesViewRefresh";
		
		public const string ScreenName = "ScreenXMLNarrationNodesView";

		[SerializeField] private Button buttonExit;

		[SerializeField] private Button buttonAdd;
		[SerializeField] private Button buttonEdit;
		[SerializeField] private Button buttonDelete;

        [SerializeField] private GameObject ObjectItemPrefab;
		[SerializeField] private SlotManagerView SlotManager;

		private int _currentLevel = -1;
		private NarrationCreator _narrationCreator;
		private List<ItemMultiObjectEntry> _itemsNarrationPOI;
		private NarrationCreatorData _narrationForCurrentPOI;
		private NarrationCreatorToken _selectedEntry;
		private int _idSelected = -1;
		private bool _isPOI = false;
		private EasterEgg _narrationSecret = null;

		public override string NameScreen
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_isPOI = (bool)parameters[0];
			_currentLevel = GameLevelData.Instance.GetLevel(GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel);
			_narrationCreator = new NarrationCreator();			
			if (_isPOI)
			{
				_narrationCreator.LoadNarrationTexts(GameLevelData.Instance.GetLevelNarration(_currentLevel));
				_narrationForCurrentPOI = _narrationCreator.Narration[GameLevelData.Instance.IndexPOILevelEdited];
			}
			else
			{
				_narrationSecret = (EasterEgg)parameters[1];
				_narrationCreator.LoadNarrationTexts(new TextAsset(_narrationSecret.Narration));
				_narrationForCurrentPOI = _narrationCreator.Narration[0];
			}

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			buttonExit.onClick.AddListener(OnButtonExit);

			buttonAdd.onClick.AddListener(OnButtonAdd);
			buttonEdit.onClick.AddListener(OnButtonEdit);
			buttonDelete.onClick.AddListener(OnButtonDelete);
			buttonEdit.gameObject.SetActive(false);
			buttonDelete.gameObject.SetActive(false);

			FillItemsNarration();
		}

        public override void Destroy()
		{
			base.Destroy();

			if (_narrationCreator != null)
			{
				if (_isPOI)
				{
					GameLevelData.Instance.SetLevelNarration(_currentLevel, _narrationCreator.ToXML());
				}
				else
				{
					_narrationSecret.Narration = _narrationCreator.ToXML();
					_narrationSecret = null;
				}
				
				_narrationCreator = null;
			}

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void FillItemsNarration()
		{
			SlotManager.ClearCurrentGameObject(true);
            _itemsNarrationPOI = new List<ItemMultiObjectEntry>();
			_itemsNarrationPOI.Add(new ItemMultiObjectEntry(this.gameObject, 0, _narrationForCurrentPOI.Title));
			for (int i = 0; i < _narrationForCurrentPOI.Segments.Count; i++)
			{
				_itemsNarrationPOI.Add(new ItemMultiObjectEntry(this.gameObject, 1 + i, _narrationForCurrentPOI.Segments[i]));
			}
            SlotManager.Initialize(_narrationCreator.Narration.Count, _itemsNarrationPOI, ObjectItemPrefab);
		}

        private void OnButtonExit()
        {
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        private void OnButtonAdd()
        {
			if (_narrationForCurrentPOI.Segments.Count > MAXIMUM_NUMBER_TEXT_SEGMENTS)
			{
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("screen.narration.nodes.segment.limit"));
				return;
			}

			if (_idSelected == -1)
			{
				_idSelected = _narrationForCurrentPOI.Segments.Count;
			}
			string[] newTextSegment = new string[LanguageController.Instance.SupportedLanguages.Length * 2];
			for (int i = 0; i < newTextSegment.Length; i+=2)
			{
				string tagLanguage = LanguageController.Instance.SupportedLanguages[i/2];
				newTextSegment[i] = tagLanguage;
				newTextSegment[i + 1] = "Text for language ("+tagLanguage+")";
			}
			TextEntry newSegment = new TextEntry(newTextSegment);
			_narrationForCurrentPOI.AddSegment(newSegment, _idSelected);
			FillItemsNarration();
			UnSelectEntry();
        }

        private void OnButtonEdit()
        {
			if (_selectedEntry != null)
			{
				if (_narrationSecret == null)
				{
					ScreenController.Instance.CreateScreen(ScreenXMLEditSegmentView.ScreenName, false, true, _selectedEntry, _narrationForCurrentPOI);
				}
				else
				{
					ScreenController.Instance.CreateScreen(ScreenXMLEditSegmentView.ScreenName, false, true, _selectedEntry, _narrationForCurrentPOI, _narrationSecret);
				}				
				UnSelectEntry();
			}
        }

        private void OnButtonDelete()
        {
			if ((_idSelected != -1) && (_idSelected > 0))
			{
				if (_narrationForCurrentPOI.DeleteSegment(_idSelected - 1))
				{
					int secret = -1;
					if (_narrationSecret != null)
					{
						secret = _narrationSecret.Index;
					}
					GameLevelData.Instance.DeleteSpeech((int)UsersController.Instance.CurrentUser.Id, UsersController.Instance.CurrentUser.PasswordPlain, "", false, secret, (int)GameLevelData.Instance.Age, MainController.Instance.CurrentGameLevel, _narrationForCurrentPOI.Id, _idSelected);
					FillItemsNarration();
					UnSelectEntry();
				}
			}
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventScreenXMLNarrationNodesViewRefresh))
			{
				FillItemsNarration();
			}
		}

		private void UnSelectEntry()
		{
			_idSelected = -1;
			_selectedEntry = null;
			buttonEdit.gameObject.SetActive(false);
			buttonDelete.gameObject.SetActive(false);
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ItemXMLNarrationToken.EventItemXMLNarrationTokenSelected))
			{
				 if ((GameObject)parameters[0] == this.gameObject)
                {
					_idSelected = (int)parameters[2];
					if (_idSelected == -1)
					{
						UnSelectEntry();
					}
					else
					{
						if (_idSelected == 0)
						{
							_selectedEntry = _narrationForCurrentPOI.Title;
							buttonDelete.gameObject.SetActive(false);
						}
						else
						{
							_selectedEntry = _narrationForCurrentPOI.Segments[_idSelected - 1];
							if (_narrationForCurrentPOI.Segments.Count > 1)
							{
								buttonDelete.gameObject.SetActive(true);
							}
						}
						buttonEdit.gameObject.SetActive(true);
					}
				}
			}
        }

		void Update()
		{
		}
	}
}