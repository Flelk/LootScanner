using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace LootScanner;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource Log;
    internal static ConfigEntry<KeyCode> ScanKey;
    internal static ConfigEntry<KeyCode> ChecklistKey;
    internal static ConfigEntry<float> LabelSeconds;
    internal static ConfigEntry<float> Cooldown;
    internal static ConfigEntry<bool> ShowChecklist;

    private void Awake()
    {
        Log = Logger;

        ScanKey = Config.Bind("Scanner", "ScanKey", KeyCode.H, "Scans the room you're in.");
        LabelSeconds = Config.Bind("Scanner", "LabelSeconds", 12f,
            new ConfigDescription("How long item labels stay up after a scan.", new AcceptableValueRange<float>(2f, 60f)));
        Cooldown = Config.Bind("Scanner", "Cooldown", 3f,
            new ConfigDescription("Seconds between scans.", new AcceptableValueRange<float>(0f, 60f)));
        ChecklistKey = Config.Bind("Checklist", "ChecklistKey", KeyCode.J, "Hides or shows the loot checklist.");
        ShowChecklist = Config.Bind("Checklist", "ShowChecklist", true, "Show the loot checklist when a level starts.");

        var go = new GameObject("LootScanner");
        go.hideFlags = HideFlags.HideAndDontSave;
        DontDestroyOnLoad(go);
        go.AddComponent<Scanner>();

        Log.LogInfo($"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} loaded.");
    }
}
