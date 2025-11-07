using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace StenchofDeath.Patches;

[HarmonyPatch]
public class StenchOfDeathPatch
{
    private const float TIME_TO_ROT = 60f;
    private const float STENCH_DURATION = 30f;
    private const float EMISSION_MULTIPLIER = 10f;
    private const float AUDIO_MULTIPLIER = 0.1f;
    private const int POOL_MAX_SIZE = 32;

    private class StenchData
    {
        public DeadBodyInfo? Body;
        public GameObject? Flies;
        public Transform? FliesTransform;
        public ParticleSystem? ParticleSystem;
        public ParticleSystem.EmissionModule EmissionModule;
        public AudioSource? AudioSource;
        public float TimeSinceDeath;
        public float TimeSinceRotten;
        public bool BodyRotten;
        
        public void Reset()
        {
            Body = null;
            Flies = null;
            FliesTransform = null;
            ParticleSystem = null;
            AudioSource = null;
            TimeSinceDeath = 0f;
            TimeSinceRotten = 0f;
            BodyRotten = false;
        }
    }
    
    private static readonly List<StenchData> _activeData = new(32);
    private static readonly Stack<StenchData> _dataPool = new(POOL_MAX_SIZE);

    private static StenchData GetPooledData()
    {
        if (_dataPool.Count > 0)
        {
            var data = _dataPool.Pop();
            data.Reset();
            return data;
        }
        return new StenchData();
    }

    private static void ReturnToPool(StenchData data)
    {
        if (_dataPool.Count < POOL_MAX_SIZE)
        {
            data.Reset();
            _dataPool.Push(data);
        }
    }
    
    [HarmonyPatch(typeof(DeadBodyInfo), "Start")]
    [HarmonyPostfix]
    private static void StartPostfix(DeadBodyInfo __instance)
    {
        var data = GetPooledData();
        data.Body = __instance;
        _activeData.Add(data);

        data.Flies = Object.Instantiate(Assets.FlyPrefab, __instance.transform, true);
        data.FliesTransform = data.Flies!.transform;
        data.FliesTransform.localPosition = Vector3.zero;
        data.FliesTransform.localScale = Vector3.one;
        
        data.ParticleSystem = data.Flies.GetComponent<ParticleSystem>();
        data.EmissionModule = data.ParticleSystem.emission;
        data.EmissionModule.rateOverTime = 0f;
        data.ParticleSystem.Play();
        
        data.AudioSource = data.Flies.GetComponent<AudioSource>();
        data.AudioSource.volume = 0f;
        data.AudioSource.Play();
    }

    [HarmonyPatch(typeof(DeadBodyInfo), "Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(DeadBodyInfo __instance)
    {
        StenchData? data = null;
        for (var i = 0; i < _activeData.Count; i++)
        {
            if (_activeData[i].Body != __instance) continue;
            data = _activeData[i];
            break;
        }
        
        if (data == null)
            return;

        data.TimeSinceDeath += Time.deltaTime;
        
        if (!data.BodyRotten)
        {
            if (!(data.TimeSinceDeath >= TIME_TO_ROT)) return;
            data.BodyRotten = true;
            data.TimeSinceRotten = data.TimeSinceDeath;
            return;
        }
        
        var rotDuration = data.TimeSinceDeath - data.TimeSinceRotten;
        if (rotDuration > STENCH_DURATION)
            return;

        var stench = rotDuration / STENCH_DURATION;
        data.EmissionModule.rateOverTime = stench * EMISSION_MULTIPLIER;
        data.AudioSource!.volume = stench * AUDIO_MULTIPLIER;
    }

    [HarmonyPatch(typeof(DeadBodyInfo), "OnDestroy")]
    [HarmonyPostfix]
    private static void OnDestroyPostfix(DeadBodyInfo __instance)
    {
        for (var i = _activeData.Count - 1; i >= 0; i--)
        {
            if (_activeData[i].Body != __instance) continue;
            var data = _activeData[i];
                
            if (data.Flies != null)
                Object.Destroy(data.Flies);
                
            _activeData.RemoveAt(i);
            ReturnToPool(data);
            return;
        }
    }
}