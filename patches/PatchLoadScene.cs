using BepInEx.Logging;
using FairyGUI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using THTD;
using THTD.Units;
using UI.Game;
using UnityEngine;

namespace THMDAllTowers.Patches
{
    struct KeyShiftPair : IEquatable<KeyShiftPair>
    {
        public KeyCode Key;
        public bool Shift;

        public KeyShiftPair(KeyCode key, bool shift)
        {
            Key = key;
            Shift = shift;
        }

        public bool Equals(KeyShiftPair other) => Key == other.Key && Shift == other.Shift;

        public override bool Equals(object obj) => obj is KeyShiftPair other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (int)Key;
                hash = hash * 23 + Shift.GetHashCode();
                return hash;
            }
        }
    }

    [HarmonyPatch(typeof(THTD.Utils), "LoadScene", [typeof(string)])]
    public static class PatchLoadScene
    {
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(PatchLoadScene));

        private static readonly KeyCode[] buildKeys =
        [
            KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.LeftBracket, KeyCode.RightBracket,
            KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote,
            KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period, KeyCode.Slash
        ];

        private static readonly HashSet<KeyCode> buildKeySet = new(buildKeys);

        private static readonly KeyCode NextLevelKey = KeyCode.Equals; // '+' (on US keyboards it's usually 'Equals' with shift)
        private static readonly KeyCode PrevLevelKey = KeyCode.Minus;

        private static readonly string[] Levels = ["Lv1", "Lv2", "Lv3", "Lv4", "Lv5", "Lv6"];
        private static int currentLevel = 0;

        private static Dictionary<KeyShiftPair, Tower> keyToUnitMap = new();

        public static EventCallback1 onKeyDownHandler { get; internal set; } = null!;

        private static void SwitchTowerLevel(int delta)
        {
            currentLevel = (currentLevel + delta + Levels.Length) % Levels.Length;
            string levelSuffix = Levels[currentLevel];

            var towers = Resources.FindObjectsOfTypeAll<Tower>()
                .Where(t => t.data.id.EndsWith(levelSuffix))
                .OrderBy(t => t.data.model)
                .ToArray();

            keyToUnitMap.Clear();

            int totalTowers = Math.Min(towers.Length, buildKeys.Length * 2);
            for (int i = 0; i < totalTowers; i++)
            {
                var shift = i >= buildKeys.Length;
                var key = buildKeys[i % buildKeys.Length];
                keyToUnitMap[new KeyShiftPair(key, shift)] = towers[i];
            }

            Logger.LogInfo($"Switched to tower level: {levelSuffix}");
        }

        [HarmonyPrefix]
        public static void Prefix(ref string name)
        {

            if (name.StartsWith("Level") || name.StartsWith("AnimLevel"))
            {
                Logger.LogInfo("Level load detected. Setting up key bindings...");

                SwitchTowerLevel(0); // Initialize to Lv1

                onKeyDownHandler = evt =>
                {
                    var key = evt.inputEvent.keyCode;
                    var shift = evt.inputEvent.shift;

                    if (key == NextLevelKey)
                    {
                        SwitchTowerLevel(1);
                        return;
                    }

                    if (key == PrevLevelKey)
                    {
                        SwitchTowerLevel(-1);
                        return;
                    }

                    if (buildKeySet.Contains(key) && keyToUnitMap.TryGetValue(new KeyShiftPair(key, shift), out var tower))
                    {
                        if (Game.Instance.CanBuild(tower))
                        {
                            Game.Instance.Build(tower);
                            UI_Game.Instance.HideStoreUI();
                            Logger.LogInfo($"Built unit: {tower.data.model} with key: {key}, shift: {shift}");
                        }
                        else
                        {
                            Logger.LogInfo($"Cannot build unit: {tower.data.model} with key: {key}, shift: {shift}");
                        }
                    }
                };

                Stage.inst.onKeyDown.Add(onKeyDownHandler);
            }
            else
            {
                Logger.LogInfo("Non-level scene detected. Removing key bindings.");
                Stage.inst.onKeyDown.Remove(onKeyDownHandler);
            }
        }
    }
}
