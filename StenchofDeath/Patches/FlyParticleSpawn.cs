using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace StenchofDeath.Patches;

[HarmonyPatch]
public class StenchOfDeathPatch
{
    // Dictionary to store stench data per instance
    private static Dictionary<DeadBodyInfo, StenchData> _stenchDataMap = new();

    private class StenchData
    {
        public GameObject? Flies;
        public float TimeSinceDeath;
        public float TimeSinceRotten;
        public ParticleSystem.EmissionModule EmissionModule;
        public AudioSource? AudioSource;
        public bool BodyRotten;
        public float Stench;
        public const float Multiplier = 1f;
    }
    
    [HarmonyPatch(typeof(DeadBodyInfo), "Start")]
    [HarmonyPostfix]
    private static void StartPostfix(DeadBodyInfo __instance)
    {
        StenchofDeath.Logger.LogDebug("Starting stench on body: " + __instance.name);
        var data = new StenchData();
        _stenchDataMap[__instance] = data;

        data.Flies = Object.Instantiate(Assets.FlyPrefab, __instance.transform, true);
        data.Flies!.transform.localPosition = Vector3.zero;
        data.Flies.transform.localScale = Vector3.one;
        
        var component = data.Flies.GetComponent<ParticleSystem>();
        data.EmissionModule = component.emission;
        data.EmissionModule.rateOverTime = 0.0f;
        component.Play();
        
        data.AudioSource = data.Flies.GetComponent<AudioSource>();
        data.AudioSource.volume = 0.0f;
        data.AudioSource.Play();
    }

    [HarmonyPatch(typeof(DeadBodyInfo), "Update")]
    [HarmonyPostfix]
    private static void UpdatePostfix(DeadBodyInfo __instance)
    {
        if (!_stenchDataMap.TryGetValue(__instance, out var data))
            return;

        data.TimeSinceDeath += Time.deltaTime;
        
        // magic numbers go brr
        if (data is { TimeSinceDeath: >= 60, BodyRotten: false })
        {
            data.BodyRotten = true;
            data.TimeSinceRotten = data.TimeSinceDeath;
        }
        
        if (!data.BodyRotten || data.TimeSinceDeath - data.TimeSinceRotten > 30.0f)
            return;

        data.Stench = ((data.TimeSinceDeath - data.TimeSinceRotten) / 30.0f) * StenchData.Multiplier;
        data.EmissionModule.rateOverTime = (int)(data.Stench * 10);
        if (data.AudioSource != null) data.AudioSource.volume = data.Stench * 0.1f;
    }

    [HarmonyPatch(typeof(DeadBodyInfo), "OnDestroy")]
    [HarmonyPostfix]
    private static void OnDestroyPostfix(DeadBodyInfo __instance)
    {
        if (!_stenchDataMap.TryGetValue(__instance, out var data)) return;
        if (data.Flies != null)
            Object.Destroy(data.Flies);
        _stenchDataMap.Remove(__instance);
    }
}