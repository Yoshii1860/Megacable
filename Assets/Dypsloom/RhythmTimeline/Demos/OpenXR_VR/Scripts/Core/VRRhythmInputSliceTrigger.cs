namespace Dypsloom.RhythmTimeline.VR.Input
{
    using System;
    using System.Collections.Generic;
    using Dypsloom.RhythmTimeline.Core.Input;
    using Dypsloom.RhythmTimeline.Core.Notes;
    using UnityEngine;

    [Serializable]
    public enum VRRhythmInputTriggerType
    {
        None,
        Enter,
        Stay,
        Exit,
        Other
    }
    
    public class VRRhythmInputEventData : InputEventData
    {
        public VRRhythmInputTriggerBase m_VRRhythmInputTrigger;
        public VRRhythmInputTriggerType TriggerType;
        public Vector3 Velocity;
        public RaycastHit RaycastHit;
    }

    public class VRRhythmInputSliceTrigger : VRRhythmInputTriggerBase
    {
        private struct TwoPoint
        {
            public Vector3 StartPoint;
            public Vector3 EndPoint;
        }
        
        [SerializeField]
        protected bool m_DebugRay;
        
        [SerializeField]
        protected Color m_Color = Color.red;
        
        [SerializeField]
        protected LayerMask m_NoteColliderLayerMask = 0;
        
        [SerializeField]
        [Tooltip("The rigidbody attached to this object.")]
        protected Rigidbody m_Rigidbody;
        
        [SerializeField]
        [Tooltip("The point where the ray for detecting notes starts.")]
        protected Transform m_RayStart;
        
        [SerializeField]
        [Tooltip("The point where the ray for detecting notes end.")]
        protected Transform m_RayEnd;
        
        [SerializeField]
        [Tooltip("When moving fast, especially rotating fast the input with low frames, you may miss notes. Increase the RayInterpolationExtraSampleCount to estimate interpolation points where the start/ends would have passed. This increase input hit precision.")]
        protected int m_RayInterpolationExtraSampleCount = 2;
        
        
        public int Index => m_Index;
        public Vector3 RayCastDirection => m_RayEnd.position - m_RayStart.position;

        protected VRRhythmInputEventData m_CachedEventData = new VRRhythmInputEventData();
        
        List<Collider> m_CollidersToExit = new List<Collider>();
        
        RaycastHit[] m_CachedRaycastHitResult = new RaycastHit[10];
        
        Dictionary<Collider, RaycastHit> m_PreviousRaycastHits = new Dictionary<Collider, RaycastHit>();
        
        List<TwoPoint> m_PreviousFramesPositions = new List<TwoPoint>();
        List<RaycastHit> m_RaycastHits = new List<RaycastHit>();
        

        private void FixedUpdate()
        {
            m_PreviousFramesPositions.Add(new TwoPoint()
            {
                StartPoint = m_RayStart.position,
                EndPoint = m_RayEnd.position
            });

            var count = 0;
            
            m_RaycastHits.Clear();
            for (var i = 0; i < m_PreviousFramesPositions.Count; i++) {
                var points = m_PreviousFramesPositions[i];
                var nextPoints = points;
                if (i != m_PreviousFramesPositions.Count - 1) {
                    nextPoints = m_PreviousFramesPositions[i+1];
                } 
                
                var samplePerPosCount = 1 + Mathf.Max(0, m_RayInterpolationExtraSampleCount);

                for (int j = 0; j < samplePerPosCount; j++) {
                    var startPoint = Vector3.Lerp(points.StartPoint, nextPoints.StartPoint, (float)j / samplePerPosCount);
                    var endPoint = Vector3.Lerp(points.EndPoint, nextPoints.EndPoint, (float)j / samplePerPosCount);

                    var direction = endPoint - startPoint;
                    var sampleCount = Physics.RaycastNonAlloc(
                        new Ray(startPoint, direction),
                        m_CachedRaycastHitResult,
                        Vector3.Distance(startPoint, endPoint),
                        m_NoteColliderLayerMask,
                        QueryTriggerInteraction.UseGlobal
                    );

                    if (m_DebugRay) {
                        Debug.DrawLine(startPoint,endPoint, m_Color, 1f);
                    }
                    
                    
                    for (int k = 0; k < sampleCount; k++) {
                        var hitThisFixedUpdate = m_CachedRaycastHitResult[k];
                        var foundMatch = false;
                        foreach (var otherHitThisFixedUpdate in m_RaycastHits) {
                            count++;
                            
                            if (otherHitThisFixedUpdate.collider == hitThisFixedUpdate.collider) {
                                foundMatch = true;
                                break;
                            }
                        }

                        if (foundMatch == false) {
                            m_RaycastHits.Add(hitThisFixedUpdate);
                        }
                    }
                }
            }

            m_CollidersToExit.Clear();
            
            
            // Exit Notes
            foreach (var keyValuePair in m_PreviousRaycastHits) {
                var foundMatch = false;

                for (int i = 0; i < m_RaycastHits.Count; i++) {
                    var raycastHit = m_RaycastHits[i];
                    if (keyValuePair.Value.collider == raycastHit.collider) {
                        foundMatch = true;
                    }
                }

                if (foundMatch == false) {
                    m_CollidersToExit.Add(keyValuePair.Key);
                }
            }

            foreach (var colliderToExit in m_CollidersToExit) {

                var rayCastHit = m_PreviousRaycastHits[colliderToExit];
                SubmitExitEvent(rayCastHit);
                m_PreviousRaycastHits.Remove(colliderToExit);
            }
            

            for (int i = 0; i < m_RaycastHits.Count; i++) {
                var raycastHit = m_RaycastHits[i];
                
                if (m_PreviousRaycastHits.TryGetValue(raycastHit.collider, out var previousHit) == false) {
                    // First time Enter
                    SubmitEnterEvent(raycastHit);
                    m_PreviousRaycastHits[raycastHit.collider] = raycastHit;
                    return;
                }

                m_PreviousRaycastHits[raycastHit.collider] = raycastHit;
                
                SubmitStayEvent(raycastHit);
            }
            
            m_PreviousFramesPositions.Clear();
            
            m_PreviousFramesPositions.Add(new TwoPoint()
            {
                StartPoint = m_RayStart.position,
                EndPoint = m_RayEnd.position
            });
        }

        private void Update()
        {
            m_PreviousFramesPositions.Add(new TwoPoint()
            {
                StartPoint = m_RayStart.position,
                EndPoint = m_RayEnd.position
            });
        }

        private bool TryPrepareEventFor(RaycastHit raycastHit, out VRRhythmInputEventData eventData)
        {
            eventData = m_CachedEventData;
            if (raycastHit.collider == null) {
                return false;
            }

            //attachedRigidbody can be null if the gameobject is inactive (Almost always the case for exiting)
            var colliderAttachedRigidbody = raycastHit.collider.attachedRigidbody;
            if(colliderAttachedRigidbody == null) {
                colliderAttachedRigidbody = raycastHit.collider.GetComponentInParent<Rigidbody>(true);
                if (colliderAttachedRigidbody == null) {
                    return false;
                }
            }
            
            var note = colliderAttachedRigidbody.GetComponentInParent<Note>(true);
            if (note == null) {
                return false;
            }
            
            m_CachedEventData.TrackID = note.RhythmClipData.TrackID;
            m_CachedEventData.m_VRRhythmInputTrigger = this;
            m_CachedEventData.Note = note;
            m_CachedEventData.RaycastHit = raycastHit;
            m_CachedEventData.Velocity = m_Rigidbody.GetRelativePointVelocity(raycastHit.point);

            return true;
        }
        
        private void SubmitEnterEvent(RaycastHit raycastHit)
        {
            if (TryPrepareEventFor(raycastHit, out var eventData) == false) {
                return;
            }
            InvokeEnterEvent(eventData);
        }
        
        private void SubmitStayEvent(RaycastHit raycastHit)
        {
            if (TryPrepareEventFor(raycastHit, out var eventData) == false) {
                return;
            }
            InvokeStayEvent(eventData);
        }

        private void SubmitExitEvent(RaycastHit raycastHit)
        {
            if (TryPrepareEventFor(raycastHit, out var eventData) == false) {
                return;
            }
            InvokeExitEvent(eventData);
        }
    }
}