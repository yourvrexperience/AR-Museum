
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

namespace yourvrexperience.Utils
{
    public delegate void AssetBundleEventHandler(string nameEvent, params object[] parameters);

    public class AssetBundleController : MonoBehaviour
    {
        public event AssetBundleEventHandler AssetBundleEvent;

        public const string EventAssetBundleAssetsLoaded     = "EventAssetBundleAssetsLoaded";
        public const string EventAssetBundleAssetsProgress   = "EventAssetBundleAssetsProgress";
        public const string EventAssetBundleAssetsUnknownProgress = "EventAssetBundleAssetsUnknownProgress";
        public const string EventAssetBundleLevelXML         = "EventAssetBundleLevelXML";
        public const string EventAssetBundleOneTimeLoadingAssets = "EventAssetBundleOneTimeLoadingAssets";

        private static AssetBundleController _instance;

        public static AssetBundleController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(AssetBundleController)) as AssetBundleController;
                }
                return _instance;
            }
        }

		[SerializeField] private string AssetBundleURL = "https://www.yourvrexperience.com/address/to/assetbundle";
		[SerializeField] private int AssetBundleVersion = 1;

        private bool _isLoadinAnAssetBundle = false;
        private List<ItemMultiObjectEntry> _loadBundles = new List<ItemMultiObjectEntry>();
        private List<string> _urlsBundle = new List<string>();
        private Dictionary<string, AssetBundle> _assetBundle = new Dictionary<string, AssetBundle>();
        private List<TimedEventData> listEvents = new List<TimedEventData>();

        private Dictionary<string, Object> _loadedObjects = new Dictionary<string, Object>();

        private bool _hasBeenLoaded = false;

        public bool HasBeenLoaded
        {
            get { return _hasBeenLoaded; }
            set { _hasBeenLoaded = value;}
        }
		
		void Start()
		{
			SystemEventController.Instance.Event += OnSystemEvent;
		}

        void OnDestroy()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (Instance != null)
            {
				AssetBundleController reference = _instance;
				_instance = null;
                Destroy(reference.gameObject);
                _loadedObjects.Clear();                

				if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
            }
        }

        public void ClearLocalCache()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            Caching.ClearCache();
#endif
        }

        public void DispatchAssetBundleEvent(string nameEvent, params object[] parameters)
        {
            if (AssetBundleEvent != null) AssetBundleEvent(nameEvent, parameters);
        }

        public void DelayAssetBundleEvent(string nameEvent, float time, params object[] parameters)
        {
            listEvents.Add(new TimedEventData(nameEvent, time, parameters));
        }

        public bool LoadAssetBundle(string url = "", string nameAsset = "", int version = -1)
        {
			string finalURL = url;
			int finalVersion = version;
			if ((finalURL == null) || (finalURL.Length == 0))
			{
				finalURL = AssetBundleURL;
			}
			if (version == -1)
			{
				finalVersion = AssetBundleVersion;
			}
            if (!_urlsBundle.Contains(finalURL))
            {
                _urlsBundle.Add(finalURL);
                _loadBundles.Add(new ItemMultiObjectEntry(finalURL, nameAsset, finalVersion));
                return false;
            }
            else
            {
                if (IsAssetBundleLoaded(finalURL, nameAsset))
                {
                    AllAssetsLoaded(finalURL, nameAsset);
                }                
                return true;
            }            
        }

        public bool CheckCachedBundle(string nameBundle)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return false;
#else
            List<Hash128> cachedVersions = new List<Hash128>();
            Caching.GetCachedVersions(nameBundle, cachedVersions);
            for (int i = 0; i < cachedVersions.Count; i++)
            {
                if (Caching.IsVersionCached(nameBundle, cachedVersions[i]))
                {
                    return true;
                }
            }            
            return false;
#endif
        }

        private void DownloadAssetBundle(string finalURL, string nameAsset)
        {
            DispatchAssetBundleEvent(EventAssetBundleOneTimeLoadingAssets);
            CachedAssetBundle cacheBundle = new CachedAssetBundle();
            StartCoroutine(WebRequestAssetBundle(UnityWebRequestAssetBundle.GetAssetBundle(finalURL, cacheBundle), finalURL, nameAsset));
        }

        public void AllAssetsLoaded(string urlSource, string nameAsset)
        {
            DelayAssetBundleEvent(EventAssetBundleAssetsLoaded, 0.1f, urlSource, nameAsset);
            _isLoadinAnAssetBundle = false;            
        }

        private bool IsAssetBundleLoaded(string urlAssetBundle, string nameAsset)
        {
            foreach(KeyValuePair<string, AssetBundle> item in _assetBundle)
            {
                if (item.Key.Equals(urlAssetBundle))
                {
                    return true;
                }
            }
            return false;
        }

        public IEnumerator WebRequestAssetBundle(UnityWebRequest www, string urlAssetBundle, string nameAsset)
        {
            www.SendWebRequest();
            while (!www.isDone)
            {
                DispatchAssetBundleEvent(EventAssetBundleAssetsProgress, www.downloadProgress);
                yield return new WaitForSeconds(.1f);
            }
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                _assetBundle.Add(urlAssetBundle, DownloadHandlerAssetBundle.GetContent(www));
            }
            AllAssetsLoaded(urlAssetBundle, nameAsset);
        }

        public Object GetObject(string name)
        {
#if UNITY_EDITOR
            yourvrexperience.Utils.Utilities.DebugLogColor("AssetbundleController::CreateGameObject::_name=" + name, Color.red);
#endif
            if (_assetBundle.Count == 0) return null;

            foreach (KeyValuePair<string,AssetBundle> item in _assetBundle)
            {
                if (item.Value.Contains(name))
                {
                    if (!_loadedObjects.ContainsKey(name))
                    {
                        _loadedObjects.Add(name, item.Value.LoadAsset(name));
                    }
                    return _loadedObjects[name];
                }
            }
            return null;
        }

        public GameObject CreateGameObject(string name)
        {
#if UNITY_EDITOR
            yourvrexperience.Utils.Utilities.DebugLogColor("AssetbundleController::CreateGameObject::_name=" + name, Color.red);
#endif
            if (_assetBundle.Count == 0) return null;

            foreach (KeyValuePair<string,AssetBundle> item in _assetBundle)
            {
                if (item.Value.Contains(name))
                {
                    if (!_loadedObjects.ContainsKey(name))
                    {
                        _loadedObjects.Add(name, item.Value.LoadAsset(name));
                    }
                    GameObject newObject = Instantiate(_loadedObjects[name]) as GameObject;
#if UNITY_EDITOR || ENABLE_URP_SHADERS
                    yourvrexperience.Utils.Utilities.ResetMaterials(newObject);
#endif
                    return newObject;
                }
            }
            return null;
        }

        public Sprite CreateSprite(string name)
        {
#if UNITY_EDITOR
            yourvrexperience.Utils.Utilities.DebugLogColor("AssetbundleController::CreateSprite::_name=" + name, Color.red);
#endif
            if (_assetBundle.Count == 0) return null;

            foreach (KeyValuePair<string,AssetBundle> item in _assetBundle)
            {
                if (item.Value.Contains(name))
                {
                    if (!_loadedObjects.ContainsKey(name))
                    {
                        _loadedObjects.Add(name, item.Value.LoadAsset(name));
                    }
                    return Instantiate(_loadedObjects[name]) as Sprite;
                }
            }
            return null;
        }

        public Texture2D CreateTexture(string name)
        {
#if UNITY_EDITOR
            yourvrexperience.Utils.Utilities.DebugLogColor("AssetbundleController::CreateTexture::_name=" + name, Color.red);
#endif
            if (_assetBundle.Count == 0) return null;

            foreach (KeyValuePair<string,AssetBundle> item in _assetBundle)
            {
                if (item.Value.Contains(name))
                {
                    if (!_loadedObjects.ContainsKey(name))
                    {
                        _loadedObjects.Add(name, item.Value.LoadAsset(name));
                    }
                    return Instantiate(_loadedObjects[name]) as Texture2D;
                }
            }

            return null;
        }

        public Material CreateMaterial(string name)
        {
#if UNITY_EDITOR
            yourvrexperience.Utils.Utilities.DebugLogColor("AssetbundleController::CreateMaterial::_name=" + name, Color.red);
#endif
            if (_assetBundle.Count == 0) return null;

            foreach (KeyValuePair<string,AssetBundle> item in _assetBundle)
            {
                if (item.Value.Contains(name))
                {
                    if (!_loadedObjects.ContainsKey(name))
                    {
                        _loadedObjects.Add(name, item.Value.LoadAsset(name));
                    }
                    return Instantiate(_loadedObjects[name]) as Material;
                }
            }

            return null;
        }

        public AudioClip CreateAudioclip(string name)
        {
            if (_assetBundle.Count == 0) return null;

            foreach (KeyValuePair<string,AssetBundle> item in _assetBundle)
            {
                if (item.Value.Contains(name))
                {
                    if (!_loadedObjects.ContainsKey(name))
                    {
                        _loadedObjects.Add(name, item.Value.LoadAsset(name));
                    }
                    return Instantiate(_loadedObjects[name]) as AudioClip;
                }
            }

            return null;
        }

        public VideoClip CreateVideoclip(string name)
        {
            if (_assetBundle.Count == 0) return null;

            foreach (KeyValuePair<string, AssetBundle> item in _assetBundle)
            {
                if (item.Value.Contains(name))
                {
                    if (!_loadedObjects.ContainsKey(name))
                    {
                        _loadedObjects.Add(name, item.Value.LoadAsset(name));
                    }
                    return Instantiate(_loadedObjects[name]) as VideoClip;
                }
            }

            return null;
        }
        
        public void ClearAssetBundleEvents(string _nameEvent = "")
        {
            if (_nameEvent.Length == 0)
            {
                for (int i = 0; i < listEvents.Count; i++)
                {
                    listEvents[i].Time = -1000;
                }
            }
            else
            {
                for (int i = 0; i < listEvents.Count; i++)
                {
                    TimedEventData eventData = listEvents[i];
                    if (eventData.NameEvent == _nameEvent)
                    {
                        eventData.Time = -1000;
                    }
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

        void Update()
        {
            if (!_isLoadinAnAssetBundle)
            {
                if (_loadBundles.Count > 0)
                {
                    string assetBundleURL = (string)_loadBundles[0].Objects[0];
                    string assetName = (string)_loadBundles[0].Objects[1];
                    int assetBundleVersion = (int)_loadBundles[0].Objects[2];
                    _loadBundles.RemoveAt(0);
                    _isLoadinAnAssetBundle = true;
                    DownloadAssetBundle(assetBundleURL, assetName);
                }
            }

            // DELAYED EVENTS
            for (int i = 0; i < listEvents.Count; i++)
            {
                TimedEventData eventData = listEvents[i];
                if (eventData.Time == -1000)
                {
                    eventData.Destroy();
                    listEvents.RemoveAt(i);
                    break;
                }
                else
                {
                    eventData.Time -= Time.deltaTime;
                    if (eventData.Time <= 0)
                    {
                        listEvents.RemoveAt(i);
                        if ((eventData != null) && (AssetBundleEvent != null))
                        {
                            AssetBundleEvent(eventData.NameEvent, eventData.Parameters);
                            eventData.Destroy();
                        }                        
                        break;
                    }
                }
            }
        }
    }
}