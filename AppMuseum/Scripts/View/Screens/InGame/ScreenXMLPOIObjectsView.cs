using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Networking;
using yourvrexperience.Utils;
using yourvrexperience.VR;
using static yourvrexperience.Narration.GameLevelData;
using static yourvrexperience.Narration.NarrationController;
using static yourvrexperience.Narration.NarrationCreator;

namespace yourvrexperience.template6dof
{
	public class ScreenXMLPOIObjectsView : BaseScreenView, IScreenView
	{
		public const int MAXIMUM_NUMBER_OBJECTS = 4;

		public const string EventScreenXMLPOIObjectsViewRefresh = "EventScreenXMLPOIObjectsViewRefresh";
		public const string EventScreenXMLPOIObjectsViewScaleUp = "EventScreenXMLPOIObjectsViewScaleUp";
		public const string EventScreenXMLPOIObjectsViewScaleDown = "EventScreenXMLPOIObjectsViewScaleDown";
		public const string EventScreenXMLPOIObjectsViewAnimationSelected = "EventScreenXMLPOIObjectsViewAnimationSelected";

		public const string ScreenName = "ScreenXMLPOIObjectsView";

		private const float MIN_SCALE = 0.3f;
		private const float MAX_SCALE = 6f;


		[SerializeField] private Button buttonExit;

		[SerializeField] private Button buttonAdd;
		[SerializeField] private Button buttonEdit;
		[SerializeField] private Button buttonDelete;

		[SerializeField] private TextMeshProUGUI assetsTitle;
		[SerializeField] private TextMeshProUGUI instancesTitle;
        [SerializeField] private GameObject POIObjectAssetPrefab;
		[SerializeField] private GameObject POIObjectInstancePrefab;
		[SerializeField] private SlotManagerView SlotManagerAssets;
		[SerializeField] private SlotManagerView SlotManagerInstances;

		[SerializeField] private GameObject ContentScale;
		[SerializeField] private CustomButton buttonPlace;
		[SerializeField] private CustomButton buttonUp;
		[SerializeField] private CustomButton buttonDown;

		private TypeObjectNarration _typePOIObject;
		private NarrationCreatorToken _selectedEntry;
		private List<ItemMultiObjectEntry> _itemsPOIsObjectsAssets;
		private List<ItemMultiObjectEntry> _itemsPOIsObjectsInstances;
		private List<Asset> _assetSelected = new List<Asset>();
		private NarrationObject _objectSelected;

		private POIPhotoGalleryController _photoController;
		private POIVideoController _videoController;
		private POIModel3DController _model3DController;
		private bool _enableModel3DPositioning = false;
		private GameObject _interactableController;
		private AudioClip _audioFXSelected;
		private GameObject _waypoint;
		private List<GameObject> _existingWaypoints = new List<GameObject>();

		private string _nameAsset;
		private bool _scaleDown = false;
		private bool _scaleUp = false;				
		private bool _consumePlacement = false;
		private string[] _animations = null;
		private string _animationSelected = "";

		public override string NameScreen
		{ 
			get { return ScreenName; }
		}

		public override void Initialize(params object[] parameters)
		{
			base.Initialize(parameters);

			_typePOIObject = (TypeObjectNarration)parameters[0];
			_selectedEntry = (NarrationCreatorToken)parameters[1];

			SystemEventController.Instance.Event += OnSystemEvent;
			UIEventController.Instance.Event += OnUIEvent;

			buttonExit.onClick.AddListener(OnButtonExit);

			buttonAdd.onClick.AddListener(OnButtonAdd);
			buttonEdit.onClick.AddListener(OnButtonEdit);
			buttonDelete.onClick.AddListener(OnButtonDelete);

			buttonAdd.gameObject.SetActive(false);
			buttonEdit.gameObject.SetActive(false);
			buttonDelete.gameObject.SetActive(false);

			assetsTitle.text = LanguageController.Instance.GetText("screen.edit.segment.object.poi.asset");
			instancesTitle.text = LanguageController.Instance.GetText("screen.edit.segment.object.poi.instance");

			buttonUp.PointerDownButton += OnPressedUp;
			buttonUp.PointerUpButton += OnReleasedUp;
			buttonUp.PointerExitButton += OnReleasedUp;

			buttonDown.PointerDownButton += OnPressedDown;
			buttonDown.PointerUpButton += OnReleasedDown;
			buttonDown.PointerExitButton += OnReleasedDown;

			buttonPlace.PointerDownButton += OnPressedPlace;

			ContentScale.SetActive(false);

			FillObjectAssets();
			FillObjectInstances();
		}

        public override void Destroy()
		{
			base.Destroy();

			_selectedEntry = null;	
			if (_photoController != null)
			{
				GameObject.Destroy(_photoController.gameObject);
			}
			_photoController = null;
			if (_videoController != null)
			{
				GameObject.Destroy(_videoController.gameObject);
			}
			_videoController = null;			
			if (_model3DController != null)
			{
				GameObject.Destroy(_model3DController.gameObject);
			}
			if (_interactableController != null)
			{
				if (_interactableController.GetComponent<IGameInteractables>() != null)
				{
					_interactableController.GetComponent<IGameInteractables>().Destroy();
				}
				else
				{
					GameObject.Destroy(_interactableController);
				}				
			}			
			if (_waypoint != null)
			{
				GameObject.Destroy(_waypoint);
			}
			_model3DController = null;
			_audioFXSelected = null;
			_interactableController = null;
			_waypoint = null;
			DestroyExistingWaypoints();

			if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
			if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;
		}

		private void OnPressedPlace(CustomButton button)
        {
            _consumePlacement = true;
        }

		private void OnPressedDown(CustomButton button)
        {
            SystemEventController.Instance.DispatchSystemEvent(EventScreenXMLPOIObjectsViewScaleDown, true);
        }

        private void OnReleasedDown(CustomButton button)
        {
			SystemEventController.Instance.DispatchSystemEvent(EventScreenXMLPOIObjectsViewScaleDown, false);
        }

        private void OnPressedUp(CustomButton button)
        {
			SystemEventController.Instance.DispatchSystemEvent(EventScreenXMLPOIObjectsViewScaleUp, true);            
        }

        private void OnReleasedUp(CustomButton button)
        {
			SystemEventController.Instance.DispatchSystemEvent(EventScreenXMLPOIObjectsViewScaleUp, false);            
        }

		private void FillObjectAssets()
		{
			SlotManagerAssets.ClearCurrentGameObject(true);
            _itemsPOIsObjectsAssets = new List<ItemMultiObjectEntry>();
			List<Asset> objectAssets = GameLevelData.Instance.GetAssetsByType(_typePOIObject);
			for (int i = 0; i < objectAssets.Count; i++)
			{
				_itemsPOIsObjectsAssets.Add(new ItemMultiObjectEntry(this.gameObject, i, objectAssets[i], _typePOIObject));
			}
            SlotManagerAssets.Initialize(_itemsPOIsObjectsAssets.Count, _itemsPOIsObjectsAssets, POIObjectAssetPrefab);
		}

		private void FillObjectInstances()
		{
			ContentScale.SetActive(false);
			SlotManagerInstances.ClearCurrentGameObject(true);
            _itemsPOIsObjectsInstances = new List<ItemMultiObjectEntry>();			
			for (int i = 0; i < _selectedEntry.Assets.Count; i++)
			{
				NarrationObject narrationObject = _selectedEntry.Assets[i];
				if (narrationObject.Type == _typePOIObject)
				{
					string[] assetComponents = narrationObject.AssetName.Split(',');
					string nameInstance = "";
					foreach (string item in assetComponents)
					{
						if (nameInstance.Length > 0) nameInstance += ", ";
						nameInstance += GameLevelData.Instance.GetNameByAsset(item);
					}					 
					_itemsPOIsObjectsInstances.Add(new ItemMultiObjectEntry(this.gameObject, i, nameInstance, narrationObject));
				}
			}

            SlotManagerInstances.Initialize(_itemsPOIsObjectsInstances.Count, _itemsPOIsObjectsInstances, POIObjectInstancePrefab);			
		}

		private void DestroyExistingWaypoints()
		{
			foreach (GameObject way in _existingWaypoints)
			{
				if (way != null)
				{
					GameObject.Destroy(way);
				}
			}
			_existingWaypoints = new List<GameObject>();
		}

		private void RenderExistingWaypoints()
		{
			if (_typePOIObject == TypeObjectNarration.Waypoints)
			{
				DestroyExistingWaypoints();

				for (int i = 0; i < _selectedEntry.Assets.Count; i++)
				{
					NarrationObject narrationObject = _selectedEntry.Assets[i];
					if (narrationObject.Type == TypeObjectNarration.Waypoints)
					{
						GameObject waypoint = MainController.Instance.CreateWaypoint(narrationObject.AssetName, NavMeshController.Instance.AreaMaxST.transform, narrationObject.Position, narrationObject.Rotation, narrationObject.Scale);
						waypoint.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
						yourvrexperience.Utils.Utilities.ApplyLayer(waypoint.gameObject.transform, LayerMask.NameToLayer("Ignore Raycast"));
						_existingWaypoints.Add(waypoint);
					}
				}
			}
		}

        private void OnButtonExit()
        {
			UIEventController.Instance.DispatchUIEvent(ScreenController.EventScreenControllerDestroyScreen, this.gameObject);
        }

		private void HideToPositionObject()
		{
			SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, true);

			Content.gameObject.SetActive(false);
#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
			ContentScale.SetActive(true);

			buttonUp.gameObject.SetActive(_typePOIObject != TypeObjectNarration.Waypoints);
			buttonDown.gameObject.SetActive(_typePOIObject != TypeObjectNarration.Waypoints);
#endif				
		}

        private void OnButtonAdd()
        {
			if (_itemsPOIsObjectsInstances.Count > MAXIMUM_NUMBER_OBJECTS)
			{
				ScreenInformationView.CreateScreenInformation(ScreenInformationView.ScreenInformation, null, LanguageController.Instance.GetText("text.error"), LanguageController.Instance.GetText("screen.edit.segment.object.poi.instance.limit"));
				return;
			}

			if (_assetSelected.Count > 0)
			{
				switch (_typePOIObject)
				{
					case TypeObjectNarration.Image:						
						HideToPositionObject();
						string[] photos = new string[_assetSelected.Count];
						_nameAsset = "";
						for (int i = 0; i < photos.Length; i++)
						{
							photos[i] = _assetSelected[i].Value;
							_nameAsset += _assetSelected[i].Value + (i + 1 < photos.Length?",":"");
						}						
						_photoController = MainController.Instance.CreatePhotoGalleryController(false, photos, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, Vector3.one);
						break;

					case TypeObjectNarration.Video:
						HideToPositionObject();
						_nameAsset = _assetSelected[0].Value;
						_videoController = MainController.Instance.CreateVideoController(false, _nameAsset, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, Vector3.one, true, false);
						break;

					case TypeObjectNarration.Model3D:						
						_nameAsset = _assetSelected[0].Value;
						_model3DController = MainController.Instance.CreateModel3DController(false, _nameAsset, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, Vector3.one, "");
						_animations = GameLevelData.Instance.GetAnimationsByAsset(_nameAsset);						
						if ((_animations == null) || (_animations.Length == 0))
						{
							_enableModel3DPositioning = true;
							HideToPositionObject();
						}
						else
						{
							_enableModel3DPositioning = false;
							ScreenController.Instance.CreateScreen(ScreenXMLGenericSelectionView.ScreenName, false, true, _animations, EventScreenXMLPOIObjectsViewAnimationSelected);
						}
						break;

					case TypeObjectNarration.Interaction:
						HideToPositionObject();
						_nameAsset = _assetSelected[0].Value;
						_interactableController = MainController.Instance.CreateInteractable(_nameAsset, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, Vector3.one);
						_interactableController.GetComponent<IGameInteractables>().SetEditionMode();
						break;

					case TypeObjectNarration.Waypoints:
						HideToPositionObject();
						RenderExistingWaypoints();
						_nameAsset = _assetSelected[0].Value;
						_waypoint = MainController.Instance.CreateWaypoint(_nameAsset, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, Vector3.one);
						_waypoint.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
						break;

					case TypeObjectNarration.Sound:
						_nameAsset = _assetSelected[0].Value;
						_audioFXSelected = AssetBundleController.Instance.CreateAudioclip(_nameAsset);
						if (_audioFXSelected != null)
						{
							_selectedEntry.Assets.Add(new NarrationController.NarrationObject(_nameAsset, Camera.main.transform.position, Camera.main.transform.rotation, Vector3.one, _typePOIObject, ""));
							FillObjectInstances();
							SoundsController.Instance.PlaySoundClipFx(SoundsController.ChannelsAudio.FX3, _audioFXSelected, false, 1);
						}						
						break;
				}
			}
        }

        private void OnButtonEdit()
        {
			if (_objectSelected != null)
			{
				if (_typePOIObject == TypeObjectNarration.Sound)
				{
					_audioFXSelected = AssetBundleController.Instance.CreateAudioclip(_objectSelected.AssetName);
					if (_audioFXSelected != null)
					{
						SoundsController.Instance.PlaySoundClipFx(SoundsController.ChannelsAudio.FX1, _audioFXSelected, false, 1);
					}
				}
				else
				{
					if (_selectedEntry.Assets.Remove(_objectSelected))
					{
						_nameAsset = _objectSelected.AssetName;
						switch (_typePOIObject)
						{
							case TypeObjectNarration.Image:
								HideToPositionObject();
								string[] photos = _nameAsset.Split(',');
								_photoController = MainController.Instance.CreatePhotoGalleryController(false, photos, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, _objectSelected.Scale);
								break;

							case TypeObjectNarration.Video:
								HideToPositionObject();
								_videoController = MainController.Instance.CreateVideoController(false, _objectSelected.AssetName, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, _objectSelected.Scale, true, false);
								break;

							case TypeObjectNarration.Model3D:
								_model3DController = MainController.Instance.CreateModel3DController(false, _objectSelected.AssetName, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, _objectSelected.Scale, _objectSelected.Animation);
								_animations = GameLevelData.Instance.GetAnimationsByAsset(_nameAsset);						
								if ((_animations == null) || (_animations.Length == 0))
								{
									_enableModel3DPositioning = true;
									HideToPositionObject();
								}
								else
								{
									_enableModel3DPositioning = false;
									ScreenController.Instance.CreateScreen(ScreenXMLGenericSelectionView.ScreenName, false, true, _animations, EventScreenXMLPOIObjectsViewAnimationSelected);
								}
								break;

							case TypeObjectNarration.Interaction:
								HideToPositionObject();
								_interactableController = MainController.Instance.CreateInteractable(_objectSelected.AssetName, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, _objectSelected.Scale);
								_interactableController.GetComponent<IGameInteractables>().SetEditionMode();
								break;

							case TypeObjectNarration.Waypoints:
								HideToPositionObject();
								_waypoint = MainController.Instance.CreateWaypoint(_objectSelected.AssetName, NavMeshController.Instance.AreaMaxST.transform, Vector3.zero, Quaternion.identity, Vector3.one);
								_waypoint.transform.localScale = new Vector3(0.2f,0.2f,0.2f);
								break;
						}
					}
				}
			}
        }

        private void OnButtonDelete()
        {
			if (_objectSelected != null)
			{
				if (_selectedEntry.Assets.Remove(_objectSelected))
				{
					FillObjectInstances();
					buttonEdit.gameObject.SetActive(false);
					buttonDelete.gameObject.SetActive(false);
				}
			}
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
		{
			if (nameEvent.Equals(EventScreenXMLPOIObjectsViewScaleUp))
			{
				_scaleUp = (bool)parameters[0];
			}
			if (nameEvent.Equals(EventScreenXMLPOIObjectsViewScaleDown))
			{
				_scaleDown = (bool)parameters[0];
			}
		}

        private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventScreenXMLPOIObjectsViewAnimationSelected))
			{
				_animationSelected = (string)parameters[0];
				if (_animationSelected.Length > 0)
				{
					_model3DController.PlayAnimation(_animationSelected);					
				}
				_enableModel3DPositioning = true;
				HideToPositionObject();
			}
			if (nameEvent.Equals(ItemXMLObjectAsset.EventItemXMLObjectAssetSelected))
			{
				if ((GameObject)parameters[0] == this.gameObject)
                {
					int idSelected = (int)parameters[2];
					Asset sAsset = (Asset)parameters[3];
					if (idSelected == -1)
					{
						_assetSelected.Remove(sAsset);
						buttonAdd.gameObject.SetActive(false);
					}
					else
					{
						if (_typePOIObject != TypeObjectNarration.Image)
						{
							_assetSelected.Clear();
						}
						_assetSelected.Add(sAsset);
						buttonAdd.gameObject.SetActive(true);
					}
					buttonEdit.gameObject.SetActive(false);
					buttonDelete.gameObject.SetActive(false);
				}
			}
			if (nameEvent.Equals(ItemXMLObjectInstance.EventItemXMLObjectInstanceSelected))
			{
				if ((GameObject)parameters[0] == this.gameObject)
                {
					int idSelected = (int)parameters[2];
					if (idSelected == -1)
					{
						_objectSelected = null;
						buttonEdit.gameObject.SetActive(false);
						buttonDelete.gameObject.SetActive(false);
					}
					else
					{
						_objectSelected = (NarrationObject)parameters[3];
						buttonEdit.gameObject.SetActive(_typePOIObject != TypeObjectNarration.Waypoints);
						buttonDelete.gameObject.SetActive(true);
					}
				}
			}
        }

		void Update()
		{
			if (!Content.gameObject.activeSelf)
			{
				MainController.Instance.PlayerView.Run();
				
				bool placedObject = false;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
				Vector3	positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
				Vector3	forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;
				positionCurrentController += forwardCurrentController.normalized * 2;
				placedObject = VRInputController.Instance.VRController.GetIndexTriggerDown(XR_HAND.both);
				Vector2 axisJoystick = VRInputController.Instance.VRController.GetVector2Joystick(XR_HAND.both);
				_scaleUp = ((axisJoystick.y> 0.5)?true:false);
				_scaleDown = ((axisJoystick.y<-0.5)?true:false);
#else
				Vector3 positionCurrentController = Camera.main.transform.position;
				Vector3 forwardCurrentController = Camera.main.transform.forward;
				positionCurrentController += forwardCurrentController.normalized * 2;
				if (_consumePlacement)
				{
					_consumePlacement = false;
					placedObject = true;
				}
#endif				

				if (placedObject)
				{
					Content.gameObject.SetActive(true);
				}

				Vector3 outputPos;
				Quaternion outputRot;

				HeightComponent heightController = null;

				switch (_typePOIObject)
				{
					case TypeObjectNarration.Image:
						if (_photoController != null)
						{
							if (_photoController.gameObject.GetComponent<HeightComponent>() == null)
							{
								heightController = _photoController.gameObject.AddComponent<HeightComponent>();
							}
							else
							{
								heightController = _photoController.gameObject.GetComponent<HeightComponent>();
							}

							_photoController.transform.position = new Vector3(positionCurrentController.x, positionCurrentController.y + heightController.Height, positionCurrentController.z);

							if (!MainController.Instance.IsNormalAxis)
							{
								Vector3 worldUp = -Vector3.forward;
								_photoController.transform.rotation = Quaternion.LookRotation(forwardCurrentController, worldUp);								
							}
							else
							{
								_photoController.transform.forward = forwardCurrentController;
							}

							if (_scaleUp)
							{
								if (_photoController.Scale.magnitude < MAX_SCALE)
								{
									_photoController.Scale += (Vector3.one * Time.deltaTime);									
								}
							}
							if (_scaleDown)
							{
								if (_photoController.Scale.magnitude > MIN_SCALE)
								{
									_photoController.Scale -= (Vector3.one * Time.deltaTime);
								}								
							}			

							if (placedObject)
							{
								SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
								_selectedEntry.Assets.Add(new NarrationController.NarrationObject(_nameAsset, _photoController.transform.localPosition, _photoController.transform.localRotation, _photoController.Scale, _typePOIObject, ""));
								FillObjectInstances();

								GameObject.Destroy(_photoController.gameObject);
								_photoController = null;
							}
						}
						break;

					case TypeObjectNarration.Video:
						if (_videoController != null)
						{
							if (_videoController.gameObject.GetComponent<HeightComponent>() == null)
							{
								heightController = _videoController.gameObject.AddComponent<HeightComponent>();
							}
							else
							{
								heightController = _videoController.gameObject.GetComponent<HeightComponent>();
							}

							_videoController.transform.position = new Vector3(positionCurrentController.x, positionCurrentController.y + heightController.Height, positionCurrentController.z);

							if (!MainController.Instance.IsNormalAxis)
							{
								Vector3 worldUp = -Vector3.forward;
								_videoController.transform.rotation = Quaternion.LookRotation(forwardCurrentController, worldUp);								
							}
							else
							{
								_videoController.transform.forward = forwardCurrentController;
							}

							if (_scaleUp)
							{
								if (_videoController.Scale.magnitude < MAX_SCALE)
								{
									_videoController.Scale += (Vector3.one * Time.deltaTime);
								}
							}
							if (_scaleDown)
							{
								if (_videoController.Scale.magnitude > MIN_SCALE)
								{
									_videoController.Scale -= (Vector3.one * Time.deltaTime);
								}								
							}

							if (placedObject)
							{
								SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
								_selectedEntry.Assets.Add(new NarrationController.NarrationObject(_nameAsset, _videoController.transform.localPosition, _videoController.transform.localRotation, _videoController.Scale, _typePOIObject, ""));
								FillObjectInstances();

								GameObject.Destroy(_videoController.gameObject);
								_videoController = null;
							}
						}
						break;

					case TypeObjectNarration.Model3D:
						if ((_model3DController != null) && _enableModel3DPositioning)
						{
							if (_model3DController.gameObject.GetComponent<HeightComponent>() == null)
							{
								heightController = _model3DController.gameObject.AddComponent<HeightComponent>();
							}
							else
							{
								heightController = _model3DController.gameObject.GetComponent<HeightComponent>();
							}

							_model3DController.transform.position = new Vector3(positionCurrentController.x, positionCurrentController.y + heightController.Height, positionCurrentController.z);
					
							
							if (!MainController.Instance.IsNormalAxis)
							{
								Vector3 worldUp = -Vector3.forward;
								_model3DController.transform.rotation = Quaternion.LookRotation(forwardCurrentController, worldUp);
							}
							else
							{
								_model3DController.transform.forward = forwardCurrentController;
							}

							if (_scaleUp)
							{
								if (_model3DController.Scale.magnitude < MAX_SCALE)
								{
									_model3DController.Scale += (Vector3.one * Time.deltaTime);									
								}
							}
							if (_scaleDown)
							{
								if (_model3DController.Scale.magnitude > MIN_SCALE)
								{
									_model3DController.Scale -= (Vector3.one * Time.deltaTime);
								}								
							}

							if (placedObject)
							{
								SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
								_selectedEntry.Assets.Add(new NarrationController.NarrationObject(_nameAsset, _model3DController.transform.localPosition, _model3DController.transform.localRotation, _model3DController.Scale, _typePOIObject, _animationSelected));
								FillObjectInstances();

								GameObject.Destroy(_model3DController.gameObject);
								_model3DController = null;
							}
						}
						break;			

					case TypeObjectNarration.Interaction:
						if (_interactableController != null)
						{
							if (_interactableController.gameObject.GetComponent<HeightComponent>() == null)
							{
								heightController = _interactableController.gameObject.AddComponent<HeightComponent>();
							}				
							else
							{
								heightController = _interactableController.gameObject.GetComponent<HeightComponent>();
							}

							_interactableController.transform.position = new Vector3(positionCurrentController.x, positionCurrentController.y + heightController.Height, positionCurrentController.z);

							if (!MainController.Instance.IsNormalAxis)
							{
								Vector3 worldUp = -Vector3.forward;
								_interactableController.transform.rotation = Quaternion.LookRotation(forwardCurrentController, worldUp);
							}
							else
							{
								_interactableController.transform.forward = forwardCurrentController;
							}

							if (_scaleUp)
							{
								if (_interactableController.transform.localScale.magnitude < MAX_SCALE)
								{
									_interactableController.transform.localScale += (Vector3.one * Time.deltaTime);
								}
							}
							if (_scaleDown)
							{
								if (_interactableController.transform.localScale.magnitude > MIN_SCALE)
								{
									_interactableController.transform.localScale -= (Vector3.one * Time.deltaTime);
								}
							}

							if (placedObject)
							{
								SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
								_selectedEntry.Assets.Add(new NarrationController.NarrationObject(_nameAsset, _interactableController.transform.localPosition, _interactableController.transform.localRotation, _interactableController.transform.localScale, _typePOIObject, ""));
								FillObjectInstances();

								_interactableController.GetComponent<IGameInteractables>().Destroy();
								_interactableController = null;
							}
						}
						break;			

					case TypeObjectNarration.Waypoints:
						if (_waypoint != null)
						{
							if (_waypoint.gameObject.GetComponent<HeightComponent>() == null)
							{
								heightController = _waypoint.gameObject.AddComponent<HeightComponent>();
							}
							else
							{
								heightController = _waypoint.gameObject.GetComponent<HeightComponent>();
							}

							_waypoint.transform.position = new Vector3(positionCurrentController.x, positionCurrentController.y + heightController.Height, positionCurrentController.z);

							if (!MainController.Instance.IsNormalAxis)
							{
								Vector3 worldUp = -Vector3.forward;
								_waypoint.transform.rotation = Quaternion.LookRotation(forwardCurrentController, worldUp);
							}
							else
							{
								_waypoint.transform.forward = forwardCurrentController;
							}

							if (placedObject)
							{
								SystemEventController.Instance.DispatchSystemEvent(PlayerView.EventPlayerAppEnableMovement, false);
								_selectedEntry.Assets.Add(new NarrationController.NarrationObject(_nameAsset, _waypoint.transform.localPosition, _waypoint.transform.localRotation, _waypoint.transform.localScale, _typePOIObject, ""));
								FillObjectInstances();

								if (_waypoint != null)
								{
									GameObject.Destroy(_waypoint);
								}
								_waypoint = null;
							}							
						}
						break;
				}
				if (heightController != null)				
				{
					if (Input.mouseScrollDelta.y > 0)
					{
						if (heightController.Height > -2f)
						{
							heightController.Height -= (Time.deltaTime * 2f);									
						}
					}	
					if (Input.mouseScrollDelta.y < 0)
					{
						if (heightController.Height < 2f)
						{
							heightController.Height += (Time.deltaTime * 2f);
						}
					}
				}
			}
		}
	}
}