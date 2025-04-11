using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class ParticleSpeedSignalHandler : MonoBehaviour, INotificationReceiver
{
    public ParticleSystemManager particleSystemManager;
    public ParticleSpeedSignal[] StarsSignals;
    public ParticleSpeedSignal[] WarpSignals;

    public void OnNotify(Playable origin, INotification notification, object context)
    {
        if (notification is SignalEmitter signalEmitter)
        {
            // Access the SignalAsset from the SignalEmitter using the 'asset' property
            SignalAsset signalAsset = signalEmitter.asset;
            if (signalAsset == null)
            {
                return;
            }

            // Check if the SignalAsset is a ParticleSpeedSignal
            if (signalAsset is ParticleSpeedSignal particleSpeedSignal)
            {
                if (particleSystemManager == null)
                {
                    return;
                }

                // Determine which signal was emitted and call the appropriate function
                foreach (var starsSignal in StarsSignals)
                {
                    if (particleSpeedSignal == starsSignal)
                    {
                        Debug.Log("Changing Stars Speed: " + particleSpeedSignal.speed + " Transition Time: " + particleSpeedSignal.transitionTime);
                        particleSystemManager.ChangeStarsSpeed(particleSpeedSignal.speed, particleSpeedSignal.transitionTime);
                        return;
                    }
                }

                foreach (var warpSignal in WarpSignals)
                {
                    if (particleSpeedSignal == warpSignal)
                    {
                        Debug.Log("Changing Warp Speed: " + particleSpeedSignal.speed + " Transition Time: " + particleSpeedSignal.transitionTime);
                        particleSystemManager.ChangeWarpSpeed(particleSpeedSignal.speed, particleSpeedSignal.transitionTime);
                        break;
                    }
                }
            }
        }
    }
}