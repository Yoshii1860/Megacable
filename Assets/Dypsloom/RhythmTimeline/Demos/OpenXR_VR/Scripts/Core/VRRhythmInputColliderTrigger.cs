namespace Dypsloom.RhythmTimeline.VR.Input
{
    using System;
    using Dypsloom.RhythmTimeline.Core.Notes;
    using UnityEngine;

    public class VRRhythmInputColliderTrigger : VRRhythmInputTriggerBase
    {
        [SerializeField]
        protected Rigidbody m_Rigidbody;
        
       

        protected VRRhythmInputEventData m_CachedEventData = new VRRhythmInputEventData();
        
        /*public void FixedUpdate()
        {
            Physics.ComputePenetration()
            m_Collider.bounds.Intersects()
            Physics.over
        }*/

        private void OnTriggerEnter(Collider other)
        {
            SubmitEnterEvent(other);
        }
        
        private void OnTriggerExit(Collider other)
        {
            SubmitStayEvent(other);
        }
        
        private void OnTriggerStay(Collider other)
        {
            SubmitStayEvent(other);
        }

        private bool TryPrepareEventFor(Collider other, out VRRhythmInputEventData eventData)
        {
            eventData = m_CachedEventData;
            if (other.attachedRigidbody == null) {
                return false;
            }
            
            var note = other.attachedRigidbody.GetComponentInParent<Note>();
            if (note == null) {
                return false;
            }
            
            m_CachedEventData.TrackID = note.RhythmClipData.TrackID;
            m_CachedEventData.m_VRRhythmInputTrigger = this;
            m_CachedEventData.Note = note;
            m_CachedEventData.RaycastHit = new RaycastHit();
            m_CachedEventData.Velocity = m_Rigidbody.linearVelocity;

            return true;
        }
        
        private void SubmitEnterEvent(Collider collider)
        {
            if (TryPrepareEventFor(collider, out var eventData) == false) {
                return;
            }
            InvokeEnterEvent(eventData);
        }
        
        private void SubmitStayEvent(Collider collider)
        {
            if (TryPrepareEventFor(collider, out var eventData) == false) {
                return;
            }
            
            InvokeStayEvent(eventData);
        }

        private void SubmitExitEvent(Collider collider)
        {
            if (TryPrepareEventFor(collider, out var eventData) == false) {
                return;
            }
            InvokeExitEvent(eventData);
        }
    }
}