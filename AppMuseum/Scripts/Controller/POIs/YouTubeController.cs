using System.Collections;
using System.Collections.Generic;
#if ENABLE_YOUTUBE
using LightShaft.Scripts;
#endif
using UnityEngine;
using UnityEngine.Video;

namespace yourvrexperience.template6dof
{
    public class YouTubeController : MonoBehaviour
    {
#if ENABLE_YOUTUBE        
        private YoutubePlayer _player;
#endif        
        private string _url;
        private VideoPlayer _videoPlayer;
        private bool _isPlaying = true;

        private void Awake()
        {
            _videoPlayer = GetComponentInChildren<VideoPlayer>();
#if ENABLE_YOUTUBE
            _player = GetComponentInChildren<YoutubePlayer>();
            _player.videoPlayer = _videoPlayer;
#endif
        }

        public void FullScreen()
        {
#if ENABLE_YOUTUBE
            _videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            _videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            _videoPlayer.targetCamera = _player.mainCamera;                
#endif
        }

        public void Pause()
        {
#if ENABLE_YOUTUBE            
            _player.Pause();
            _isPlaying = false;       
#endif            
        }

        public bool IsPlaying()
        {
            return _isPlaying;
        }

        public float Duration()
        {
#if ENABLE_YOUTUBE                        
            return Mathf.RoundToInt(_videoPlayer.frameCount / _videoPlayer.frameRate);
#else
            return 0;
#endif            
        }

        public float Time()
        {
#if ENABLE_YOUTUBE
            return Mathf.RoundToInt(_videoPlayer.frame / _videoPlayer.frameRate);
#else
            return 0;
#endif
        }

        private float Progress()
        {
            return Time() / Duration();
        }

        public void JumpToTime(float time, float duration = -1)
        {   
            float progress = 0;         
            if (duration != -1)
            {
                progress = time / duration;    
            }
            else
            {
                progress = time / Duration();
            }       
#if ENABLE_YOUTUBE
            _player.SkipToPercent(progress);
            _player.Play();
#endif            
            _isPlaying = true; 
        }

        public void Forward()
        {
            float progress = Progress();
            float jumpTo = progress + 0.01f;
            if (jumpTo > 1) jumpTo = 1;
#if ENABLE_YOUTUBE            
            _player.SkipToPercent(jumpTo);
            _player.Play();
#endif            
            _isPlaying = true; 
        }

        public void Backward()
        {
            float progress = Progress();
            float jumpTo = progress - 0.01f;
            if (jumpTo < 0) jumpTo = 0;   
#if ENABLE_YOUTUBE                        
            _player.SkipToPercent(jumpTo);
            _player.Play();
#endif                        
            _isPlaying = true; 
        }

        public void Play(string url)
        {
            _url = url;
#if ENABLE_YOUTUBE            
            _player.videoQuality = YoutubePlayer.YoutubeVideoQuality.Standard;
            _player.Play(_url);
#endif            
            _isPlaying = true; 
        }
    }
}
