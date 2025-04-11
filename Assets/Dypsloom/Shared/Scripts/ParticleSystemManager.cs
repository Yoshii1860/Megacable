using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParticleSystemManager : MonoBehaviour
{
    public static ParticleSystemManager Instance { get; private set; }

    public ParticleSystem StarsParticles;
    public ParticleSystem[] WarpParticles;
    private float StarsStartSpeed;
    private float WarpStartSpeed;
    private bool isWarpPlaying = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (StarsParticles != null)
        {
            StarsStartSpeed = StarsParticles.main.simulationSpeed;
            StarsParticles.Pause(true);
        }

        if (WarpParticles != null)
        {
            WarpStartSpeed = WarpParticles[0].main.simulationSpeed;
            foreach (var warpPS in WarpParticles)
            {
                warpPS.Stop(true);
            }
        }
    }

    public void StartStars()
    {
        Debug.Log("Start Stars");
        StarsParticles.Play(true);
    }

    public void StartWarp()
    {
        Debug.Log("Start Warp");
        foreach (var warpPS in WarpParticles)
        {
            warpPS.Play(true);
        }
    }

    public void StopStars()
    {
        Debug.Log("Stop Stars");
        StarsParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void StopWarp()
    {
        Debug.Log("Stop Warp");
        foreach (var warpPS in WarpParticles)
        {
            warpPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void ResetPS()
    {
        Debug.Log("Reset PS");
        StarsParticles.Pause(true);
        var mainModule = StarsParticles.main;
        mainModule.simulationSpeed = StarsStartSpeed;
        foreach (var warpPS in WarpParticles)
        {
            warpPS.Stop(true);
            var warpPSMain = warpPS.main;
            warpPSMain.simulationSpeed = WarpStartSpeed;
        }
    }

    public void PausePS()
    {
        Debug.Log("Pause PS");
        StarsParticles.Pause(true);
        if (WarpParticles[0].isPlaying)
        {
            isWarpPlaying = true;
            foreach (var warpPS in WarpParticles)
            {
                    warpPS.Pause(true);
            }
        }
        else
        {
            isWarpPlaying = false;
        }
    }

    public void UnpausePS()
    {
        Debug.Log("Unpause PS");
        StarsParticles.Play(true);
        if (isWarpPlaying)
        {
            foreach (var warpPS in WarpParticles)
            {
                warpPS.Play(true);
            }
        }
    }

    public void ChangeStarsSpeed(float speed, float t)
    {
        StartCoroutine(SpeedTransition(StarsParticles, speed, t));
    }

    public void ChangeWarpSpeed(float speed, float t)
    {
        foreach (var warpPS in WarpParticles)
        {
            StartCoroutine(SpeedTransition(warpPS, speed, t));
        }
    }

    IEnumerator SpeedTransition(ParticleSystem ps, float speed, float t)
    {
        ParticleSystem.MainModule main = ps.main;
        float startSpeed = main.simulationSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < t)
        {
            main.simulationSpeed = Mathf.Lerp(startSpeed, speed, elapsedTime / t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        main.simulationSpeed = speed;   
    }
}
