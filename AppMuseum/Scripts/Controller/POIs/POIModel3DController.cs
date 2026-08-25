using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using yourvrexperience.Narration;
using yourvrexperience.Utils;
using static yourvrexperience.Narration.NarrationController;

namespace yourvrexperience.template6dof
{
    public class POIModel3DController : MonoBehaviour
    {
        private bool _inited = false;
        private bool _isEasterEgg = false;
        private Vector3 _scale;

        public Vector3 Scale
        {
            get { return _scale; }
            set { 
                _scale = value;
                this.transform.localScale = _scale;
            }
        }

        public void Play(bool isEasterEgg, string assetName, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _isEasterEgg = isEasterEgg;
            if (!_inited)
            {
                _inited = true;

                GameObject model3D = AssetBundleController.Instance.CreateGameObject(assetName);
                model3D.transform.parent = this.transform;
                model3D.transform.localPosition = Vector3.zero;
                _scale = scale;

                this.transform.parent = parent;
                this.transform.localPosition = position;
                this.transform.localRotation = rotation;
                this.transform.localScale = _scale;

                SystemEventController.Instance.Event += OnSystemEvent;
            }
        }

        public void PlayAnimation(string animationName)
        {
            AnimatorSystem animator = this.GetComponentInChildren<AnimatorSystem>();
            if (animator != null)
            {
                animator.ChangeAnimation(animationName);
            }
        }

        private void OnDestroy()
        {
            _inited = false;
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
        }

        private void OnSystemEvent(string nameEvent, object[] parameters)
        {
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
    }
}
