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
        [SerializeField] private YouTubeController youTubePlayer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameObject videoControls;

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
        private string _videoURL = "";
        private bool _isWorldspace = true;

        public Vector3 Scale
        {
            get { return _scale; }
            set { 
                _scale = value;
                this.transform.localScale = _scale;
            }
        }

        public void PlayScreenspace(string video, string videoURL, float time, float duration)        
        {
            _isWorldspace = false;
            if (!_inited && !_isWorldspace)
            {
                _inited = true;

                if (VREndVideo != null)
                {
                    GameObject.Destroy(VREndVideo.gameObject);
                    VREndVideo = null;
                }

                previous.onClick.AddListener(OnPreviousClicked);                
                next.onClick.AddListener(OnNextClicked);
                maximize.onClick.AddListener(OnMaximizeClicked);

                _video = video;
                _videoURL = videoURL;
                _shouldPlay = true;
            }
            InitializeVideo();
            InitialPlay(time, duration);
        }

        public void PlayWorldspace(bool isEasterEgg, string video, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, bool shouldPlay, bool shouldMinimize, string videoURL)
        {
            _isEasterEgg = isEasterEgg;
            _isWorldspace = true;
            if (!_inited && _isWorldspace)
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

                _videoURL = videoURL;
                InitializeVideo();
                InitialPlay(-1);
                
                previous.onClick.AddListener(OnPreviousClicked);                
                next.onClick.AddListener(OnNextClicked);
                SystemEventController.Instance.Event += OnSystemEvent;

#if !(ENABLE_OCULUS || ENABLE_OPENXR || ENABLE_ULTIMATEXR || ENABLE_NREAL || ENABLE_NIANTICXR || ENABLE_QR_XR || ENABLE_QR_META)
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
                    if ((_videoURL != null) && (_videoURL.Length > 0))
                    {
                        youTubePlayer.Pause();
                    }
                    else
                    {
                        videoPlayer.Pause();
                    }
                    videoPlayer.gameObject.SetActive(false);
                    youTubePlayer.gameObject.SetActive(false);
                    SystemEventController.Instance.DelaySystemEvent(EventPOIVideoControllerPlayDelayed, 0.1f, 0.1f);
                }                
            }
        }

        private void InitializeVideo()
        {
            if ((_videoURL != null) && (_videoURL.Length > 0))
            {
                videoPlayer.gameObject.SetActive(false);
                youTubePlayer.gameObject.SetActive(true);
            }
            else
            {
                youTubePlayer.gameObject.SetActive(false);
                VideoClip videoClip = AssetBundleController.Instance.CreateVideoclip(_video);

                if (videoClip != null)
                {
                    videoPlayer.clip = videoClip;
                }
                else
                {
                    Debug.LogError("Video not found!");
                }
            }    
        }

        private void InitialPlay(float timeJump = -1, float duration = -1)
        {
            if (_shouldPlay)
            {                
                if ((_videoURL != null) && (_videoURL.Length > 0))
                {
                    youTubePlayer.Play(_videoURL);
                    youTubePlayer.JumpToTime(timeJump, duration); // THE ASSET FROM THE ASSET STORE USED IS REALLY BAD AND HAS LEAKS EVERYWHERE, IF THE PROJECT MOVES FORWARD THIS SHOULD BE REPLACED BY SOMETHING PROFESSIONAL
                }
                else
                {   
                    if (timeJump != -1) videoPlayer.time = timeJump;                 
                    videoPlayer.Play();                    
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
            if (_isWorldspace)
            {
                if ((_videoURL != null) && (_videoURL.Length > 0))
                {
                    SystemEventController.Instance.DispatchSystemEvent(EventPOIVideoControllerMaximize, _isEasterEgg, _video, _videoURL, youTubePlayer.Time(), youTubePlayer.Duration());
                    youTubePlayer.Pause();
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(EventPOIVideoControllerMaximize, _isEasterEgg, _video, _videoURL, (float)videoPlayer.time, (float)videoPlayer.length);
                    videoPlayer.Pause();
                }
                youTubePlayer.gameObject.SetActive(false);
                videoPlayer.gameObject.SetActive(false);
                videoControls.SetActive(false);
            }
            else
            {
			    if ((_videoURL != null) && (_videoURL.Length > 0))
                {
                    SystemEventController.Instance.DispatchSystemEvent(POIVideoController.EventPOIVideoControllerMinimize, youTubePlayer.Time());
                    youTubePlayer.Pause();
                }
                else
                {
                    SystemEventController.Instance.DispatchSystemEvent(POIVideoController.EventPOIVideoControllerMinimize, (float)videoPlayer.time);
                    videoPlayer.Pause();
                }
            }
        }

        private void OnNextClicked()
        {
            if ((_videoURL != null) && (_videoURL.Length > 0))
            {
                youTubePlayer.Forward();
            }
            else
            {
                if (videoPlayer.time + POIVideoController.JumpTime < videoPlayer.clip.length)
                {
                    videoPlayer.time += POIVideoController.JumpTime;
                }
            }
        }

        private void OnPreviousClicked()
        {
            if ((_videoURL != null) && (_videoURL.Length > 0))
            {
                youTubePlayer.Backward();
            }
            else
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
        }

        public bool IsPlaying()
        {
            if ((_videoURL != null) && (_videoURL.Length > 0))
            {
                return youTubePlayer.IsPlaying();
            }
            else
            {
                return videoPlayer.isPlaying;
            }
        }

        public void Pause()
        {
            if ((_videoURL != null) && (_videoURL.Length > 0))
            {
                youTubePlayer.Pause();
            }
            else
            {
                videoPlayer.Pause();
            }
        }
        
        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(EventPOIVideoControllerMinimize) || nameEvent.Equals(EventPOIVideoControllerPlayDelayed))
            {
                if (_shouldPlay)
                {
                    float time = (float)parameters[0];
                    float duration = -1;
                    if (parameters.Length > 1)
                    {
                        duration = (float)parameters[1];
                    }

                    if ((_videoURL != null) && (_videoURL.Length > 0))
                    {
                        videoPlayer.gameObject.SetActive(false);
                        youTubePlayer.gameObject.SetActive(true);                        
                        youTubePlayer.JumpToTime(time);
                    }
                    else
                    {
                        youTubePlayer.gameObject.SetActive(false);
                        videoPlayer.gameObject.SetActive(true);
                        videoPlayer.time = time;
                        videoPlayer.Play();
                    }
                    videoControls.SetActive(true);
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
