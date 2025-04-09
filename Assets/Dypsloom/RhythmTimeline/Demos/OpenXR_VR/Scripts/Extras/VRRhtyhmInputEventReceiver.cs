using System;
using Dypsloom.RhythmTimeline.VR.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class VRRhtyhmInputEventReceiver : MonoBehaviour
{
    [Tooltip("The note to listen the events on.")]
    [SerializeField] protected VRRhythmInputTriggerBase m_VRInputTrigger;
    
    [Tooltip("The transform to place at the collision or raycast hit point.")]
    [SerializeField] protected Transform m_FollowHitPoint;
    
    
    [FormerlySerializedAs("m_OnTriggerEnter")]
    [Tooltip("The event when the trigger enters.")]
    [SerializeField] protected UnityEvent m_OnNoteTriggerEnter;
    [FormerlySerializedAs("m_OnTriggerStay")]
    [Tooltip("The event when the trigger stays.")]
    [SerializeField] protected UnityEvent m_OnNoteTriggerStay;
    [FormerlySerializedAs("m_OnTriggerExit")]
    [Tooltip("The event when the trigger exits.")]
    [SerializeField] protected UnityEvent m_OnNoteTriggerExit;
    
   

    private void Awake()
    {
        if (m_VRInputTrigger == null)
        {
            Debug.LogError("No VRInputTrigger assigned to " + name + "!", this);
            enabled = false;
            return;
        }
        
        m_VRInputTrigger.OnTriggerEnterE += HandleVRInputTriggerEnter;
        m_VRInputTrigger.OnTriggerStayE += HandleVRInputTriggerStay;
        m_VRInputTrigger.OnTriggerExitE += HandleVRInputTriggerExit;
    }

    private void HandleVRInputTriggerEnter(VRRhythmInputEventData obj)
    {
        m_OnNoteTriggerEnter.Invoke();
        PlaceTransformAtHitPoint(obj);
    }
    
    private void HandleVRInputTriggerStay(VRRhythmInputEventData obj)
    {
        m_OnNoteTriggerStay.Invoke();
        PlaceTransformAtHitPoint(obj);
    }
    
    private void HandleVRInputTriggerExit(VRRhythmInputEventData obj)
    {
        m_OnNoteTriggerExit.Invoke();
        PlaceTransformAtHitPoint(obj);
    }
    
    private void PlaceTransformAtHitPoint(VRRhythmInputEventData obj)
    {
        if (m_FollowHitPoint == null)
        {
            return;
        }

        if (obj.RaycastHit.point != Vector3.zero)
        {
            m_FollowHitPoint.position = obj.RaycastHit.point;
            return;
            
        }
    }
}
