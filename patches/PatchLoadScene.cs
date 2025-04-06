using BepInEx.Logging;
using FairyGUI;
using HarmonyLib;
using System;
using System.Collections.Generic;
using THTD;
using THTD.Units;
using UI.Game;
using UnityEngine;

namespace THMDAllTowers.Patches
{
    [HarmonyPatch(typeof(THTD.Utils), "LoadScene", [typeof(string)])]
    public static class PatchLoadScene
    {
        internal static ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(PatchLoadScene));

        private static readonly KeyCode[] buildKeys =
        [
        KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.LeftBracket, KeyCode.RightBracket,
        KeyCode.G, KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.Semicolon, KeyCode.Quote,
        KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M, KeyCode.Comma, KeyCode.Period,
        ];

        private static readonly HashSet<KeyCode> buildKeySet = new(buildKeys);

        public static EventCallback1 onKeyDownHandler { get; internal set; } = null!;

        [HarmonyPrefix]
        public static void Prefix(ref string name)
        {
            if (name.StartsWith("Level") || name.StartsWith("AnimLevel"))
            {
                Logger.LogInfo("Level load detected. Setting up key bindings...");

                var keyToUnitMap = new Dictionary<KeyCode, Tower>();

                var towers = MapGame.Instance.towers;
                for (int i = 0; i < Math.Min(buildKeys.Length, towers.Length); i++)
                {
                    keyToUnitMap[buildKeys[i]] = towers[i];
                }

                onKeyDownHandler = evt =>
                {
                    var key = evt.inputEvent.keyCode;
                    if (buildKeySet.Contains(key) && keyToUnitMap.TryGetValue(key, out var unit))
                    {
                        if (Game.Instance.CanBuild(unit))
                        {
                            Game.Instance.Build(unit);
                            UI_Game.Instance.HideStoreUI();
                            Logger.LogInfo($"Built unit: {unit.name} with key: {key}");
                        }
                        else
                        {
                            Logger.LogInfo($"Cannot build unit: {unit.name} with key: {key}");
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
