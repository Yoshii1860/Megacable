namespace Dypsloom.RhythmTimeline.VR.Input
{
    using System.Collections.Generic;
    using Dypsloom.RhythmTimeline.Core.Input;
    using Dypsloom.RhythmTimeline.Core.Managers;
    using UnityEngine;
    using UnityEngine.Serialization;
    using Vector3 = System.Numerics.Vector3;


    /// <summary>
    /// Gets information from the RhythmDirector and from the input to processes notes.
    /// </summary>
    public class VRRhythmInputManager : MonoBehaviour
    {
        [Tooltip("The Rhythm Processor.")]
        [SerializeField] protected RhythmProcessor m_RhythmProcessor;
        
        [FormerlySerializedAs("m_VRRhythmInputColliders")]
        [SerializeField]
        protected VRRhythmInputTriggerBase[] m_VRRhythmInputTriggers;

        private void Awake()
        {
            for (int i = 0; i < m_VRRhythmInputTriggers.Length; i++) {
                var trigger = m_VRRhythmInputTriggers[i];
                if (trigger == null) {
                    continue;
                }

                trigger.OnTriggerEnterE += OnVRInputTriggerEnter;
                trigger.OnTriggerStayE += OnVRInputTriggerStay;
                trigger.OnTriggerExitE += OnVRInputTriggerExit;
            }
        }

        private void OnVRInputTriggerEnter(VRRhythmInputEventData inputData)
        {
            TriggerInput(inputData);
        }
        
        private void OnVRInputTriggerStay(VRRhythmInputEventData inputData)
        {
            TriggerInput(inputData);
        }

        private void OnVRInputTriggerExit(VRRhythmInputEventData inputData)
        {
            TriggerInput(inputData);
        }

        protected virtual void TriggerInput(InputEventData trackInputEventData)
        {
            m_RhythmProcessor.TriggerInput(trackInputEventData);
        }
    }
}