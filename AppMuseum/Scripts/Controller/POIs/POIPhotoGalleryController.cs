using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.template6dof
{
    public class POIPhotoGalleryController : MonoBehaviour
    {
        public const string EventPOIPhotoGalleryControllerMaximize = "EventPOIPhotoGalleryControllerMaximize";
        public const string EventPOIPhotoGalleryControllerMinimize = "EventPOIPhotoGalleryControllerMinimize";

        [SerializeField] private Image containerImage;

        [SerializeField] private Button previous;
        [SerializeField] private Button maximize;
        [SerializeField] private Button next;
        [SerializeField] private Button VREndPhoto;

        private bool _inited = false;
        private List<Sprite> _images = new List<Sprite>();
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;
        private int _currentImage = 0;
        private bool _isEasterEgg = false;

        public Vector3 Scale
        {
            get { return _scale; }
            set { 
                _scale = value;
                this.transform.localScale = _scale;
            }
        }

        private int CurrentImage
        {
            get { return _currentImage; }
            set
            {
                _currentImage = value;
                if (_currentImage < _images.Count)
                {
                    if (_currentImage < 0)
                    {
                        _currentImage = _images.Count - 1;
                        containerImage.overrideSprite = _images[_currentImage];
                    }
                }
                else
                {
                    _currentImage = 0;
                }
                containerImage.overrideSprite = _images[_currentImage];
            }
        }

        public void Play(bool isEasterEgg, string[] images, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _isEasterEgg = isEasterEgg;
            if (!_inited)
            {
                _inited = true;

                for (int i = 0; i < images.Length; i++)
                {
                    _images.Add(ImageUtils.ToSprite(AssetBundleController.Instance.CreateTexture(images[i])));
                }
                _position = position;
                _rotation = rotation;
                _scale = scale;

                this.transform.parent = parent;
                this.transform.localPosition = _position;
                this.transform.localRotation = _rotation;
                this.transform.localScale = _scale;

                previous.onClick.AddListener(OnPreviousClicked);
                next.onClick.AddListener(OnNextClicked);
                if (_images.Count == 1)
                {
                    previous.gameObject.SetActive(false);
                    next.gameObject.SetActive(false);
                }

                CurrentImage = 0;

#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
                maximize.onClick.AddListener(OnMaximizeClicked);
                VREndPhoto.gameObject.SetActive(false);
#else
                maximize.gameObject.SetActive(false);
                VREndPhoto.onClick.AddListener(OnEndPhotoClicked);
                ScreenController.Instance.ApplyVRRayCasterOnCanvas(this.gameObject);                
#endif                

                VREndPhoto.gameObject.SetActive(false);

                if (MainController.Instance.EnableEditionPOIs)
                {
                    previous.gameObject.SetActive(false);
                    next.gameObject.SetActive(false);      
                    maximize.gameObject.SetActive(false);              
                }

                SystemEventController.Instance.Event += OnSystemEvent;
            }
        }

        private void OnDestroy()
        {
            _inited = false;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void OnEndPhotoClicked()
        {            
			SystemEventController.Instance.DispatchSystemEvent(ScreenHUDEasterEggView.EventScreenHUDEasterEggViewTriggerResume);
        }

        private void OnMaximizeClicked()
        {
            SystemEventController.Instance.DispatchSystemEvent(EventPOIPhotoGalleryControllerMaximize, _isEasterEgg, _images, _currentImage);
            containerImage.gameObject.SetActive(false);
        }

        private void OnNextClicked()
        {
            CurrentImage++;
        }

        private void OnPreviousClicked()
        {
            CurrentImage--;
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventPOIPhotoGalleryControllerMinimize))
            {
                CurrentImage = (int)parameters[0];
                containerImage.gameObject.SetActive(true);
            }
            if (nameEvent.Equals(LevelView.EventLevelViewDestroyEasterEgg))
            {
                if (_inited && _isEasterEgg)
                {
                    GameObject.Destroy(this.gameObject);
                }
            }
            if (nameEvent.Equals(GameLevelData.EventGameLevelDataDestroyNarrationObjects))
            {
                GameObject.Destroy(this.gameObject);
            }
            if (nameEvent.Equals(NarrationToken.EventNarrationTokenDestroyNarrationObject))
            {
                if (parameters.Length > 0)
                {
                    bool easterEggDestruction = (bool)parameters[0];
                    if (easterEggDestruction)
                    {
                        if (_isEasterEgg)
                        {
                            GameObject.Destroy(this.gameObject);     
                        }
                    }
                    else
                    {
                        GameObject.Destroy(this.gameObject);
                    }
                }
                else
                {
                    GameObject.Destroy(this.gameObject);
                }                
            }
        }

        void Update()
        {
        }
    }
}
