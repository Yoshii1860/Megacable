using UnityEngine;
using UnityEngine.Timeline;

[CreateAssetMenu(fileName = "ParticleSpeedSignal", menuName = "Timeline Signals/ParticleSpeedSignal", order = 1)]
public class ParticleSpeedSignal : SignalAsset
{
    // Parameters to pass through the signal
    public float speed = 1.0f;
    public float transitionTime = 1.0f;
}