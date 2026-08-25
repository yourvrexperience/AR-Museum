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
    public class POINarratorDestroyer : MonoBehaviour
    {
        private bool _inited = false;
        
        public void Init()
        {
            if (!_inited)
            {
                _inited = true;
                SystemEventController.Instance.Event += OnSystemEvent;
            }
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
                GameObject.Destroy(this.gameObject);
            }
            if (nameEvent.Equals(GameLevelData.EventGameLevelDataDestroyNarrationObjects))
            {
                GameObject.Destroy(this.gameObject);
            }
            if (nameEvent.Equals(NarrationToken.EventNarrationTokenDestroyNarrationObject))
            {
                GameObject.Destroy(this.gameObject);
            }
        }
    }
}
