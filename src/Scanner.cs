using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LootScanner;

/// <summary>
/// H scans the local player's room: its valuables get the game's discover outline, a map marker and a label with their
/// value. J toggles a checklist of everything scanned on this level, plus totals for the whole map.
/// </summary>
internal class Scanner : MonoBehaviour
{
    private enum Status { Waiting, Secured, Broken }

    private class Entry
    {
        public ValuableObject Valuable;
        public string Name;
        public float Value;
        public float LabelUntil;
        public Status Status;
    }

    private const int MaxLines = 12;

    private readonly List<Entry> entries = new List<Entry>();
    private ValuableDirector director;
    private bool checklistVisible;
    private float nextScan;
    private string message = "";
    private float messageUntil;

    private int mapCount, foundCount;
    private float mapValue, foundValue, nextTotals;

    private GUIStyle textStyle;

    private static bool InLevel => SemiFunc.RunIsLevel() && ValuableDirector.instance;

    private static bool MenuOpen => MenuManager.instance && MenuManager.instance.currentMenuPage;

    private void Update()
    {
        if (!InLevel)
        {
            director = null;
            entries.Clear();
            return;
        }

        if (director != ValuableDirector.instance)
        {
            director = ValuableDirector.instance;
            entries.Clear();
            checklistVisible = Plugin.ShowChecklist.Value;
            nextScan = 0f;
        }

        UpdateEntries();
        if (Time.unscaledTime >= nextTotals)
        {
            nextTotals = Time.unscaledTime + 0.5f;
            UpdateTotals();
        }

        if (MenuOpen || !SemiFunc.NoTextInputsActive()) return;
        if (Input.GetKeyDown(Plugin.ChecklistKey.Value)) checklistVisible = !checklistVisible;
        if (Input.GetKeyDown(Plugin.ScanKey.Value)) Scan();
    }

    private void Scan()
    {
        if (Time.unscaledTime < nextScan) return;

        PlayerAvatar me = SemiFunc.PlayerAvatarLocal();
        if (!me || !me.RoomVolumeCheck || me.deadSet || me.isDisabled || !ValuableDiscover.instance) return;
        nextScan = Time.unscaledTime + Plugin.Cooldown.Value;

        // One room on the map can be made of several volumes, so the map module counts as the same room.
        var rooms = me.RoomVolumeCheck.CurrentRooms.Where(r => r).ToList();
        var modules = new HashSet<MapModule>(rooms.Where(r => r.MapModule).Select(r => r.MapModule));
        if (rooms.Count == 0)
        {
            Say("Scan failed: you're not in a room.");
            return;
        }

        int count = 0;
        float total = 0f;
        foreach (ValuableObject valuable in ValuableDirector.instance.valuableList)
        {
            if (!valuable || !valuable.roomVolumeCheck || !valuable.physGrabObject) continue;

            valuable.roomVolumeCheck.CheckSet();
            bool here = valuable.roomVolumeCheck.CurrentRooms.Any(r => r && (rooms.Contains(r) || (r.MapModule && modules.Contains(r.MapModule))));
            if (!here) continue;

            valuable.Discover(valuable.discovered ? ValuableDiscoverGraphic.State.Reminder : ValuableDiscoverGraphic.State.Discover);

            Entry entry = entries.FirstOrDefault(e => e.Valuable == valuable);
            if (entry == null)
            {
                entry = new Entry { Valuable = valuable, Name = CleanName(valuable.name) };
                entries.Add(entry);
            }
            entry.Value = valuable.dollarValueCurrent;
            entry.LabelUntil = Time.unscaledTime + Plugin.LabelSeconds.Value;

            count++;
            total += valuable.dollarValueCurrent;
        }

        Say(count == 0 ? "Scan: no loot in this room." : $"Scan: {count} item{(count == 1 ? "" : "s")}, {Money(total)}");
        Plugin.Log.LogInfo($"Scanned {count} valuables worth {total} in {rooms.Count} room volume(s).");
    }

    private void UpdateEntries()
    {
        foreach (Entry entry in entries)
        {
            if (entry.Status == Status.Broken) continue;

            ValuableObject valuable = entry.Valuable;
            if (!valuable)
            {
                // The game removes loot when it's sold, as well as when it breaks. Loot last seen secured counts as sold.
                if (entry.Status != Status.Secured) entry.Status = Status.Broken;
                continue;
            }

            entry.Value = valuable.dollarValueCurrent;
            RoomVolumeCheck check = valuable.roomVolumeCheck;
            entry.Status = check && (check.inTruck || check.inExtractionPoint) ? Status.Secured : Status.Waiting;
        }
    }

    private void UpdateTotals()
    {
        mapCount = foundCount = 0;
        mapValue = foundValue = 0f;
        foreach (ValuableObject valuable in ValuableDirector.instance.valuableList)
        {
            if (!valuable) continue;
            mapCount++;
            mapValue += valuable.dollarValueCurrent;
            if (!valuable.discovered) continue;
            foundCount++;
            foundValue += valuable.dollarValueCurrent;
        }
    }

    private void Say(string text)
    {
        message = text;
        messageUntil = Time.unscaledTime + 4f;
    }

    private static string CleanName(string name)
    {
        name = name.Replace("(Clone)", "").Trim();
        if (name.StartsWith("Valuable ")) name = name.Substring("Valuable ".Length);
        return name;
    }

    private static string Money(float value) => "$" + Mathf.RoundToInt(value).ToString("N0");

    private static Color ValueColor(float value) =>
        value >= 20000f ? new Color(1f, 0.76f, 0.2f) :
        value >= 5000f ? new Color(0.45f, 0.95f, 0.5f) :
        Color.white;

    // ---------- drawing ----------

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || !InLevel || MenuOpen) return;
        textStyle ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Overflow };

        float scale = Mathf.Clamp(Screen.height / 1080f, 0.6f, 2f);
        DrawLabels(scale);
        if (checklistVisible) DrawChecklist(scale);

        if (Time.unscaledTime < messageUntil)
        {
            int size = Mathf.RoundToInt(22f * scale);
            Text(new Rect(0f, Screen.height * 0.72f, Screen.width, size * 1.6f), message, size, TextAnchor.MiddleCenter, Color.white);
        }
        GUI.color = Color.white;
    }

    private void DrawLabels(float scale)
    {
        CameraUtils utils = CameraUtils.Instance;
        Camera cam = utils && utils.MainCamera ? utils.MainCamera : Camera.main;
        if (!cam) return;

        float now = Time.unscaledTime;
        foreach (Entry entry in entries)
        {
            if (now >= entry.LabelUntil || !entry.Valuable || !entry.Valuable.physGrabObject) continue;

            Vector3 viewport = cam.WorldToViewportPoint(entry.Valuable.physGrabObject.centerPoint);
            if (viewport.z < 0.1f) continue;
            var pos = new Vector2(viewport.x * Screen.width, (1f - viewport.y) * Screen.height);

            float k = scale * Mathf.Lerp(1f, 0.7f, Mathf.InverseLerp(3f, 25f, viewport.z));
            float alpha = Mathf.Clamp01((entry.LabelUntil - now) / 1.5f);
            int nameSize = Mathf.RoundToInt(15f * k), valueSize = Mathf.RoundToInt(19f * k);

            Color nameColor = new Color(1f, 1f, 1f, alpha);
            Color valueColor = ValueColor(entry.Value);
            valueColor.a = alpha;
            Text(new Rect(pos.x - 150f, pos.y - nameSize * 1.5f, 300f, nameSize * 1.5f), entry.Name, nameSize, TextAnchor.LowerCenter, nameColor);
            Text(new Rect(pos.x - 150f, pos.y, 300f, valueSize * 1.5f), Money(entry.Value), valueSize, TextAnchor.UpperCenter, valueColor);
        }
    }

    private void DrawChecklist(float scale)
    {
        var sorted = entries.OrderBy(e => e.Status).ThenByDescending(e => e.Value).ToList();
        int lines = Mathf.Min(sorted.Count, MaxLines);
        bool more = sorted.Count > MaxLines;

        float width = 330f * scale, lineHeight = 24f * scale, pad = 12f * scale;
        int rows = 4 + lines + (more ? 1 : 0) + (sorted.Count == 0 ? 1 : 0);
        var panel = new Rect(Screen.width - width - 20f * scale, 90f * scale, width, rows * lineHeight + pad * 2f);

        GUI.color = new Color(0.03f, 0.03f, 0.05f, 0.72f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);

        int size = Mathf.RoundToInt(15f * scale);
        float x = panel.x + pad, w = width - pad * 2f, y = panel.y + pad;

        Row(ref y, lineHeight, x, w, "LOOT CHECKLIST", "", size, new Color(1f, 0.82f, 0.25f), Color.white);
        Row(ref y, lineHeight, x, w, $"On the map: {mapCount}", Money(mapValue), size, Color.white, Color.white);
        Row(ref y, lineHeight, x, w, $"Found: {foundCount}", Money(foundValue), size, Color.white, Color.white);
        Row(ref y, lineHeight, x, w, $"Not found: {mapCount - foundCount}", Money(mapValue - foundValue), size, Color.white, Color.white);

        if (sorted.Count == 0)
            Row(ref y, lineHeight, x, w, $"Press {Plugin.ScanKey.Value} to scan this room", "", size, new Color(0.8f, 0.8f, 0.8f), Color.white);

        for (int i = 0; i < lines; i++)
        {
            Entry entry = sorted[i];
            string box = entry.Status == Status.Secured ? "[x] " : entry.Status == Status.Broken ? "[-] " : "[ ] ";
            Color color = entry.Status == Status.Waiting ? Color.white : new Color(0.6f, 0.6f, 0.6f);
            Color value = entry.Status == Status.Waiting ? ValueColor(entry.Value) : color;
            string price = entry.Status == Status.Broken ? "broken" : Money(entry.Value);
            Row(ref y, lineHeight, x, w, box + entry.Name, price, size, color, value);
        }

        if (more)
            Row(ref y, lineHeight, x, w, $"+{sorted.Count - MaxLines} more", "", size, new Color(0.8f, 0.8f, 0.8f), Color.white);
    }

    private void Row(ref float y, float height, float x, float width, string left, string right, int size, Color leftColor, Color rightColor)
    {
        var rect = new Rect(x, y, width, height);
        Text(rect, left, size, TextAnchor.MiddleLeft, leftColor);
        if (right.Length > 0) Text(rect, right, size, TextAnchor.MiddleRight, rightColor);
        y += height;
    }

    private void Text(Rect rect, string text, int size, TextAnchor anchor, Color color)
    {
        textStyle.fontSize = size;
        textStyle.alignment = anchor;

        textStyle.normal.textColor = new Color(0f, 0f, 0f, color.a);
        GUI.color = Color.white;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, textStyle);

        textStyle.normal.textColor = color;
        GUI.Label(rect, text, textStyle);
    }
}
