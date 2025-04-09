namespace Dypsloom.RhythmTimeline.VR
{
    using Dypsloom.Shared;
    using Dypsloom.Shared.Utility;
    using UnityEngine;

    public class VRRhythmNoteMeshSlicesContainer : MonoBehaviour
    {
        [SerializeField]
        protected float m_LifeTime = 20;

        protected float m_Timer;

        protected virtual void OnEnable()
        {
            m_Timer = 0;
            for (int i = gameObject.transform.childCount - 1; i >= 0; i--) {
                var child = gameObject.transform.GetChild(i);
                Destroy(child.gameObject);
            }
        }

        protected virtual void Update()
        {
            m_Timer += Time.deltaTime;
            if (m_Timer >= m_LifeTime) {
                PoolManager.Destroy(gameObject, true);
            }
        }

        public virtual void AddSlice(GameObject sliceGO)
        {
            sliceGO.transform.SetParent(transform);
        }
    }
}