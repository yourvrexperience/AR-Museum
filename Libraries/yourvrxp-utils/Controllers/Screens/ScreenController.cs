using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_NREAL
using NRKernal;
#endif
#if ENABLE_ULTIMATEXR
using UltimateXR.UI.UnityInputModule;
#endif
#if ENABLE_OPENXR
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
using yourvrexperience.VR;
#endif

namespace yourvrexperience.Utils
{
    public class ScreenController : MonoBehaviour
    {

		[System.Serializable]
		public class GameFontAsset
		{
			[SerializeField] public string Name;
			[SerializeField] public TMP_FontAsset Font;
		}

		public const string EventScreenControllerStarted = "EventScreenControllerStarted";
        public const string EventScreenControllerRequestCameraData = "EventScreenControllerRequestCameraData";
        public const string EventScreenControllerResponseCameraData = "EventScreenControllerResponseCameraData";
		public const string EventScreenControllerDestroyScreen = "EventScreenControllerDestroyScreen";
		public const string EventScreenControllerDestroyAllScreens = "EventScreenControllerDestroyAllScreens";
		public const string EventScreenControllerToggleInGameDebugConsole = "EventScreenControllerToggleInGameDebugConsole";
		public const string EventScreenControllerCreateScreen = "EventScreenControllerCreateScreen";
		public const string EventScreenControllerCreateInformationScreen = "EventScreenControllerCreateInformationScreen";
		public const string EventScreenControllerReactivateAllScreens = "EventScreenControllerReactivateAllScreens";

        private static ScreenController _instance;
        public static ScreenController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType<ScreenController>();
                }
                return _instance;
            }
        }

		[SerializeField] private LanguageController languageData;
		[SerializeField] private float distanceScreen = 1.2f;
		[SerializeField] private float sizeVRScreen = 0.002f;
		[SerializeField] private float scaleFactor = 0.5f;
		[SerializeField] private float withVRScreen = 1920;
		[SerializeField] private float heightVRScreen = 3414;
		[SerializeField] private GameFontAsset[] Fonts;
		public GameObject[] Screens;
 
 		[SerializeField] private bool applyOverlay = false;   // shader: UI/WorldspaceOverlay
 		[SerializeField] private Material overlayUIMaterial;   // shader: UI/WorldspaceOverlay
        [SerializeField] private Shader   overlayTextShader;   // duplicated TMP SDF shader with ZTest Always		

        private List<GameObject> _screensCreated = new List<GameObject>();

		private bool _isInGameDebugConsole = false;
		private Vector3 _forward = Vector3.zero;
        private Vector3 _position = Vector3.zero;
		private GameObject _anchor = null;
		private float _scale = -1;
		private float _defaultDistance = -1;
		private bool _hasBeenInitialized = false;

		public float DistanceScreen
		{
			get { return distanceScreen; }
		}
		public float SizeVRScreen
		{
			get { return sizeVRScreen; }
		}
		public int ScreenCreatedTotal
        {
			get { return _screensCreated.Count; }
        }
		public bool ApplyOverlay 
		{
			get { return applyOverlay; }
			set { applyOverlay = value; }
		}

		public void Initialize()
		{
			RunInitialization();
		}

        void Start()
        {
			RunInitialization();
        }

		public GameFontAsset[] GetFonts()
        {
			return Fonts;
        }

		public TMP_FontAsset GetFontByName(string nameFont)
		{
			for (int i = 0; i < Fonts.Length; i++)
			{
				if (Fonts[i].Name.Equals(nameFont))
                {
					return Fonts[i].Font;
				}
			}
			return null;
		}

		private void RunInitialization()
		{
			if (!_hasBeenInitialized)
			{
				_hasBeenInitialized = true;
				
				SystemEventController.Instance.Event += OnSystemEvent;
				UIEventController.Instance.Event += OnUIEvent;

				languageData.Initialize();
				
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
				VRInputController.Instance.Event += OnVREvent;
#endif			
				SystemEventController.Instance.DispatchSystemEvent(EventScreenControllerStarted);

				_isInGameDebugConsole = false;
			}
		}

        void OnDestroy()
        {
            Destroy();
        }

		void Destroy()
		{
			if (_instance != null)
			{
				_instance = null;
				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
				if (UIEventController.Instance != null) UIEventController.Instance.Event -= OnUIEvent;

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
				if (VRInputController.Instance != null) VRInputController.Instance.Event -= OnVREvent;
#endif			

				GameObject.Destroy(this.gameObject);
			}			
		}

		public int GetTotalNumberScreens()
		{
			return _screensCreated.Count;
		}

#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
		private void OnVREvent(string nameEvent, object[] parameters)
		{
            if (nameEvent.Equals(EventScreenControllerResponseCameraData))
            {
                GameObject targetScreen = (GameObject)parameters[0];
				Vector3 positionCamera = (Vector3)parameters[1];
				Vector3 forwardCamera = (Vector3)parameters[2];
				targetScreen.transform.position = positionCamera + ((_defaultDistance!=-1)?_defaultDistance:DistanceScreen) * forwardCamera;
				targetScreen.transform.forward = forwardCamera;
				targetScreen.transform.localScale = new Vector3(SizeVRScreen, SizeVRScreen, SizeVRScreen);
				if (_forward != Vector3.zero)
				{
					targetScreen.transform.position = positionCamera + _forward * ((_defaultDistance!=-1)?_defaultDistance:DistanceScreen);
					targetScreen.transform.forward = _forward;
				}
				if (_position != Vector3.zero)
				{
					targetScreen.transform.position = _position;
					if (_forward != Vector3.zero)
					{
						targetScreen.transform.forward = _forward;
					}
					else
					{
						targetScreen.transform.forward = forwardCamera;
					}                        
				}
				if (_scale == -1)
				{
					targetScreen.transform.localScale = new Vector3(SizeVRScreen, SizeVRScreen, SizeVRScreen);
				}
				else
				{
					targetScreen.transform.localScale = new Vector3(_scale, _scale, _scale);
				}
				_forward = Vector3.zero;
				_scale = -1;
				_position = Vector3.zero;
				_defaultDistance = -1;
            }
		}
#endif			

        public void ApplyOverlayShader(GameObject screen)
        {
            if (screen == null) return;
 
            Graphic[] graphics = screen.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] is TMP_Text) continue;
                if (overlayUIMaterial != null) graphics[i].material = overlayUIMaterial;
            }
 
            TMP_Text[] texts = screen.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (overlayTextShader != null)
                {
                    texts[i].fontMaterial.shader = overlayTextShader;
                }
            }
        }

		private void OnSystemEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerReleaseAllResources))
            {
                Destroy();
            }		
			if (nameEvent.Equals(SystemEventController.EventSystemEventControllerDontDestroyOnLoad))
			{
				if (Instance)
				{
					DontDestroyOnLoad(Instance.gameObject);
				}
			}
        }

		private void OnUIEvent(string nameEvent, object[] parameters)
        {
			if (nameEvent.Equals(EventScreenControllerCreateScreen))
			{
				string nameScreen = (string)parameters[0];
				bool destroyPreviousScreen = (bool)parameters[1];
				bool hidePreviousScreen = (bool)parameters[2]; 
				object[] extraParameters = (object[])parameters[3]; 
				CreateScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, extraParameters);
			}
			if (nameEvent.Equals(EventScreenControllerCreateInformationScreen))
			{
				string screenName = (string)parameters[0];
				GameObject origin = (GameObject)parameters[1];
				string title = (string)parameters[2];
				string description = (string)parameters[3]; 
				string customEvent = ((parameters.Length>4)?(string)parameters[4]:""); 
				string ok = ((parameters.Length>5)?(string)parameters[5]:""); 
				string cancel = ((parameters.Length>6)?(string)parameters[6]:""); 
				Sprite infoImage = ((parameters.Length>7)?(Sprite)parameters[7]:null);

				ScreenInformationView.CreateScreenInformation(screenName, origin, title, description, customEvent, ok, cancel, infoImage);
			}
			if (nameEvent.Equals(EventScreenControllerReactivateAllScreens))
            {
				foreach (GameObject screen in _screensCreated)
				{
					IScreenView screenInterface = screen.GetComponent<IScreenView>();
					if (screenInterface != null)
					{
						screenInterface.ActivateContent(true);
					}
				}
			}
			if (nameEvent.Equals(EventScreenControllerDestroyScreen))
            {
                GameObject screen = (GameObject)parameters[0];
                if (_screensCreated.Contains(screen))
                {
					if (screen.GetComponent<IScreenView>() != null)  screen.GetComponent<IScreenView>().Destroy();
                    _screensCreated.Remove(screen);
                    GameObject.Destroy(screen);
                    screen = null;
					if (_screensCreated.Count > 0)
					{
						IScreenView screenInterface =  _screensCreated[_screensCreated.Count - 1].GetComponent<IScreenView>();
						if (screenInterface != null)
						{
							screenInterface.ActivateContent(true);
						}
					}
                }
            }
			if (nameEvent.Equals(EventScreenControllerDestroyAllScreens))
			{
				DestroyScreens();
			}
			if (nameEvent.Equals(EventScreenControllerToggleInGameDebugConsole))
			{
			}
        }

        public GameObject CreateForwardScreen(string nameScreen, Vector3 forward, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
            _forward = forward;
            GameObject screen = CreateScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, parameters);
			if (applyOverlay)
			{
				ApplyOverlayShader(screen);
			}
			return screen;			
        }

		public GameObject CreateDistanceScreen(string nameScreen, float distance, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
            _defaultDistance = distance;
            GameObject screen = CreateScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, parameters);
			if (applyOverlay)
			{
				ApplyOverlayShader(screen);
			}
			return screen;			
        }

        public GameObject CreatePositionScreen(string nameScreen, Vector3 position, float scale, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
            _position = position;
            _scale = scale;
            GameObject screen = CreateScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, parameters);
			if (applyOverlay)
			{
				ApplyOverlayShader(screen);
			}
			return screen;				
        }

        public GameObject CreatePositionForwardScreen(string nameScreen, Vector3 position, Vector3 forward, float scale, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
            _position = position;
            _scale = scale;
			_forward = forward;
			GameObject screen = CreateScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, parameters);
			if (applyOverlay)
			{
				ApplyOverlayShader(screen);
			}
			return screen;			
        }

        public GameObject CreateScreen3DAnchor(string nameScreen, GameObject anchor, Vector3 position, Vector3 forward, float scale, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
            _position = position;
            _scale = scale;
			_forward = forward;
			_anchor = anchor;
            GameObject screen = CreateScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, parameters);
			if (applyOverlay)
			{
				ApplyOverlayShader(screen);
			}
			return screen;
        }

        public GameObject CreateWorldScreen(string nameScreen, Vector3 position, Vector3 forward, float scale, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
            GameObject newScreen = CreateSingleScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, parameters);
			newScreen.transform.position = position;
			newScreen.transform.forward = forward;
			newScreen.transform.localScale = new Vector3(scale, scale, scale);
			newScreen.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
			if (applyOverlay)
			{
				ApplyOverlayShader(newScreen);
			}
			return newScreen;
        }
		public GameObject CreateScreen(string nameScreen, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
			GameObject screen = CreateSingleScreen(nameScreen, destroyPreviousScreen, hidePreviousScreen, parameters);
			if (applyOverlay)
			{
				ApplyOverlayShader(screen);
			}
			return screen;
		}
        private GameObject CreateSingleScreen(string nameScreen, bool destroyPreviousScreen, bool hidePreviousScreen, params object[] parameters)
        {
			GameObject newScreen = null;
            if (destroyPreviousScreen)
            {
                DestroyScreens();
            }
			else
			{
				if (hidePreviousScreen)
				{
					foreach (GameObject screen in _screensCreated)
					{
						IScreenView screenInterface = screen.GetComponent<IScreenView>();
						if (screenInterface != null)
						{
							screenInterface.ActivateContent(false);
						}
					}
				}
			}

			bool screenFound = false;
            for (int i = 0; i < Screens.Length; i++)
            {
				if (Screens[i] != null)
				{
					if (Screens[i].name == nameScreen)
					{
						screenFound = true;
						newScreen = Instantiate(Screens[i]);
						_screensCreated.Add(newScreen);
						if (newScreen.GetComponent<IScreenView>() != null)  newScreen.GetComponent<IScreenView>().Initialize(parameters);
						ApplyCanvas(newScreen, _defaultDistance);
					}
				}
            }
			if (!screenFound)
			{
				throw new Exception("Screen with the name["+nameScreen+"] has not been found in ScreenController");
			}
			return newScreen;
        }

		public void ApplyCanvas(GameObject newScreen, float defaultDistance = -1)
		{
			_defaultDistance = defaultDistance;
			if (_anchor != null)
			{
				newScreen.transform.SetParent(_anchor.transform);
			}
			else
			{
				newScreen.transform.SetParent(this.transform);
			}			
			_anchor = null;
#if ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL
			newScreen.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
			newScreen.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			newScreen.GetComponent<CanvasScaler>().scaleFactor = scaleFactor;			
			newScreen.GetComponent<RectTransform>().sizeDelta = new Vector2(withVRScreen, heightVRScreen);
#if !ENABLE_NREAL
			yourvrexperience.Utils.Utilities.ApplyZTestTop(newScreen.transform);
#endif			
#if ENABLE_OCULUS
			newScreen.AddComponent<OVRRaycaster>();
#elif ENABLE_OPENXR
			newScreen.AddComponent<TrackedDeviceGraphicRaycaster>();
#elif ENABLE_ULTIMATEXR
			if (newScreen.GetComponent<UxrCanvas>() == null) newScreen.AddComponent<UxrCanvas>();
			if (newScreen.GetComponent<UxrLaserPointerRaycaster>() == null) newScreen.AddComponent<UxrLaserPointerRaycaster>();
			newScreen.GetComponent<UxrCanvas>().CanvasInteractionType = UxrInteractionType.LaserPointers;
			newScreen.GetComponentInChildren<Canvas>().renderMode = RenderMode.WorldSpace;									
#elif ENABLE_NREAL
			newScreen.AddComponent<CanvasRaycastTarget>();					
#endif
			VRInputController.Instance.DispatchVREvent(EventScreenControllerRequestCameraData, newScreen);
#endif
		}

		public void ApplyVRRayCasterOnCanvas(GameObject target)
		{
			Canvas targetCanvas = target.GetComponentInChildren<Canvas>();			
			if (targetCanvas != null)
			{
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
#if ENABLE_OCULUS
                targetCanvas.gameObject.AddComponent<OVRRaycaster>();
#elif ENABLE_OPENXR
                targetCanvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
#elif ENABLE_ULTIMATEXR
                if (targetCanvas.gameObjectGetComponent<UxrCanvas>() == null) targetCanvas.gameObjectAddComponent<UxrCanvas>();
                if (targetCanvas.gameObjectGetComponent<UxrLaserPointerRaycaster>() == null) targetCanvas.gameObjectAddComponent<UxrLaserPointerRaycaster>();
                targetCanvas.gameObject.GetComponent<UxrCanvas>().CanvasInteractionType = UxrInteractionType.LaserPointers;
                targetCanvas.gameObject.GetComponentInChildren<Canvas>().renderMode = RenderMode.WorldSpace;									
#elif ENABLE_NREAL
                targetCanvas.gameObject.AddComponent<CanvasRaycastTarget>();					
#endif
#endif                    
			}
		}

        public void DestroyScreens()
        {
            for (int i = 0; i < _screensCreated.Count; i++)
            {
				if (_screensCreated[i].GetComponent<IScreenView>() != null)  _screensCreated[i].GetComponent<IScreenView>().Destroy();
                GameObject.Destroy(_screensCreated[i]);
            }
            _screensCreated.Clear();
        }

		public void Update()
		{
			if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.H))
			{
				UIEventController.Instance.DispatchUIEvent(EventScreenControllerToggleInGameDebugConsole);
			}			
		}
    }
}