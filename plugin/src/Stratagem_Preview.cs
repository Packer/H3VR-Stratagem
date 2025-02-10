using UnityEngine;
using UnityEngine.UI;
using FistVR;

namespace Stratagem
{
    public class Stratagem_Preview : FVRPointable
    {
        [Header("Top Layer")]
        public GameObject topLayer;
        public Text title;
        public Image icon;
        public Image[] arrows;
        public GameObject arrowsParent;

        [Header("Description Layer")]
        public GameObject descriptionLayer;
        public Text description;

        [Header("Cooldown Layer")]
        public Text cooldown; //WORK THIS OUT, PROBABLY USE 3 LAYERS AND ADD UPDATE TO THE STRATAGEM GRENADE IF PREVIEW IS ENABLED*/
        [HideInInspector]
        public Stratagem_Package package;


        public override void OnPoint(FVRViveHand hand)
        {
            base.OnPoint(hand);
            topLayer.SetActive(false);
            descriptionLayer.SetActive(true);
        }

        public override void EndPoint(FVRViveHand hand)
        {
            base.EndPoint(hand);
            topLayer.SetActive(true);
            descriptionLayer.SetActive(false);
        }
    }
}
