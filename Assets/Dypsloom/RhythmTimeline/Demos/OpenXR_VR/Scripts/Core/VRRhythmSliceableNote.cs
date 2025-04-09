namespace Dypsloom.RhythmTimeline.VR
{
    using System;
    using System.Collections.Generic;
    using Dypsloom.RhythmTimeline.Core.Input;
    using Dypsloom.RhythmTimeline.Core.Notes;
    using Dypsloom.RhythmTimeline.Core.Playables;
    using Dypsloom.RhythmTimeline.VR.Input;
    using Dypsloom.Shared.Utility;
    using EzySlice;
    using UnityEngine;
    using UnityEngine.Serialization;

    public class VRRhythmSliceableNote : VRRhythmNoteBase
    {

        [Header("Mesh Slicing")]
        
        [SerializeField]
        [Tooltip("The mesh that will be sliced")]
        private MeshFilter m_MeshToSlice;
        
        [SerializeField]
        [Tooltip("The mesh that will be sliced")]
        private VRRhythmNoteMeshSlicesContainer m_MeshSliceContainerPrefab;
        
        [SerializeField]
        [Tooltip("The material to use in the sliced up procedural mesh UV")]
        private Material m_SliceMaterial;
        
        [SerializeField]
        [Tooltip("The force applied to the pieces when sliced")]
        private float m_SliceForce = 10f;
        
        [SerializeField]
        [Tooltip("The force applied to the pieces when sliced")]
        private float m_SliceForceRadius = 1f;
        
        [SerializeField]
        [Tooltip("The maximum time it takes for the mesh to be sliced if the input trigger does not exit before then.")]
        private float m_CutMeshMaxTime = 0.3f;
        
        [SerializeField]
        [Tooltip("This transform will follow the Hit Directions, can be used for effects.")]
        private Transform m_FollowHitDirection;
        
        protected RaycastHit m_FirstHitPoint;
        protected RaycastHit m_LastHitPoint;
        protected Vector3 m_VelocitySum;
        protected int m_InputSampleCount;
        
        /// <summary>
        /// The note is initialized when it is added to the top of a track.
        /// </summary>
        /// <param name="rhythmClipData">The rhythm clip data.</param>
        public override void Initialize(RhythmClipData rhythmClipData)
        {
            base.Initialize(rhythmClipData);
            m_InputSampleCount = 0;
            m_VelocitySum = Vector3.zero;
        }
        
        /// <summary>
        /// An input was triggered on this note.
        /// The input event data has the information about what type of input was triggered.
        /// </summary>
        /// <param name="inputEventData">The input event data.</param>
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
                m_FirstHitPoint = vrInputEvent.RaycastHit;
                m_FirstInputTime = Time.time;
                m_VelocitySum = vrInputEvent.Velocity;
                m_InputSampleCount = 1;
            };

            if (m_InputTriggered == false) {
                return;
            }

            //Not the same input that started
            if (vrInputEvent.m_VRRhythmInputTrigger.Index != m_InputTriggerIndex) {
                return;
            }
            
            m_LastHitPoint = vrInputEvent.RaycastHit;
            m_VelocitySum += vrInputEvent.Velocity;
            m_InputSampleCount += 1;

            if (vrInputEvent.TriggerType == VRRhythmInputTriggerType.Exit) {
                SliceNote(vrInputEvent);
                return;
            }

            if (m_FirstInputTime + m_CutMeshMaxTime > Time.time) {
                SliceNote(vrInputEvent);
                return;
            }
        }

        private void SliceNote(VRRhythmInputEventData vrInputEvent)
        {
            //The gameobject can be set to active false. It is returned to the pool automatically when reset.
            gameObject.SetActive(false);
            m_IsTriggered = true;
            
            
            //We use the Vector2 paramater for offseting the note.
            var offset = m_RhythmClipData.ClipParameters.Vector2Parameter;
            
            var endPoint = m_RhythmClipData.TrackObject.EndPoint.position + new Vector3(offset.x, offset.y);
            var startPoint = m_RhythmClipData.TrackObject.StartPoint.position + new Vector3(offset.x, offset.y);
            var noteDirection = (endPoint - startPoint).normalized;


            var vrRhythmInputSliceTrigger = vrInputEvent.m_VRRhythmInputTrigger as VRRhythmInputSliceTrigger;
            if(vrRhythmInputSliceTrigger == null){
                Debug.LogWarning($"The input trigger used '{vrInputEvent.m_VRRhythmInputTrigger}' is not compatible with the Sliceable VR Notes");
                return;
            }
            
            var swordVector = vrRhythmInputSliceTrigger.RayCastDirection;
            var velocityVector = (m_VelocitySum / m_InputSampleCount) + noteDirection;

            var normal = Vector3.Cross(swordVector, velocityVector).normalized;

            if (m_FollowHitDirection != null) {
                m_FollowHitDirection.position = m_LastHitPoint.point;
                m_FollowHitDirection.right = normal;
            }

            SpawnMeshSlices(m_LastHitPoint.point, normal);


            //Hit with the wrong track.
            if (m_InputTrackWrongID) {
                switch (m_WrongTrackTriggerOption) {
                    case TriggerOption.Ignore:
                        return;
                    case TriggerOption.Bad:
                        TriggerBad(vrInputEvent);
                        return;
                    case TriggerOption.Good:
                        //Nothing
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            if (m_InputIndexToTriggerOption.TryGetValue(vrRhythmInputSliceTrigger.Index, out TriggerOption triggerOption) ==
                false) {
                Debug.LogWarning($"The input trigger index {vrRhythmInputSliceTrigger.Index} was no defined for this note, please set it in the ignore list of the Note");
                return;
            }
            
            switch (triggerOption) {
                case TriggerOption.Ignore:
                    return;
                case TriggerOption.Bad:
                    TriggerBad(vrInputEvent);
                    return;
                case TriggerOption.Good:
                    //Nothing
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            TriggerGood(vrInputEvent);
        }

        protected virtual void SpawnMeshSlices(Vector3 point, Vector3 normal)
        {
            bool hasContainer = false;
            VRRhythmNoteMeshSlicesContainer slicesContainer = null;
            if (m_MeshSliceContainerPrefab != null) {
                slicesContainer = PoolManager.Instantiate(m_MeshSliceContainerPrefab.gameObject)
                    .GetComponent<VRRhythmNoteMeshSlicesContainer>();
                hasContainer = slicesContainer != null;
            }

            var slices = m_MeshToSlice.gameObject.SliceInstantiate(point, normal, m_SliceMaterial);
            if (slices == null) {
                //Debug.LogError($"Could not cut, why!!!!!????: point:{point} normal:{normal}");
                return;
            }
            foreach (var slice in slices) {

                slice.transform.position = m_MeshToSlice.transform.position;
                slice.transform.rotation = m_MeshToSlice.transform.rotation;
                
                var sliceRigidbody = slice.AddComponent<Rigidbody>();
                var sliceCollider = slice.AddComponent<MeshCollider>();
                sliceCollider.convex = true;
                
                sliceRigidbody.AddExplosionForce(m_SliceForce,m_LastHitPoint.point,m_SliceForceRadius);

                if (hasContainer) {
                    slicesContainer.AddSlice(slice);
                }
            }
            
        }
    }
}