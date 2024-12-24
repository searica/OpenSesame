// Ignore Spelling: CameraTweaks Jotunn

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using System.Reflection;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace OpenSesame;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
internal sealed class OpenSesame : BaseUnityPlugin
{
    internal const string Author = "Searica";
    public const string PluginName = "OpenSesame";
    public const string PluginGUID = $"{Author}.Valheim.{PluginName}";
    public const string PluginVersion = "0.1.0";

    public void Awake()
    {
        Log.Init(Logger);



        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), harmonyInstanceId: PluginGUID);
        Game.isModded = true;

    }

    public void OnDestroy() { }
}


[HarmonyPatch]
internal static class DoorPatches
{
    /// <summary>
    ///     Claim ownership before trying to open a door.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Door), nameof(Door.Open))]
    public static void Door_Open_Prefix(Door __instance)
    {
        if (!__instance || !__instance.m_nview || !__instance.m_nview.IsValid())
        {
            return;
        }
        __instance.m_nview.ClaimOwnership();
    }
}

/// <summary>
///     Log level to control output to BepInEx log
/// </summary>
internal enum LogLevel
{
    Low = 0,
    Medium = 1,
    High = 2,
}

/// <summary>
///     Helper class for properly logging from static contexts.
/// </summary>
internal static class Log
{
    #region Verbosity

    internal static ConfigEntry<LogLevel> Verbosity { get; set; }
    internal static LogLevel VerbosityLevel => Verbosity.Value;
    internal static bool IsVerbosityLow => Verbosity.Value >= LogLevel.Low;
    internal static bool IsVerbosityMedium => Verbosity.Value >= LogLevel.Medium;
    internal static bool IsVerbosityHigh => Verbosity.Value >= LogLevel.High;

    #endregion Verbosity

    private static ManualLogSource logSource;

    internal static void Init(ManualLogSource logSource)
    {
        Log.logSource = logSource;
    }

    internal static void LogDebug(object data) => logSource.LogDebug(data);

    internal static void LogError(object data) => logSource.LogError(data);

    internal static void LogFatal(object data) => logSource.LogFatal(data);

    internal static void LogMessage(object data) => logSource.LogMessage(data);

    internal static void LogWarning(object data) => logSource.LogWarning(data);

    internal static void LogInfo(object data, LogLevel level = LogLevel.Low)
    {
        if (Verbosity is null || VerbosityLevel >= level)
        {
            logSource.LogInfo(data);
        }
    }

    internal static void LogGameObject(GameObject prefab, bool includeChildren = false)
    {
        LogInfo("***** " + prefab.name + " *****");
        foreach (Component compo in prefab.GetComponents<Component>())
        {
            LogComponent(compo);
        }

        if (!includeChildren) { return; }

        LogInfo("***** " + prefab.name + " (children) *****");
        foreach (Transform child in prefab.transform)
        {
            if (!child) { continue; }

            LogInfo($" - {child.name}");
            foreach (Component compo in child.GetComponents<Component>())
            {
                LogComponent(compo);
            }
        }
    }

    internal static void LogComponent(Component compo)
    {
        if (!compo) { return; }
        try
        {
            LogInfo($"--- {compo.GetType().Name}: {compo.name} ---");
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            Log.LogWarning("Could not get type name for component!");
            return;
        }

        try
        {
            List<PropertyInfo> properties = AccessTools.GetDeclaredProperties(compo.GetType());
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    LogInfo($" - {property.Name} = {property.GetValue(compo)}");
                }
                catch (Exception ex)
                {
                    Log.LogError(ex.ToString());
                    Log.LogWarning($"Could not get property: {property.Name} for component!");
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            Log.LogWarning("Could not get properties for component!");
        }

        try
        {
            List<FieldInfo> fields = AccessTools.GetDeclaredFields(compo.GetType());
            foreach (FieldInfo field in fields)
            {
                try
                {
                    LogInfo($" - {field.Name} = {field.GetValue(compo)}");
                }
                catch (Exception ex)
                {
                    Log.LogError(ex.ToString());
                    Log.LogWarning($"Could not get field: {field.Name} for component!");
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogError(ex.ToString());
            Log.LogWarning("Could not get fields for component!");
        }

    }
}
