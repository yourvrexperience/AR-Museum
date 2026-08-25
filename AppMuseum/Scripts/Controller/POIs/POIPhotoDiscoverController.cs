using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Utils;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR)	     
using yourvrexperience.VR;
#endif

namespace yourvrexperience.template6dof
{
    public class POIPhotoDiscoverController : MonoBehaviour, IGameInteractables
    {
        public const float LimitToDiscover = 0.65f;

        private enum StatesDiscover { Edition, Synchronization, AppearImages, Discover, Reveal }

        [SerializeField] private Collider CameraPlacement;
        [SerializeField] private GameObject CenterDetection;

        [SerializeField] private Collider colliderFront;
        [SerializeField] private Image containerFront;
        [SerializeField] private Image containerTarget;
        [SerializeField] private Button VREndDiscover;

        private bool _inited = false;
        private Sprite _imageTarget;
        private Sprite _imageFront;

        private StatesDiscover _stateDiscover = StatesDiscover.Synchronization;
        private bool _isErasing = false;
        private int _layerDiscover = -1;

        private Texture2D _textureFront;
        private Texture2D _textureTarget;

        private int _totalPixels = 0;
        private float _timeAcum = 0;


        public void SetEditionMode()
        {
            if (!_inited)
            {
                _inited = true;
                _stateDiscover = StatesDiscover.Edition;

                SystemEventController.Instance.Event += OnSystemEvent;
            }
        }

        public void Destroy()
        {
            GameObject.Destroy(this.gameObject);
        }

        public void Play()
        {
            if (!_inited)
            {
                _inited = true;
                _stateDiscover = StatesDiscover.Synchronization;

                _textureFront  = ImageUtils.MakeWritableCopy(containerFront.sprite.texture);
                _textureTarget = containerTarget.sprite.texture;
                _imageTarget = ImageUtils.ToSprite(_textureTarget);
                _imageFront = ImageUtils.ToSprite(_textureFront);

                _totalPixels = _textureFront.width * _textureFront.height;

                containerFront.overrideSprite = _imageFront;
                containerTarget.overrideSprite = _imageTarget;
                containerFront.gameObject.SetActive(false);
                containerTarget.gameObject.SetActive(false);
                CameraPlacement.gameObject.SetActive(true);
                CenterDetection.gameObject.SetActive(true);

                SystemEventController.Instance.Event += OnSystemEvent;

                // _stateDiscover = StatesDiscover.Synchronization;
                _stateDiscover = StatesDiscover.Discover;
                containerFront.gameObject.SetActive(true);
                containerTarget.gameObject.SetActive(true);
                CameraPlacement.gameObject.SetActive(false);
                CenterDetection.gameObject.SetActive(false);
                
                _layerDiscover = LayerMask.GetMask("Discover");

#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
                VREndDiscover.gameObject.SetActive(false);
#else
                VREndDiscover.onClick.AddListener(OnEndDiscoverClicked);
                ScreenController.Instance.ApplyVRRayCasterOnCanvas(containerTarget.transform.parent.gameObject);
                ScreenController.Instance.ApplyVRRayCasterOnCanvas(containerFront.transform.parent.gameObject);                
#endif                
           }
        }

        private void OnEndDiscoverClicked()
        {
            SystemEventController.Instance.DispatchSystemEvent(ScreenHUDEasterEggView.EventScreenHUDEasterEggViewTriggerResume);
        }

        private void OnDestroy()
        {
            if (_inited)
            {
                _inited = false;
                if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
            }
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(LevelView.EventLevelViewDestroyEasterEgg))
            {
                if (_inited)
                {
                    GameObject.Destroy(this.gameObject);
                }
            }
            if (nameEvent.Equals(GameLevelData.EventGameLevelDataDestroyNarrationObjects))
            {
                GameObject.Destroy(this.gameObject);
            }
        }

        private Vector2 WorldToLocal2D(Transform objectTransform, Vector3 worldPos)
        {
            Vector3 relativePosition = worldPos - objectTransform.position;
            relativePosition = Quaternion.Inverse(objectTransform.rotation) * relativePosition;
            Vector3 localPosition = new Vector3(relativePosition.x / objectTransform.localScale.x,
                                                relativePosition.y / objectTransform.localScale.y,
                                                relativePosition.z / objectTransform.localScale.z);
            return new Vector2(localPosition.x, localPosition.y);
        }

        private bool SetPixelToAlpha(int x, int y)
        {
            if ((x < 0) || (y < 0)) return false;
            if ((x >= _textureFront.width) || (y >= _textureFront.height)) return false;

            _textureFront.SetPixel(x, y, new Color(1, 1, 1, 0));
            return true;
        }

        private void ErasePixelImage(Vector3 worldPointErase)
        {
            Vector2 positionInImage = WorldToLocal2D(colliderFront.gameObject.transform, worldPointErase);
            positionInImage.x += _textureFront.width / 2;
            positionInImage.y += _textureFront.height / 2;

            int sizeBrush = 100;
            int startingX = (int)(positionInImage.x - sizeBrush / 2);
            int startingY = (int)(positionInImage.y - sizeBrush / 2);

            for (int k = startingX; k < startingX + (sizeBrush / 2); k++)
            {
                for (int l = startingY; l < startingY + (sizeBrush / 2); l++)
                {
                    SetPixelToAlpha(k, l);
                }
            }

            _textureFront.Apply();

            _imageFront = ImageUtils.ToSprite(_textureFront);
            containerFront.overrideSprite = _imageFront;
        }

        private int CountAlphaPixelsImage()
        {
            int counter = 0;
            for (int k = 0; k < _textureFront.width ; k++)
            {
                for (int l = 0; l < _textureFront.height; l++)
                {
                    Color colorPixel = _textureFront.GetPixel(k, l);
                    if (colorPixel.a == 0) counter++;
                }
            }
            return counter;
        }

        private void Update()
        {
            switch (_stateDiscover)
            {
                case StatesDiscover.Synchronization:
                    if (CameraPlacement.bounds.Contains(Camera.main.transform.position))
                    {
					    bool isPhotoVisible = yourvrexperience.Utils.Utilities.IsVisibleFrom(CenterDetection.transform.position, Camera.main);
#if ENABLE_VUFORIA
					    isPhotoVisible = VuforiaController.Instance.CheckVisiblePoint(CenterDetection.transform.position);
#elif !UNITY_EDITOR && !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || UNITY_WEBGL)
					    isPhotoVisible = ARMaxSTController.Instance.CheckVisiblePoint(CenterDetection.transform.position);
#endif
                        if (isPhotoVisible)
                        {
                            containerFront.gameObject.SetActive(true);
                            containerTarget.gameObject.SetActive(true);
                            CameraPlacement.gameObject.SetActive(false);
                            CenterDetection.gameObject.SetActive(false);
                            _stateDiscover = StatesDiscover.Discover;
                        }
                    }
                    break;
                case StatesDiscover.AppearImages:
                    break;
                case StatesDiscover.Discover:
                    Vector3 collisionPoint = Vector3.zero;
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
                    Vector3	positionCurrentController = VRInputController.Instance.VRController.CurrentController.transform.position;
                    Vector3	forwardCurrentController = VRInputController.Instance.VRController.CurrentController.transform.forward;
                    RaycastHit ray = new RaycastHit();
                    collisionPoint = RaycastingTools.GetRaycastOriginForward(positionCurrentController, forwardCurrentController, ref ray, 100, _layerDiscover);
#endif
                    if (!_isErasing)
                    {
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
                        if (MainController.Instance.GameInputController.ActionPrimaryDown())
                        {
                            if (collisionPoint != Vector3.zero)
                            {                                
                                GameObject targetCollided = RaycastingTools.GetRaycastObject(positionCurrentController, forwardCurrentController, 100, ref ray, _layerDiscover);
                                if (targetCollided == VREndDiscover.gameObject)
                                {
                                    OnEndDiscoverClicked();
                                }
                                else
                                {
                                    _isErasing = true;
                                }
                            }
                        }
#else
                        if (Input.GetMouseButtonDown(0))
                        {
                            RaycastHit rayData = new RaycastHit();
                            if (RaycastingTools.GetMouseCollisionObject(Camera.main, ref rayData, _layerDiscover))
                            {
                                if (rayData.collider.gameObject == colliderFront.gameObject)
                                {
                                    _isErasing = true;
                                    collisionPoint = rayData.point;
                                }
                            }
                        }
#endif
                        if (_isErasing)
                        {
                            ErasePixelImage(collisionPoint);
                            _isErasing = true;
                        }
                    }
                    else
                    {
#if (ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
                        if (MainController.Instance.GameInputController.ActionPrimaryUp())
                        {
                            _isErasing = false;
                        }
#else
                        if (Input.GetMouseButtonUp(0))
                        {
                            _isErasing = false;
                        }
#endif
                        if (!_isErasing)
                        {
                            int countedAlpha = CountAlphaPixelsImage();
                            if (countedAlpha > _totalPixels * LimitToDiscover)
                            {
                                containerFront.gameObject.SetActive(false);
                                _stateDiscover = StatesDiscover.Reveal;
                                SoundsController.Instance.PlaySoundFX(GameSounds.FxWin, false, 1);
                            }
                            _isErasing = false;
                        }
                        else
                        {
#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)                            
                            RaycastHit rayData = new RaycastHit();
                            if (RaycastingTools.GetMouseCollisionObject(Camera.main, ref rayData, _layerDiscover))
                            {
                                if (rayData.collider.gameObject == colliderFront.gameObject)
                                {
                                    collisionPoint = rayData.point;
                                }
                            }
#endif

                            ErasePixelImage(collisionPoint);

                            _timeAcum += Time.deltaTime;
                            if (_timeAcum > 2)
                            {
                                _timeAcum = 0;
                                int countedAlpha = CountAlphaPixelsImage();
                                if (countedAlpha > _totalPixels * LimitToDiscover)
                                {
                                    containerFront.gameObject.SetActive(false);
                                    _stateDiscover = StatesDiscover.Reveal;
                                    SoundsController.Instance.PlaySoundFX(GameSounds.FxWin, false, 1);
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}
