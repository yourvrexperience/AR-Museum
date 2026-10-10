using System.Collections;
using System.Collections.Generic;
using LightShaft.Scripts;
using UnityEngine;
using UnityEngine.Video;

namespace yourvrexperience.template6dof
{
    public class YouTubeController : MonoBehaviour
    {
        private YoutubePlayer _player;
        private string _url;
        private VideoPlayer _videoPlayer;
        private bool _isPlaying = true;

        private void Awake()
        {
            _videoPlayer = GetComponentInChildren<VideoPlayer>();
            _player = GetComponentInChildren<YoutubePlayer>();
            _player.videoPlayer = _videoPlayer;            
        }

        public void FullScreen()
        {
            _videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            _videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            _videoPlayer.targetCamera = _player.mainCamera;                
        }

        public void Pause()
        {
            _player.Pause();
            _isPlaying = false;       
        }

        public bool IsPlaying()
        {
            return _isPlaying;
        }

        public float Duration()
        {
            return Mathf.RoundToInt(_videoPlayer.frameCount / _videoPlayer.frameRate);
        }

        public float Time()
        {
            return Mathf.RoundToInt(_videoPlayer.frame / _videoPlayer.frameRate);
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
            _player.SkipToPercent(progress);
            _player.Play();
            _isPlaying = true; 
        }

        public void Forward()
        {
            float progress = Progress();
            float jumpTo = progress + 0.01f;
            if (jumpTo > 1) jumpTo = 1;
            _player.SkipToPercent(jumpTo);
            _player.Play();
            _isPlaying = true; 
        }

        public void Backward()
        {
            float progress = Progress();
            float jumpTo = progress - 0.01f;
            if (jumpTo < 0) jumpTo = 0;            
            _player.SkipToPercent(jumpTo);
            _player.Play();
            _isPlaying = true; 
        }

        public void Play(string url)
        {
            _url = url;
            _player.videoQuality = YoutubePlayer.YoutubeVideoQuality.Standard;
            _player.Play(_url);
            _isPlaying = true; 
        }
    }
}
