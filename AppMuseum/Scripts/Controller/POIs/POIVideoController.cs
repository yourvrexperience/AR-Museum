using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using yourvrexperience.Narration;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.template6dof
{
    public class POIVideoController : MonoBehaviour
    {
        public const string EventPOIVideoControllerMaximize = "EventPOIVideoControllerMaximize";
        public const string EventPOIVideoControllerMinimize = "EventPOIVideoControllerMinimize";
        public const string EventPOIVideoControllerPlayDelayed = "EventPOIVideoControllerPlayDelayed";

        public const float JumpTime = 10;

        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private AudioSource audioSource;

        [SerializeField] private Button previous;
        [SerializeField] private Button maximize;
        [SerializeField] private Button next;
        [SerializeField] private Button VREndVideo;

        private bool _inited = false;
        private string _video;
        private Vector3 _position;
        private Quaternion _rotation;
        private Vector3 _scale;
        private bool _shouldPlay = false;
        private bool _isEasterEgg = false;

        public Vector3 Scale
        {
            get { return _scale; }
            set { 
                _scale = value;
                this.transform.localScale = _scale;
            }
        }

        public void Play(bool isEasterEgg, string video, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, bool shouldPlay, bool shouldMinimize)
        {
            _isEasterEgg = isEasterEgg;
            if (!_inited)
            {
                _inited = true;                

                _video = video;
                _position = position;
                _rotation = rotation;
                _scale = scale;
                _shouldPlay = shouldPlay;

                this.transform.parent = parent;
                this.transform.localPosition = _position;
                this.transform.localRotation = _rotation;
                this.transform.localScale = _scale;

                VideoClip videoClip = AssetBundleController.Instance.CreateVideoclip(video);

                if (videoClip != null)
                {
                    videoPlayer.clip = videoClip;
                    if (_shouldPlay) videoPlayer.Play();
                }
                else
                {
                    Debug.LogError("Video not found!");
                }
                previous.onClick.AddListener(OnPreviousClicked);                
                next.onClick.AddListener(OnNextClicked);
                SystemEventController.Instance.Event += OnSystemEvent;

#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL)
                maximize.onClick.AddListener(OnMaximizeClicked);
                VREndVideo.gameObject.SetActive(false);
#else
                maximize.gameObject.SetActive(false);
                VREndVideo.onClick.AddListener(OnEndVideoClicked);
                ScreenController.Instance.ApplyVRRayCasterOnCanvas(this.gameObject);                
#endif                

                VREndVideo.gameObject.SetActive(false);

                if (MainController.Instance.EnableEditionPOIs)
                {
                    previous.gameObject.SetActive(false);
                    next.gameObject.SetActive(false);
                    maximize.gameObject.SetActive(false);
                }

                if (shouldMinimize)
                {
                    OnMaximizeClicked();
                    SystemEventController.Instance.DispatchSystemEvent(ScreenHUDEggVideoView.EventScreenHUDEggVideoViewForceMinimize);
                }
                else
                {
                    videoPlayer.Pause();
                    videoPlayer.gameObject.SetActive(false);
                    SystemEventController.Instance.DelaySystemEvent(EventPOIVideoControllerPlayDelayed, 0.1f, 0.1f);
                }                
            }
        }

        private void OnEndVideoClicked()
        {            
			SystemEventController.Instance.DispatchSystemEvent(ScreenHUDEasterEggView.EventScreenHUDEasterEggViewTriggerResume);
        }

        private void OnDestroy()
        {
            _inited = false;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void OnMaximizeClicked()
        {
            SystemEventController.Instance.DispatchSystemEvent(EventPOIVideoControllerMaximize, _isEasterEgg, _video, (float)videoPlayer.time);
            videoPlayer.Pause();
            videoPlayer.gameObject.SetActive(false);
        }

        private void OnNextClicked()
        {
            if (videoPlayer.time + POIVideoController.JumpTime < videoPlayer.clip.length)
            {
                videoPlayer.time += POIVideoController.JumpTime;
            }
        }

        private void OnPreviousClicked()
        {
            if (videoPlayer.time - POIVideoController.JumpTime < 0)
            {
                videoPlayer.time = 0;
            }
            else
            {
                videoPlayer.time -= POIVideoController.JumpTime;
            }
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventPOIVideoControllerMinimize) || nameEvent.Equals(EventPOIVideoControllerPlayDelayed))
            {
                if (_shouldPlay)
                {
                    float time = (float)parameters[0];
                    videoPlayer.gameObject.SetActive(true);
                    videoPlayer.time = time;
                    videoPlayer.Play();
                }
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
