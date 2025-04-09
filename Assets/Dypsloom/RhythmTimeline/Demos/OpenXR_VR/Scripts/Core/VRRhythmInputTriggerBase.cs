namespace Dypsloom.RhythmTimeline.VR.Input
{
    using System;
    using UnityEngine;

    public abstract class VRRhythmInputTriggerBase : MonoBehaviour
    {
        public event Action<VRRhythmInputEventData> OnTriggerEnterE;
        public event Action<VRRhythmInputEventData> OnTriggerStayE;
        public event Action<VRRhythmInputEventData> OnTriggerExitE;
        
        [SerializeField]
        protected int m_Index;
        public int Index => m_Index;
        
        public void InvokeEnterEvent(VRRhythmInputEventData eventData)
        {
            eventData.TriggerType = VRRhythmInputTriggerType.Enter;
            OnTriggerEnterE?.Invoke(eventData);
        }
        
        
        public void InvokeStayEvent(VRRhythmInputEventData eventData)
        {
            eventData.TriggerType = VRRhythmInputTriggerType.Stay;
            OnTriggerStayE?.Invoke(eventData);
        }
        
        public void InvokeExitEvent(VRRhythmInputEventData eventData)
        {
            eventData.TriggerType = VRRhythmInputTriggerType.Exit;
            OnTriggerExitE?.Invoke(eventData);
        }
    }
}