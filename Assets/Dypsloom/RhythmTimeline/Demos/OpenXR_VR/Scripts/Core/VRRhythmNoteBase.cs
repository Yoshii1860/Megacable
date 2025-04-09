namespace Dypsloom.RhythmTimeline.VR
{
    using System;
    using System.Collections.Generic;
    using Dypsloom.RhythmTimeline.Core.Notes;
    using Dypsloom.RhythmTimeline.Core.Playables;
    using Dypsloom.RhythmTimeline.VR.Input;
    using Dypsloom.Shared.Utility;
    using UnityEngine;

    public abstract class VRRhythmNoteBase : Note, INoteRhythmClipOnSceneGUIChange
    {
        [Serializable]
        public enum TriggerOption
        {
            Ignore,
            Bad,
            Good,
        }
        
        [SerializeField]
        [Tooltip("If no triggers, then invoke miss event.")]
        protected bool MissOnDeactivateWithNoTrigger = true;

        [SerializeField]
        [Tooltip("Whether the note should be triggered by the wrong track.")]
        protected TriggerOption m_WrongTrackTriggerOption = TriggerOption.Good;
        
        
        [SerializeField]
        [Tooltip("A list on trigger indexes that will trigger the note with a good (example red note triggered by red sword).")]
        protected int[] m_GoodTriggerIndexes;
        
        [SerializeField]
        [Tooltip("A list on trigger indexes that will trigger the note with a bad (example red note triggered by blue sword).")]
        protected int[] m_BadTriggerIndexes;
        
        [SerializeField]
        [Tooltip("a list of indexes that can be ignored without giving warnings.")]
        protected int[] m_IgnoreTriggerIndexes;


        protected Dictionary<int, TriggerOption> m_InputIndexToTriggerOption = new Dictionary<int, TriggerOption>();

        protected float m_FirstInputTime;
        protected bool m_InputTrackWrongID;
        protected int m_InputTriggerIndex;
        protected bool m_InputTriggered;
        

        protected override void Awake()
        {
            base.Awake();
            foreach (var goodTriggerIndex in m_GoodTriggerIndexes) {
                if (m_InputIndexToTriggerOption.TryAdd(goodTriggerIndex, TriggerOption.Good) == false) {
                    Debug.LogWarning($"The same index {goodTriggerIndex} is specified in two lists. Please make sure each index is unique per list.");
                }
            }
            foreach (var badTriggerIndex in m_BadTriggerIndexes) {
                if (m_InputIndexToTriggerOption.TryAdd(badTriggerIndex, TriggerOption.Bad) == false) {
                    Debug.LogWarning($"The same index {badTriggerIndex} is specified in two lists. Please make sure each index is unique per list.");
                }
            }
            foreach (var ignoreTriggerIndex in m_IgnoreTriggerIndexes) {
                if (m_InputIndexToTriggerOption.TryAdd(ignoreTriggerIndex, TriggerOption.Ignore) == false) {
                    Debug.LogWarning($"The same index {ignoreTriggerIndex} is specified in two lists. Please make sure each index is unique per list.");
                }
            }
        }

        /// <summary>
        /// The note is initialized when it is added to the top of a track.
        /// </summary>
        /// <param name="rhythmClipData">The rhythm clip data.</param>
        public override void Initialize(RhythmClipData rhythmClipData)
        {
            base.Initialize(rhythmClipData);
            m_FirstInputTime = 0;
            m_InputTriggered = false;
            m_InputTriggerIndex = -1;
            m_InputTrackWrongID = false;
        }

        /// <summary>
        /// The note needs to be deactivated when it is out of range from being triggered.
        /// This usually happens when the clip ends.
        /// </summary>
        protected override void DeactivateNote()
        {
            base.DeactivateNote();

            //Only send the trigger miss event during play mode.
            if(Application.isPlaying == false){return;}
            
            if (MissOnDeactivateWithNoTrigger && m_IsTriggered == false) {
                InvokeNoteTriggerEventMiss();
            }
        }

        public bool CheckIfIgnore(VRRhythmInputEventData vrInputEvent)
        {
            //Hit with the wrong track.
            var wrongTrack = vrInputEvent.TrackID != RhythmClipData.TrackID;
            if (wrongTrack) {
                switch (m_WrongTrackTriggerOption) {
                    case TriggerOption.Ignore:
                        return true;
                }
            }

            if (m_InputIndexToTriggerOption.TryGetValue(vrInputEvent.m_VRRhythmInputTrigger.Index, out TriggerOption triggerOption) ==
                false) {
                Debug.LogWarning($"The input trigger index {vrInputEvent.m_VRRhythmInputTrigger.Index} was no defined for this note, please set it in the ignore list of the Note");
                return true;
            }
            
            switch (triggerOption) {
                case TriggerOption.Ignore:
                    return true;
            }

            return false;
        }
        

        // In the score settings we've defined bad as being worse than 100%
        protected void TriggerGood(VRRhythmInputEventData inputEventData)
        {
            //You may compute the perfect time anyway you want.
            //In this case the perfect time is half of the clip.
            var perfectTime = m_RhythmClipData.RealDuration / 2f;
            var timeDifference = TimeFromActivate - perfectTime - (m_FirstInputTime - Time.time)/2f;
            var timeDifferencePercentage =  Mathf.Clamp((float)(Mathf.Abs((float)(100f*timeDifference)) / perfectTime),0f,99f);
            
            //Debug.Log("Good:"+perfectTime + " / " + timeDifference + " / " + timeDifferencePercentage);
            
            //Send a trigger event such that the score system can listen to it.
            InvokeNoteTriggerEvent(inputEventData, timeDifference, (float) timeDifferencePercentage);
            RhythmClipData.TrackObject.RemoveActiveNote(this);
        }
        
        // In the score settings we've defined bad as being worse than 100%
        protected void TriggerBad(VRRhythmInputEventData inputEventData)
        {
            //Force a bad
            
            //Send a trigger event such that the score system can listen to it.
            InvokeNoteBadTriggerEvent(inputEventData, -1, (float) 100f);
            RhythmClipData.TrackObject.RemoveActiveNote(this);
        }
    
        /// <summary>
        /// Hybrid Update is updated both in play mode, by update or timeline, and edit mode by the timeline. 
        /// </summary>
        /// <param name="timeFromStart">The time from reaching the start of the clip.</param>
        /// <param name="timeFromEnd">The time from reaching the end of the clip.</param>
        protected override void HybridUpdate(double timeFromStart, double timeFromEnd)
        {
            //Compute the perfect timing.
            var perfectTime = m_RhythmClipData.RealDuration / 2f;
            var deltaT = (float)(timeFromStart - perfectTime);

            //Compute the position of the note using the delta T from the perfect timing.
            //Here we use the direction of the track given at delta T.
            //You can easily curve all your notes to any trajectory, not just straight lines, by customizing the TrackObjects.
            //Here the target position is found using the track object end position.
            
            //We use the Vector2 paramater for offseting the note.
            var offset = m_RhythmClipData.ClipParameters.Vector2Parameter;
            
            var endPoint = m_RhythmClipData.TrackObject.EndPoint.position + new Vector3(offset.x, offset.y);
            var startPoint = m_RhythmClipData.TrackObject.StartPoint.position + new Vector3(offset.x, offset.y);
            var direction = (endPoint - startPoint).normalized;
            var distance = deltaT * m_RhythmClipData.RhythmDirector.NoteSpeed;
            var targetPosition = endPoint;
        
            //Using those parameters we can easily compute the new position of the note at any time.
            var newPosition = targetPosition + (direction * distance);
            transform.position = newPosition;
        }
        
#if UNITY_EDITOR
        
        /// <summary>
        /// Editor extsion to add handles in the scene view when selecting the rhythm clip.
        /// </summary>
        /// <param name="sceneView"></param>
        /// <param name="mainSelectedClip"></param>
        /// <param name="selectedClips"></param>
        /// <returns></returns>
        public virtual bool RhythmClipOnSceneGUIChange(UnityEditor.SceneView sceneView, RhythmClip mainSelectedClip,
            List<RhythmClip> selectedClips)
        {
            UnityEditor.EditorGUI.BeginChangeCheck();
            var clipOffset = mainSelectedClip.ClipParameters.Vector2Parameter;
            
            var rhythmClipData = mainSelectedClip.RhythmClipData;
            if (rhythmClipData.IsValid == false) { return false;}
            
            var trackObject = rhythmClipData.TrackObject;
            if (trackObject == null) { return false; }
            
            var noteOriginalPosition = trackObject.EndPoint.position + new Vector3(clipOffset.x,clipOffset.y,0);
            
            Vector3 newTargetPosition = UnityEditor.Handles.PositionHandle(noteOriginalPosition, Quaternion.identity);
            if (UnityEditor.EditorGUI.EndChangeCheck())
            {
                var deltaPos = newTargetPosition - noteOriginalPosition;

                if (deltaPos == Vector3.zero)
                {
                    return false;
                }
                
                foreach (var otherRhythmClip in selectedClips)
                {
                    UnityEditor.Undo.RecordObject(otherRhythmClip, "Change Target Position");
                    otherRhythmClip.ClipParameters.Vector2Parameter += new Vector2(deltaPos.x, deltaPos.y);
                }

                return false;
            }

            return false;
        }
#endif
    }
}