using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using yourvrexperience.Utils;
using static yourvrexperience.template6dof.LevelView;

namespace yourvrexperience.template6dof
{
    public class PanelVideoEffectView : MonoBehaviour
    {
        public const string PanelVideoEffectViewStarted = "PanelVideoEffectViewStarted";
        public const string PanelVideoEffectViewCompleted = "PanelVideoEffectViewCompleted";

        private string _anim;
        private EasterEgg _easterEgg;

        private void Start()
        {
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        private void OnDestroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;            
        }

        public void Play(string anim, EasterEgg easterEgg)
        {
            _easterEgg = easterEgg;
            _anim = anim;
            if ((_easterEgg != null) && ((_easterEgg.Star != null))) _easterEgg.Star.SetActive(false);
            SystemEventController.Instance.DispatchSystemEvent(PanelVideoEffectViewStarted, _anim);
            Invoke("OnAnimationCompleted", 1f);
        }

        private void OnAnimationCompleted()
        {
            SystemEventController.Instance.DispatchSystemEvent(PanelVideoEffectViewCompleted, _anim);
            GameObject.Destroy(this.gameObject);
            if ((_easterEgg != null) && ((_easterEgg.Star != null)))
            {
                _easterEgg.ShowStar();
            }
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
            if (nameEvent.Equals(LevelView.EventLevelViewPlayEasterEgg))
            {
                OnAnimationCompleted();
                _easterEgg.Target.SetActive(false);
            }
        }
    }
}