namespace Dypsloom.RhythmTimeline.VR
{
    using Dypsloom.RhythmTimeline.Core.Input;
    using Dypsloom.RhythmTimeline.VR.Input;
    using UnityEngine;

    public class VRRhythmTriggerNote : VRRhythmNoteBase
    {
        public override void OnTriggerInput(InputEventData inputEventData)
        {
            var vrInputEvent = inputEventData as VRRhythmInputEventData;

            if (vrInputEvent == null) {
                Debug.LogError("The input event used is not compatible with the VR Notes");
                return;
            }
            
            if (m_InputTriggered == false && vrInputEvent.TriggerType != VRRhythmInputTriggerType.Enter) {
                return;
            }
            
            if (m_InputTriggered && vrInputEvent.TriggerType == VRRhythmInputTriggerType.Enter) {
                return;
            }
            
            if (m_InputTriggered == false && vrInputEvent.TriggerType == VRRhythmInputTriggerType.Enter) {
                
                if(CheckIfIgnore(vrInputEvent)){ return; }
                
                // First hit.
                m_InputTrackWrongID = inputEventData.TrackID != RhythmClipData.TrackID;
                m_InputTriggerIndex = vrInputEvent.m_VRRhythmInputTrigger.Index;
                m_InputTriggered = true;
                m_FirstInputTime = Time.time;
            };

            if (m_InputTriggered == false) {
                return;
            }
            
            
            m_FirstInputTime = Time.time;
            m_InputTriggerIndex = vrInputEvent.m_VRRhythmInputTrigger.Index;
            m_InputTrackWrongID = vrInputEvent.TrackID != RhythmClipData.TrackID;

            if (m_InputIndexToTriggerOption.TryGetValue(vrInputEvent.m_VRRhythmInputTrigger.Index, out TriggerOption triggerOption) == false) {
                Debug.LogWarning($"The input trigger index {vrInputEvent.m_VRRhythmInputTrigger.Index} was no defined for this note, please set it in the ignore list of the Note");
                return;
            }
            
            //The gameobject can be set to active false. It is returned to the pool automatically when reset.
            gameObject.SetActive(false);
            m_IsTriggered = true;

            switch (triggerOption) {
                case TriggerOption.Good:
                    TriggerGood(vrInputEvent);
                    break;
                case TriggerOption.Bad:
                    TriggerBad(vrInputEvent);
                    break;
            }
        }
    }
}