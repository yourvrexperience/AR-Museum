using yourvrexperience.Utils;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using yourvrexperience.Narration;
using static yourvrexperience.template6dof.LevelView;
using System.Collections.Generic;

namespace yourvrexperience.template6dof
{
	public class PanelPOIListView : MonoBehaviour
	{
		[SerializeField] private SlotManagerView slotManagerPOIS;
		[SerializeField] private GameObject poiMuseumItemPrefab;
		[SerializeField] private TextMeshProUGUI titlePanel;

		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			this.gameObject.SetActive(true);

			LoadListPOIsLevel();
		}

        void OnDestroy()
		{
			if (slotManagerPOIS != null)
			{
				slotManagerPOIS.Destroy();
				slotManagerPOIS = null;
			}
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void LoadListPOIsLevel()
		{
			slotManagerPOIS.ClearCurrentGameObject(true);
			if (GameLevelData.Instance.EditPOIsMode)
			{
				titlePanel.text = "POIs";

				POIData[] poisMuseum = MainController.Instance.LevelView.GetPOIS();
				List<ItemMultiObjectEntry> poisEntries = new List<ItemMultiObjectEntry>();		
				int totalEntriesMuseum = 0;
				if (poisMuseum != null)
				{
					totalEntriesMuseum = poisMuseum.Length;
					for (int i = 0; i < poisMuseum.Length; i++)
					{
						ItemMultiObjectEntry data = new ItemMultiObjectEntry((i+1).ToString(), poisMuseum[i].Root.GetComponent<POIBaseView>());
						poisEntries.Add(new ItemMultiObjectEntry(this.gameObject, i, data));
					}
				}
				slotManagerPOIS.Initialize(totalEntriesMuseum, poisEntries, poiMuseumItemPrefab);
			}
			else
			{
				titlePanel.text = "Secrets";

				EasterEgg[] secretsMuseum = MainController.Instance.LevelView.GetEasterEggs();
				List<ItemMultiObjectEntry> secretsEntries = new List<ItemMultiObjectEntry>();	
				int totalSecretsMuseum = 0;	
				if (secretsMuseum != null)
				{
					totalSecretsMuseum = secretsMuseum.Length;
					for (int i = 0; i < secretsMuseum.Length; i++)
					{
						ItemMultiObjectEntry data = new ItemMultiObjectEntry((i+1).ToString(), secretsMuseum[i].Target.GetComponent<EasterEggBaseView>());
						secretsEntries.Add(new ItemMultiObjectEntry(this.gameObject, i, data));
					}
				}
				slotManagerPOIS.Initialize(totalSecretsMuseum, secretsEntries, poiMuseumItemPrefab);				
			}
		}

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataEditModeChanged))
			{
				LoadListPOIsLevel();
			}
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataRefreshPOILevel))
			{
				LoadListPOIsLevel();
			}
			if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(ItemPOIMuseum.EventItemPOIMuseumSelected))
			{
				int poiIndex = (int)parameters[2];
				if (GameLevelData.Instance.EditPOIsMode)
				{
					POIBaseView poiData = (POIBaseView)parameters[3];
					SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataSelectedIndexPOI, poiIndex, poiData.Index);
				}
				else
				{
					EasterEggBaseView secretData = (EasterEggBaseView)parameters[3];
					SystemEventController.Instance.DispatchSystemEvent(GameLevelData.EventGameLevelDataSelectedIndexPOI, poiIndex, secretData.Index);
				}
			}
        }
	}
}
