using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;

using BepInEx;
using BepInEx.Configuration;

namespace CharaSelect
{
    [BepInPlugin("org.macdoniel.charaselect", "CharaSelect", "1.0.0")]
    public class CharaSelectPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> MenuHeight { get; private set; }
        public static ConfigEntry<int> ButtonHeight { get; private set; }
        public static ConfigEntry<int> Columns { get; private set; }

        void Awake()
        {
            Logger.LogInfo("Testing Plugin");
        }

        void Start()
        {
            MenuHeight = Config.Bind("Appearance", "Folder menu height", 100);
            ButtonHeight = Config.Bind("Appearance", "Button height", 30);
            Columns = Config.Bind("Appearance", "Columns", 2);

            var harmony = new Harmony("org.macdoniel.charaselect");

            harmony.PatchAll(typeof(CharaSelect.PatchListUI));
            harmony.PatchAll(typeof(CharaSelect.PatchHSceneUI));

            // harmony.PatchAll(typeof(CharaSelect.PatchCoordinateListUI));
            // harmony.PatchAll(typeof(CharaSelect.PatchHSceneCoords));
        }
    }
}
