using UnityEngine;
using UnityEngine.UI;

namespace yourvrexperience.VR
{
    public class Shift : Key
    {
        Text subscript;

        public override void Awake()
        {
            base.Awake();
            subscript = transform.Find("Subscript").GetComponent<Text>();
        }

        public override void ShiftKey()
        {
            var tmp = key.text;
            key.text = subscript.text;
            subscript.text = tmp;
        }
    }
}