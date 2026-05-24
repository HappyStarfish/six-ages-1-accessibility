using System;
using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SixAgesAccessibility.Patches;
using UnityEngine;

namespace SixAgesAccessibility
{
    /// <summary>
    /// BepInEx entry point for the Six Ages 1 (Ride Like the Wind) accessibility mod.
    /// Initializes Tolk, applies Harmony patches, attaches the KeyboardNavigationHandler
    /// to the plugin GameObject, and subscribes to ScreenManager.onScreenChanged for
    /// screen change announcements.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(TranslationModGUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.accessibility.sixages1";

        /// <summary>
        /// BepInEx GUID of the future RLTW German translation mod. Soft dependency so that
        /// once it exists, BepInEx loads it before this plugin and the language detection
        /// picks it up. Until then the check always returns false and output stays English.
        /// </summary>
        public const string TranslationModGUID = "com.translation.sixages1.de";

        public const string PluginName = "Six Ages 1 Accessibility (RLTW)";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo(PluginName + " v" + PluginVersion + " loading...");

            DebugLogger.Init(Log);

            bool germanModActive = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(TranslationModGUID);
            if (germanModActive)
            {
                LocSetup.RegisterGerman();
                Log.LogInfo("RLTW German translation mod detected — screen reader output set to German.");
            }
            else
            {
                Log.LogInfo("RLTW German translation mod not present — screen reader output stays English.");
            }
            Loc.SetLanguageGerman(germanModActive);

            if (!ScreenReader.Init())
            {
                Log.LogWarning("Tolk not found — screen reader output disabled.");
            }

            try
            {
                _harmony = new Harmony(PluginGUID);
                _harmony.PatchAll();
                Log.LogInfo("Harmony patches applied.");
                VerifyPatches();
            }
            catch (Exception ex)
            {
                Log.LogError("Harmony patching failed: " + ex.ToString());
            }

            gameObject.AddComponent<KeyboardNavigationHandler>();
            Log.LogInfo("Keyboard navigation handler added.");

            // Subscribe as soon as the ScreenManager singleton exists. The fallback
            // postfix in ScreenChangePatches.ManagementScreen_Postfix re-tries on
            // each show, so missing the first instant load is not fatal.
            StartCoroutine(SubscribeWhenScreenManagerReady());

            ScreenReader.Say("Six Ages 1 Accessibility loaded.");
            Log.LogInfo(PluginName + " loaded successfully.");
        }

        private IEnumerator SubscribeWhenScreenManagerReady()
        {
            const float timeoutSeconds = 30f;
            float elapsed = 0f;
            while (elapsed < timeoutSeconds)
            {
                ScreenManager sm = null;
                try
                {
                    if (!Singleton<ScreenManager>.isShuttingDown)
                        sm = UnityEngine.Object.FindObjectOfType<ScreenManager>();
                }
                catch (Exception ex)
                {
                    DebugLogger.Error("Plugin.SubscribeWhenScreenManagerReady", ex);
                }

                if (sm != null && sm.onScreenChanged != null)
                {
                    ScreenChangePatches.SubscribeOnScreenChanged(sm);
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }
            DebugLogger.Warn("Plugin", "ScreenManager singleton never became available; screen-change announcements may be missed on first load.");
        }

        private void VerifyPatches()
        {
            try
            {
                int count = 0;
                foreach (var m in _harmony.GetPatchedMethods())
                {
                    string typeName = "?";
                    try
                    {
                        object dt = m.DeclaringType;
                        if (dt is Type t)
                            typeName = t.Name;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Error("Plugin.VerifyPatches.DeclaringType", ex);
                    }
                    DebugLogger.LogPatch(typeName + "." + m.Name, true);
                    count++;
                }
                Log.LogInfo("Total patched methods: " + count);
            }
            catch (Exception ex)
            {
                Log.LogWarning("VerifyPatches failed (non-critical): " + ex.Message);
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
            ScreenReader.Shutdown();
        }
    }
}
