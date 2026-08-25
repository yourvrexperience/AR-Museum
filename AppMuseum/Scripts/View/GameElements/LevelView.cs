using yourvrexperience.Utils;
using UnityEngine;
using System;
using System.Collections.Generic;
using yourvrexperience.Networking;
using yourvrexperience.Narration;
using static yourvrexperience.Narration.GameLevelData;
using static yourvrexperience.Narration.NarrationCreator;
#if !UNITY_WEBGL && ENABLE_MAXST
using maxstAR;
#endif

namespace yourvrexperience.template6dof
{
	public class LevelView : MonoBehaviour
	{
		public const string EventLevelViewStarted = "EventLevelViewStarted";
		public const string EventLevelViewDestroy = "EventLevelViewDestroy";		
		public const string EventLevelViewPlayEasterEgg = "EventLevelViewPlayEasterEgg";
		public const string EventLevelViewDestroyEasterEgg = "EventLevelViewDestroyEasterEgg";
		public const string EventLevelViewUnlockEasterEggs = "EventLevelViewUnlockEasterEggs";
		public const string EventLevelViewUnlockReplayPOI = "EventLevelViewUnlockReplayPOI";
		public const string EventLevelViewDelayToTimeoutDatabase = "EventLevelViewDelayToTimeoutDatabase";
		public const string EventLevelViewEnablePOIsForEdit = "EventLevelViewEnablePOIsForEdit";
		public const string EventLevelViewForcePOIsVisible = "EventLevelViewForcePOIsVisible";

		public const string EventEasterEggVideo1 = "EventEasterEggVideo1";
		public const string EventEasterEggPhotos1 = "EventEasterEggPhotos1";
		public const string EventEasterEggEnding = "EventEasterEggEnding";

		public const string EventEnableEasterEggVideo1 = "EventEnableEasterEggVideo1";
		public const string EventEnableEasterEggPhotos1 = "EventEnableEasterEggPhotos1";
		public const string EventEnableEasterEggEnding = "EventEnableEasterEggEnding";

		[Serializable]
		public class POIData
		{
			public GameObject Root;
			public GameObject GOPosition;
			public string Description;
			public string EventStart;
			public string EventEnd;
			public float DetectionDistance = -1;
			public GameObject ExtraData;
			public POIReplayView ReplayPOI;

			public POIData(GameObject root, GameObject sphere, POIReplayView replayPOI, int index)
			{
				Root = root;
				GOPosition = sphere;
				ReplayPOI = replayPOI;
				ReplayPOI.SetPOIIndex(index);
			}

			public void Destroy()
			{
				ReplayPOI.DeActivate();
				if (Root != null)
				{
					GameObject.Destroy(Root);
				}
				GOPosition = null;
				Root = null;
				ExtraData = null;
			}
		}

		[Serializable]
		public class EasterEgg
		{
			public int Index;
			public GameObject Target;
			public GameObject Reference;
			public GameObject Star;
			public MeshRenderer MaterialStarUndiscovered;
			public MeshRenderer MaterialStarDiscovered;
			public string ActivationEvent;
			public string FinishedEvent;			
			public string Title;
			public string Description;
			public bool Appeared = false;
			public bool Enabled = false;
			public bool Active = false;
			public bool Played = false;
			public string Narration = "";
			private NarrationCreatorData _narrationForCurrentPOI;

			public EasterEgg(GameObject target, GameObject reference, int index)
			{
				Index = index;
				Target = target;
				Reference = reference;
			}

			public void Destroy()
			{
				if (Target != null)
				{
					GameObject.Destroy(Target);
				}
				Target = null;
				Reference = null;
			}

			public void SetNarration(string narration)
			{
				Narration = narration;

				NarrationCreator narrationCreator = new NarrationCreator();
				if ((Narration != null) && (Narration.Length > 0))
				{
					narrationCreator.LoadNarrationTexts(new TextAsset(Narration));
					_narrationForCurrentPOI = narrationCreator.Narration[0];
					ActivationEvent = _narrationForCurrentPOI.Title.StartEvent;
					FinishedEvent = _narrationForCurrentPOI.Title.EndEvent;
				}
			}

			public void Activation()
            {
				Enabled = true;
				Target.SetActive(true);
				Star.SetActive(true);
				if (Played)
				{
					MaterialStarUndiscovered.gameObject.SetActive(false);
					MaterialStarDiscovered.gameObject.SetActive(true);
					Color currColor = MaterialStarDiscovered.material.color;
					currColor.a = 0.2f;
					MaterialStarDiscovered.material.color = currColor;
				}
				else
				{
					MaterialStarUndiscovered.gameObject.SetActive(true);
					MaterialStarDiscovered.gameObject.SetActive(false);
					Color currColor = MaterialStarUndiscovered.material.color;
					currColor.a = 0.2f;
					MaterialStarUndiscovered.material.color = currColor;
				}
			}

			public void ShowStar()
            {
				Target.SetActive(true);
				Star.SetActive(true);
				if (Played)
				{
					MaterialStarUndiscovered.gameObject.SetActive(false);
					MaterialStarDiscovered.gameObject.SetActive(true);
					Color currColor = MaterialStarDiscovered.material.color;
					currColor.a = 1f;
					MaterialStarDiscovered.material.color = currColor;
				}
				else
				{
					MaterialStarUndiscovered.gameObject.SetActive(true);
					MaterialStarDiscovered.gameObject.SetActive(false);
					Color currColor = MaterialStarUndiscovered.material.color;
					currColor.a = 1f;
					MaterialStarUndiscovered.material.color = currColor;
				}
			}

			public void SetActive(bool value)
            {
				Active = value;
				Color currColor;
				if (Played)
				{
					MaterialStarUndiscovered.enabled = false;
					MaterialStarDiscovered.enabled = true;
					currColor = MaterialStarDiscovered.material.color;
					if (!Active)
					{					
						currColor.a = 0.2f;					
					}
					else
					{
						currColor.a = 1f;
					}
					MaterialStarDiscovered.material.color = currColor;
				}
				else
				{
					MaterialStarUndiscovered.enabled = true;
					MaterialStarDiscovered.enabled = false;
					currColor = MaterialStarUndiscovered.material.color;
					if (!Active)
					{
						currColor.a = 0.2f;					
					}
					else
					{
						currColor.a = 1f;
					}
					MaterialStarUndiscovered.material.color = currColor;
				}				
			}

			public void SetPlayed(bool played)
			{
				Played = played;
				if (Played)
				{
					MaterialStarUndiscovered.gameObject.SetActive(false);
					MaterialStarDiscovered.gameObject.SetActive(true);
					Color currColor = MaterialStarDiscovered.material.color;
					currColor.a = 1f;
					MaterialStarDiscovered.material.color = currColor;
				}
				else
				{
					MaterialStarUndiscovered.gameObject.SetActive(true);
					MaterialStarDiscovered.gameObject.SetActive(false);
					Color currColor = MaterialStarUndiscovered.material.color;
					currColor.a = 1f;
					MaterialStarUndiscovered.material.color = currColor;
				}
			}

			public string GetTitle()
			{
				if (_narrationForCurrentPOI != null)
				{
					return _narrationForCurrentPOI.Title.GetCurrentLanguageMessage();
				}
				else
				{
					return LanguageController.Instance.GetText("screen.easter.egg.title.discovered");
				}				
			}

			public string GetDescription()
			{
				return LanguageController.Instance.GetText("screen.easter.egg.description.press.to.start");
			}
		}

		[SerializeField] private GameObject tourGuideWorld;
		[SerializeField] private GameObject content;
		[SerializeField] private GameObject initialPosition;
		[SerializeField] private GameObject floor;
		[SerializeField] private GameObject navigationFloor;
		[SerializeField] private POIData[] POIS;
		[SerializeField] private EasterEgg[] easterEggs;
		[SerializeField] private GameObject Corner1;
		[SerializeField] private GameObject Corner2;
		[SerializeField] private GameObject Center;
#if !UNITY_WEBGL && ENABLE_MAXST
		[SerializeField] private SpaceTrackableBehaviour spaceTrackable;
#endif		
		[SerializeField] private GameObject[] visualMesh;
		[SerializeField] private string maxSTPackageFileName;
		[SerializeField] private GameObject aerealCamera;
		[SerializeField] private GameObject walls;
		[SerializeField] private string payLoad;

		private Vector3 _initialCenter;
		
		public GameObject InitialPosition
		{
			get { return initialPosition; }
		}

		private Rect _area;
		private float _initialRotation;
		private POIData _currentPOI = null;

		public Rect Area 
		{
			get { return _area; }
		}
		public Vector3 InitialCenter
		{
			get { return _initialCenter; }
		}
		public float InitialRotation
		{
			get { return _initialRotation; }
		}
		public GameObject Content
        {
			get { return content; }
        }
		public GameObject Floor
        {
			get { return floor; }
        }
		public EasterEgg[] EasterEggs
        {
			get { return easterEggs; }
        }
		public POIData CurrentPOI
        {
			get { return _currentPOI; }
			set { 
				_currentPOI = value;  
			}
        }
		public GameObject TourGuideWorld
		{
			get { return tourGuideWorld; }
		}
		public string MaxSTPackageFileName
		{
			get { return maxSTPackageFileName; }
		}
		public GameObject AerealCamera
		{
			get { return aerealCamera; }
		}
		public POIData[] GetPOIS()
		{
			return POIS;
		}
		public EasterEgg[] GetEasterEggs()
		{
			return easterEggs;
		}
		public GameObject GetCenter 
		{
			get { return Center; }
		}

  		public string PackPOIsContent()
        {
            SerializedPOIPosition serializedPOIs = new SerializedPOIPosition();
			if (POIS == null)
			{
				serializedPOIs.Positions = new POIPosition[0];
			}
			else
			{
				serializedPOIs.Positions = new POIPosition[POIS.Length];
				for (int i = 0; i < POIS.Length; i++)
				{
					Vector3 updatedPosition = POIS[i].Root.transform.localPosition;
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
#if ENABLE_NIANTIC && !UNITY_EDITOR
					updatedPosition = POIS[i].Root.transform.position;
					updatedPosition = NavMeshController.Instance.ConvertARWorldToNavigation(updatedPosition);
#else
					updatedPosition = NavMeshController.Instance.ConvertARWorldToNavigation(updatedPosition);
#endif
#endif
					serializedPOIs.Positions[i] = new POIPosition(i, updatedPosition);
				}
			}

	        string jsonData = JsonUtility.ToJson(serializedPOIs, true);
            return jsonData;
        }

  		public string PackSecretsContent()
        {
            SerializedSecretPosition serializedSecrets = new SerializedSecretPosition();
			if (easterEggs == null)
			{
				serializedSecrets.Secrets = new SecretPosition[0];
			}
			else
			{
				serializedSecrets.Secrets = new SecretPosition[easterEggs.Length];
				for (int i = 0; i < easterEggs.Length; i++)
				{
#if ENABLE_NIANTIC && !UNITY_EDITOR
					Vector3 updatedPosition = easterEggs[i].Target.transform.position;
					updatedPosition = NavMeshController.Instance.ConvertARWorldToNavigation(updatedPosition);
#else
					Vector3 updatedPosition = easterEggs[i].Target.transform.localPosition;
#endif
					Vector3 finalPosition = updatedPosition;
					serializedSecrets.Secrets[i] = new SecretPosition(i, finalPosition, easterEggs[i].ActivationEvent, HexadecimalEncoding.ToHexString(easterEggs[i].Narration));
				}
			}

            string jsonData = JsonUtility.ToJson(serializedSecrets, true);
            return jsonData;
        }

		private void Awake()
        {
#if !UNITY_WEBGL && !ENABLE_VUFORIA	&& ENABLE_MAXST		
			if (spaceTrackable != null)
            {
				spaceTrackable.TrackerDataFileObject = MainController.Instance.GetCurrentMap();
			}	
#endif					
		}

        void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;

			if ((Corner1 != null) && (Corner2 != null))
			{
				float width = Math.Abs(Corner1.transform.position.x - Corner2.transform.position.x);
				float height = Math.Abs(Corner1.transform.position.z - Corner2.transform.position.z);
				_area = new Rect(Corner1.transform.position.x, Corner1.transform.position.z, width, height);
			}
			_initialRotation = this.transform.eulerAngles.y;

#if ENABLE_NIANTIC && !UNITY_EDITOR
			if (NavMeshController.Instance != null)
			{
				NavMeshController.Instance.UnParent();
			}				
#endif

#if UNITY_EDITOR || UNITY_WEBGL || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR
			if (visualMesh != null)
            {
				foreach (GameObject mesh in visualMesh)
				{
					if (mesh != null) mesh.SetActive(true);
				}
			}
#else
			if (visualMesh != null)
            {
				foreach (GameObject mesh in visualMesh)
				{
					if (mesh != null) mesh.SetActive(false);
				}
			}
#endif			
			if (navigationFloor != null) navigationFloor.GetComponent<MeshRenderer>().enabled = false;

			// REPOSITION POIs
			if (content != null)
			{
				POIS = null;
				InitializePOIs(true);
				InitializeSecrets(true);

				if (easterEggs != null)
				{
					foreach (EasterEgg egg in easterEggs)
					{
						egg.Target.SetActive(false);
						egg.Reference.SetActive(false);
					}
				}

				if (POIS != null)
				{
					for (int i = 0; i < POIS.Length; i++)
					{
						POIData item = POIS[i];
						foreach (Transform child in item.Root.transform)
						{
							child.gameObject.SetActive(false);
						}

						if (item.ReplayPOI != null)
						{
#if UNLOCK_EVERYTHING									
						if (!MainController.Instance.IsMultiplayer)
						{
							item.ReplayPOI.Activate();
						}
						else
						{
							if (NetworkController.Instance.IsServer)
							{
								item.ReplayPOI.Activate();
							}
							else
							{
								item.ReplayPOI.DeActivate();
							}
						}							
#else
							item.ReplayPOI.DeActivate();
#endif							
						}

					}
				}
			}


			if (Corner1 != null) Corner1.SetActive(false);
			if (Corner2 != null) Corner2.SetActive(false);

#if !UNITY_WEBGL && ENABLE_VUFORIA	&& !UNITY_EDITOR
			if (VuforiaController.Instance.HasAreaBeenDetected)
			{
				MainController.Instance.ApplyOclusionNavigation();
			}
#endif
#if !UNITY_WEBGL && ENABLE_NIANTIC	&& !UNITY_EDITOR
			if (NianticController.Instance.HasAreaBeenDetected)
			{
				MainController.Instance.ApplyOclusionNavigation();
			}
#endif

			SystemEventController.Instance.DelaySystemEvent(EventLevelViewStarted, 0.1f, this, payLoad);
		}

		void OnDestroy()
		{
			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
		}

		private void HideRenderWalls()
		{
			if (walls != null)
			{
				foreach (Transform child in walls.transform)
				{
					child.gameObject.GetComponent<Renderer>().enabled = false;
				}
			}
		}

		private void DestroyPOIs()
		{
			if (POIS != null)
			{
				for (int i = 0; i < POIS.Length; i++)
				{
					POIS[i].Destroy();
				}
				POIS = null;
			}
		}

		private void DestroySecrets()
		{
			if (easterEggs != null)
			{
				for (int i = 0; i < easterEggs.Length; i++)
				{
					easterEggs[i].Destroy();
				}
				easterEggs = null;
			}
		}

		private void InitializePOIs(bool isInitialization)
		{
			DestroyPOIs();
			int currID = GameLevelData.Instance.GetLevel(MainController.Instance.CurrentGameLevel);
			POIPosition[] poisStoredPosition = GameLevelData.Instance.GetLevelPOIsPositions(currID);
			if ((poisStoredPosition != null) && (poisStoredPosition.Length > 0))
			{
				POIS = new POIData[poisStoredPosition.Length];
				for (int i = 0; i < POIS.Length; i++)
				{
					POIBaseView poiBase = MainController.Instance.CreatePOIBase();
					POIS[i] = new POIData(poiBase.gameObject, poiBase.Sphere, poiBase.ReplayView, i);
					poiBase.Index = i;
					poiBase.gameObject.name = "POI_" + i;
					poiBase.gameObject.transform.parent = content.transform;
					poiBase.gameObject.transform.localPosition = Vector3.zero;
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
					if (!isInitialization)
					{
						if (!MainController.Instance.IsNormalAxis)
						{							
							poiBase.gameObject.transform.localRotation = tourGuideWorld.transform.localRotation;
						}
					}
#endif					
				}

				for (int i = 0; i < POIS.Length; i++)
				{
					if (i < poisStoredPosition.Length)
					{
						Vector3 updatedPosition = poisStoredPosition[i].Position;
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
#if ENABLE_VUFORIA || ENABLE_NIANTIC
						updatedPosition = NavMeshController.Instance.ConvertNavigationToStandardAR(updatedPosition);
#else
						if (isInitialization)
						{
							updatedPosition = NavMeshController.Instance.ConvertNavigationToStandardAR(updatedPosition);
						}
						else
						{
							updatedPosition = NavMeshController.Instance.ConvertNavigationToARWorld(updatedPosition);
						}		
#endif
#else
						updatedPosition.y = floor.transform.position.y + NavMeshController.SHIFT_FROM_FLOOR;
#endif
						POIS[i].Root.transform.position = updatedPosition;
					}
				}
			}			
		}

		private void InitializeSecrets(bool isInitialization)
		{
			DestroySecrets();
			int currID = GameLevelData.Instance.GetLevel(MainController.Instance.CurrentGameLevel);
			SecretPosition[] secretStoredPosition = GameLevelData.Instance.GetLevelSecretsPositions(currID);
			if ((secretStoredPosition != null) && (secretStoredPosition.Length > 0))
			{
				easterEggs = new EasterEgg[secretStoredPosition.Length];
				for (int i = 0; i < easterEggs.Length; i++)
				{
					EasterEggBaseView easterEggBase = MainController.Instance.CreateSecretBase();
					easterEggs[i] = new EasterEgg(easterEggBase.gameObject, easterEggBase.Reference, i);
					easterEggBase.Index = i;
					easterEggBase.gameObject.name = "Secret_" + i;
					easterEggBase.gameObject.transform.parent = content.transform;
					easterEggBase.gameObject.transform.localPosition = Vector3.zero;
					easterEggs[i].Star = easterEggBase.ContainerStart;
					easterEggs[i].MaterialStarDiscovered = easterEggBase.StartOn.GetComponent<MeshRenderer>();
					easterEggs[i].MaterialStarUndiscovered = easterEggBase.StartOff.GetComponent<MeshRenderer>();
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
					if (!isInitialization)
					{
						if (!MainController.Instance.IsNormalAxis)
						{							
							easterEggBase.gameObject.transform.localRotation = tourGuideWorld.transform.localRotation;
						}
					}
#endif					
				}

				for (int i = 0; i < easterEggs.Length; i++)
				{
					if (i < secretStoredPosition.Length)
					{
						Vector3 updatedPosition = secretStoredPosition[i].Position;
						Vector3 finalPosition = updatedPosition;
						Quaternion outputRotation = Quaternion.identity;
						bool shouldUpdatePosition = true;
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
						if (!isInitialization)
						{
							shouldUpdatePosition = false;
#if ENABLE_NIANTIC && !UNITY_EDITOR
							finalPosition = NianticController.Instance.DesignToWorldPoint(finalPosition);
							easterEggs[i].Target.transform.position = finalPosition;
#else
							easterEggs[i].Target.transform.localPosition = finalPosition;
#endif							
							
						}
#endif
						if (shouldUpdatePosition)
						{
#if ENABLE_NIANTIC && !UNITY_EDITOR
							finalPosition = NianticController.Instance.DesignToWorldPoint(finalPosition);
#endif							
							easterEggs[i].Target.transform.position = finalPosition;
						}						
						easterEggs[i].ActivationEvent = secretStoredPosition[i].CustomEvent;
						easterEggs[i].SetNarration(HexadecimalEncoding.FromHexString(secretStoredPosition[i].Narration));
					}
				}
			}
		}

		public int GetIndexSelectedPOI(Transform selectedPOI)
		{
			int indexPOISelected = -1;
			for (int i = 0; i < POIS.Length; i++)
			{
				POIData item = POIS[i];
				if (item.Root.transform == selectedPOI)
				{
					indexPOISelected = i;
				}
			}

			return indexPOISelected;
		}

		public int GetIndexSelectedSecret(Transform selectedSecret)
		{
			int indexSecretSelected = -1;
			for (int i = 0; i < easterEggs.Length; i++)
			{
				EasterEgg item = easterEggs[i];
				if (item.Target.transform == selectedSecret)
				{
					indexSecretSelected = i;
				}
			}

			return indexSecretSelected;
		}

		public EasterEgg GetSelectedSecret(Transform secret)
		{
			EasterEgg secretSelected = null;
			for (int i = 0; i < easterEggs.Length; i++)
			{
				EasterEgg item = easterEggs[i];
				if (item.Target.transform == secret)
				{
					secretSelected = item;
				}
			}

			return secretSelected;
		}		

		public bool IsInsideNavigationArea(Transform selectedPOI)
		{
			int indexPOISelected = GetIndexSelectedPOI(selectedPOI);

			if (indexPOISelected != -1)
			{
				Transform poiA = POIS[indexPOISelected].Root.transform;
				int nextPOI = (indexPOISelected+1) % POIS.Length;
				Transform poiB = POIS[nextPOI].Root.transform;
				Vector3 posNavigationA = Vector3.zero;
				Vector3 posNavigationB = Vector3.zero;

#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR  || UNITY_WEBGL)
				NavMeshController.Instance.RefAreaMaxSTHelper.transform.position = poiA.position;
#if ENABLE_NIANTIC
				posNavigationA = NavMeshController.Instance.ConvertARWorldToNavigation(poiA.position, false);
#else
				posNavigationA = NavMeshController.Instance.ConvertARWorldToNavigation(NavMeshController.Instance.RefAreaMaxSTHelper.transform.localPosition, false);
#endif				
#else
				posNavigationA = poiA.localPosition;
#endif

#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
				NavMeshController.Instance.RefAreaMaxSTHelper.transform.position = poiB.position;
#if ENABLE_NIANTIC				
				posNavigationB = NavMeshController.Instance.ConvertARWorldToNavigation(poiB.position, false);
#else
				posNavigationB = NavMeshController.Instance.ConvertARWorldToNavigation(NavMeshController.Instance.RefAreaMaxSTHelper.transform.localPosition, false);
#endif
#else
				posNavigationB = poiB.localPosition;
#endif
				NavMeshController.Instance.CreateNavigationAgentProvider(posNavigationA);
				List<Vector3> positionsToTarget = MainController.Instance.GetPathToTarget(posNavigationA, posNavigationB);
				if ((positionsToTarget == null) || (positionsToTarget.Count < 2))
				{
					return false;
				}
				else
				{
#if !(UNITY_EDITOR || ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || UNITY_WEBGL)
					return true;
#else
					Vector3 posNavigationStartFound = positionsToTarget[0];
					Vector3 posNavigationEndFound = positionsToTarget[positionsToTarget.Count - 1];
					posNavigationStartFound.y = 0;
					posNavigationA.y = 0;
					posNavigationEndFound.y = 0;
					posNavigationB.y = 0;

					if ((Vector3.Distance(posNavigationEndFound, posNavigationB) > 0.1f) || (Vector3.Distance(posNavigationStartFound, posNavigationA) > 0.1f))
					{
						return false;
					}
					else
					{
						return true;
					}
#endif					
				}
			}
			return false;
		}

		private void RefreshVisibilityElements(bool enableVisibility)
		{
			if (POIS != null)
			{
				for (int i = 0; i < POIS.Length; i++)
				{
					POIData item = POIS[i];
					foreach (Transform child in item.Root.transform)
					{
						child.gameObject.SetActive(enableVisibility);
					}
				}
			}
			if (easterEggs != null)
			{
				for (int i = 0; i < easterEggs.Length; i++)
				{
					EasterEgg egg = easterEggs[i];
					egg.Target.SetActive(!enableVisibility);
					egg.Reference.SetActive(!enableVisibility);
					EasterEggBaseView eggBase = egg.Target.GetComponentInChildren<EasterEggBaseView>();
					eggBase.Reference.SetActive(!enableVisibility);
					eggBase.RenderComponent.enabled = !enableVisibility;
				}
			}
		}
		
		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(ARMaxSTController.EventARMaxSTControllerAreaRecognized))
			{
#if ENABLE_NIANTIC && !UNITY_EDITOR
				this.transform.SetParent(NianticController.Instance.Anchor.transform, false);
				this.transform.localPosition = Vector3.zero;
				this.transform.localRotation = Quaternion.identity;
				this.transform.localScale    = Vector3.one;
#endif
			}
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataEditModeChanged))
			{
				RefreshVisibilityElements(GameLevelData.Instance.EditPOIsMode);
			}
			if (nameEvent.Equals(EventLevelViewEnablePOIsForEdit))
			{
				RefreshVisibilityElements(GameLevelData.Instance.EditPOIsMode);
			}
			if (nameEvent.Equals(EventLevelViewForcePOIsVisible))
			{
				RefreshVisibilityElements(true);
				if (Corner1 != null) Corner1.SetActive(true);
				if (Corner2 != null) Corner2.SetActive(true);
			}
            if (nameEvent.Equals(MainController.EventMainControllerReleaseGameResources))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(EventLevelViewDestroy))
			{
				GameObject.Destroy(this.gameObject);
			}
			if (nameEvent.Equals(EventLevelViewPlayEasterEgg))
			{
				foreach (EasterEgg egg in easterEggs)
				{
					if (egg != null)
					{
						if (egg.Enabled)
						{
							if (egg.ActivationEvent != null)
							{
								string activationEvent = (string)parameters[1];
								if (activationEvent.Equals(egg.ActivationEvent))
								{
									egg.ShowStar();
									int indexEasterEgg = (int)parameters[0];
									TourAnalyticsController.Instance.LogEasterEggUnlockedEvent(GameLevelData.Instance.Age, TourAnalyticsController.Instance.Floor, indexEasterEgg);
								}
							}
						}
					}
				}
			}
			if (nameEvent.Equals(EventLevelViewUnlockEasterEggs))
			{
				if (GameLevelData.Instance.UnlockSecretsIndex < MainController.Instance.CurrentNarrationPOI)
				{
					if (easterEggs != null)
					{
						foreach (EasterEgg egg in easterEggs)
						{
							egg.Activation();
						}
					}
				}
			}
			if (nameEvent.Equals(EventLevelViewUnlockReplayPOI))
            {
                int indexPOIReplayUnlock = (int)parameters[0];
                for (int i = 0; i < POIS.Length; i++)
                {
                    if (i <= indexPOIReplayUnlock)
                    {
                        if (POIS[i].ReplayPOI != null)
                        {
                            POIS[i].ReplayPOI.Activate();
                        }
                    }
                }
            }
			if (nameEvent.Equals(GameLevelData.EventGameLevelDataRefreshPOILevel))
			{
				InitializePOIs(false);
				InitializeSecrets(false);
			}
		}
    }
}
