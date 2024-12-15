using UnityEngine;

namespace InfinityPLATEAU
{
    public class PLATEAUAnchor : MonoBehaviour
    {
        [SerializeField]
        private double lat, lon;

        [SerializeField]
        private float height;

        // Start is called before the first frame update
        void Start()
        {
            var pos = PLATEAUManager.CalcOffsetPos(lat, lon) + new Vector3(0f, height, 0f);
            transform.localPosition = pos;
        }
    }
}
