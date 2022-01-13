using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using KMod;
using PeterHan.PLib.Actions;
using PeterHan.PLib.Core;
using PeterHan.PLib.Database;
using PeterHan.PLib.Options;
using UnityEngine;

namespace Pliers
{
    public class PliersMod: UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            PUtil.InitLibrary();
            new PLocalization().Register();
            var options = new POptions();
            options.RegisterOptions(this, typeof(PliersConfig));
            
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            string currentAssemblyDirectory = Path.GetDirectoryName(currentAssembly.Location);

            PliersAssets.PLIERS_PATH_CONFIGFOLDER = Path.Combine(currentAssemblyDirectory, "config");
            PliersAssets.PLIERS_PATH_CONFIGFILE = Path.Combine(PliersAssets.PLIERS_PATH_CONFIGFOLDER, "config.json");
            PliersAssets.PLIERS_PATH_KEYCODESFILE = Path.Combine(PliersAssets.PLIERS_PATH_CONFIGFOLDER, "keycodes.txt");

            PliersAssets.PLIERS_ICON_SPRITE = Utilities.CreateSpriteDXT5(Assembly.GetExecutingAssembly().GetManifestResourceStream("Pliers.images.image_wirecutter_button.dds"), 32, 32);
            PliersAssets.PLIERS_ICON_SPRITE.name = PliersAssets.PLIERS_ICON_NAME;
            PliersAssets.PLIERS_VISUALIZER_SPRITE = Utilities.CreateSpriteDXT5(Assembly.GetExecutingAssembly().GetManifestResourceStream("Pliers.images.image_wirecutter_visualizer.dds"), 256, 256);

            PliersAssets.PLIERS_OPENTOOL = new PActionManager().CreateAction("Pliers.opentool", "Pliers", new PKeyBinding(KKeyCode.None, Modifier.None));

            Debug.Log("Pliers Loaded: Version " + currentAssembly.GetName().Version);
        }
    }
    
    [HarmonyPatch(typeof(PlayerController), "OnPrefabInit")]
    public static class PlayerController_OnPrefabInit {
        public static void Postfix(PlayerController __instance) {
            List<InterfaceTool> interfaceTools = new List<InterfaceTool>(__instance.tools);


            GameObject pliersTool = new GameObject(PliersAssets.PLIERS_TOOLNAME);
            pliersTool.AddComponent<PliersTool>();

            pliersTool.transform.SetParent(__instance.gameObject.transform);
            pliersTool.gameObject.SetActive(true);
            pliersTool.gameObject.SetActive(false);

            interfaceTools.Add(pliersTool.GetComponent<InterfaceTool>());


            __instance.tools = interfaceTools.ToArray();
        }
    }
    
    [HarmonyPatch(typeof(ToolMenu), "OnPrefabInit")]
    public static class ToolMenu_OnPrefabInit {
        public static void Postfix()
        {
            if (Assets.Sprites.ContainsKey(PliersAssets.PLIERS_ICON_SPRITE.name))
                Assets.Sprites.Remove(PliersAssets.PLIERS_ICON_SPRITE.name);
            Assets.Sprites.Add(PliersAssets.PLIERS_ICON_SPRITE.name, PliersAssets.PLIERS_ICON_SPRITE);
        }
    }

    [HarmonyPatch(typeof(ToolMenu), "CreateBasicTools")]
    public static class ToolMenu_CreateBasicTools {
        public static void Prefix(ToolMenu __instance) {
            __instance.basicTools.Add(ToolMenu.CreateToolCollection(
                PliersStrings.STRING_PLIERS_NAME,
                PliersAssets.PLIERS_ICON_NAME,
                PliersAssets.PLIERS_OPENTOOL.GetKAction(),
                PliersAssets.PLIERS_TOOLNAME,
                string.Format(PliersStrings.STRING_PLIERS_TOOLTIP, "{Hotkey}"),
                false
            ));
        }
    }
    //
    // [HarmonyPatch(typeof(ToolMenu), "BuildRowToggles")]
    // public static class ToolMenu_BuildRowToggles {
    //     public static void Prefix(IList<ToolMenu.ToolCollection> row)
    //     {
    //         Debug.Log("Next row");
    //         foreach (var tool in row)
    //         {
    //             Debug.Log(tool.text);    
    //         }
    //                     
    //     }
    // }

    [HarmonyPatch(typeof(Game), "DestroyInstances")]
    public static class Game_DestroyInstances {
        public static void Postfix() {
            PliersTool.DestroyInstance();
        }
    }

    [HarmonyPatch(typeof(GasConduitConfig), nameof(GasConduitConfig.DoPostConfigureComplete))]
    public static class GasConduitConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            Debug.Log("Gasconduid init pliers workable");
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(GasConduitRadiantConfig), nameof(GasConduitRadiantConfig.DoPostConfigureComplete))]
    public static class GasConduitRadiantConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(InsulatedGasConduitConfig), nameof(InsulatedGasConduitConfig.DoPostConfigureComplete))]
    public static class InsulatedGasConduitConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(LiquidConduitConfig), nameof(LiquidConduitConfig.DoPostConfigureComplete))]
    public static class LiquidConduitConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(LiquidConduitRadiantConfig), nameof(LiquidConduitRadiantConfig.DoPostConfigureComplete))]
    public static class LiquidConduitRadiantConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(InsulatedLiquidConduitConfig), nameof(InsulatedLiquidConduitConfig.DoPostConfigureComplete))]
    public static class InsulatedLiquidConduitConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(BaseWireConfig), nameof(BaseWireConfig.DoPostConfigureComplete))]
    public static class BaseWireConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(BaseLogicWireConfig), nameof(BaseLogicWireConfig.DoPostConfigureComplete))]
    public static class BaseLogicWireConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(SolidConduitConfig), nameof(SolidConduitConfig.DoPostConfigureComplete))]
    public static class SolidConduitConfig_DoPostConfigureComplete
    {
        public static void Prefix(GameObject go)
        {
            go.AddComponent<PliersWorkable>();
        }
    }
    
    [HarmonyPatch(typeof(Assets), "OnPrefabInit")]
    public static class Assets_OnPrefabInit
    {
        public static void Prefix(Assets __instance)
        {
            var sprite = Utilities.CreateSpriteDXT5(Assembly.GetExecutingAssembly().GetManifestResourceStream("Pliers.images.image_wirecutter_status.dds"), 256, 256);
            sprite.name = PliersAssets.PLIERS_STATUS_ICON_NAME;
            var tintedSprite = new TintedSprite
            {
                name = PliersAssets.PLIERS_STATUS_ICON_NAME,
                color = Color.black,
                sprite = sprite
            };
            __instance.TintedSpriteAssets.Add(tintedSprite);
        }
    }
}