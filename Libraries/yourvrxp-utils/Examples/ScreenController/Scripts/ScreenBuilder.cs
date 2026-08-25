using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.Utils
{
	public class ScreenBuilder : BaseScreenView, IScreenView
	{
		public const string ScreenName = "ScreenBuilder";

        public const string EventScreenBuilderBuildObject = "EventScreenBuilderBuildObject";

        [SerializeField] private GameObject ObjectItemPrefab;
        [SerializeField] private string[] AssetNamesItemLogos;

        private SlotManagerView _slotManager;
        private ItemMultiObjectEntry _selectedObject = null;
        private Button _btnBuild;

		public override string NameScreen
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

            _content.Find("Title").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.builder.title");
            _content.Find("Button_Close").GetComponent<Button>().onClick.AddListener(CancelPressed);

            _btnBuild = _content.Find("ButtonBuild").GetComponent<Button>();
            _btnBuild.onClick.AddListener(BuildPressed);
            _content.Find("ButtonBuild/Text").GetComponent<TextMeshProUGUI>().text = LanguageController.Instance.GetText("screen.builder.build.selected");

            _slotManager = _content.Find("ListItems").GetComponent<SlotManagerView>();
            List<ItemMultiObjectEntry> sampleItems = new List<ItemMultiObjectEntry>();
            for (int i = 0; i < AssetNamesItemLogos.Length; i++)
            {
				string spriteAssetName = "Image_" + AssetNamesItemLogos[i];
				Sprite itemLogo = ImageUtils.ToSprite(AssetBundleController.Instance.CreateTexture(spriteAssetName));
                ItemMultiObjectEntry data = new ItemMultiObjectEntry(LanguageController.Instance.GetText("screen.builder.object." + AssetNamesItemLogos[i]), itemLogo, AssetNamesItemLogos[i]);
                sampleItems.Add(new ItemMultiObjectEntry(this.gameObject, i, data));
            }
            _slotManager.Initialize(AssetNamesItemLogos.Length, sampleItems, ObjectItemPrefab);

			UIEventController.Instance.Event += OnUIEvent;
		}

		public override void Destroy()
        {
			base.Destroy();

            if (_content != null)
            {
                _content = null;
                if (_slotManager != null)
                {
                    _slotManager.Destroy();
                    _slotManager = null;
                }

                UIEventController.Instance.Event -= OnUIEvent;
            }
        }

        private void CancelPressed()
        {
            UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

        private void BuildPressed()
        {
			SystemEventController.Instance.DispatchSystemEvent(EventScreenBuilderBuildObject, _selectedObject);
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }		

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(ItemSelectable.EventItemObjectSelected))
            {
				if (this.gameObject == (GameObject)parameters[0])
				{
					_selectedObject = (ItemMultiObjectEntry)parameters[3];
				}
            }
        }
	}
}