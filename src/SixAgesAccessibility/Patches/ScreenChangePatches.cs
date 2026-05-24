using System;
using HarmonyLib;
using UnityEngine;

namespace SixAgesAccessibility.Patches
{
    /// <summary>
    /// Harmony patches for screen change announcements. Adapted from the LGO
    /// (Six Ages 2) accessibility mod; the deferred-flush mechanism and phase
    /// classification are identical, but RLTW has no SacredTime year-advance
    /// suppression flag (that was a SA2-specific re-activation bug).
    /// </summary>
    [HarmonyPatch]
    public static class ScreenChangePatches
    {
        private static bool _subscribed;
        private static GamePhase _lastPhase = GamePhase.Unknown;
        private static string _lastScreenLabel = null;

        public enum GamePhase
        {
            Unknown,
            Menu,
            Management,
            Dialog,
            Story,
            Battle,
            SacredTime,
            GameOver
        }

        public static void SubscribeOnScreenChanged(ScreenManager sm)
        {
            if (_subscribed) return;
            try
            {
                if (sm == null || sm.onScreenChanged == null) return;
                sm.onScreenChanged.AddListener(OnScreenChangedListener);
                _subscribed = true;
                DebugLogger.Log("ScreenChange", "Subscribed to onScreenChanged");
            }
            catch (Exception ex)
            {
                DebugLogger.Error("ScreenChangePatches.Subscribe", ex);
            }
        }

        [HarmonyPatch(typeof(GameManager), "ShowManagementScreen", new Type[] { typeof(GameScreen), typeof(bool) })]
        [HarmonyPostfix]
        public static void ManagementScreen_Postfix(GameScreen aScreen)
        {
            try
            {
                DebugLogger.Log("ScreenChange", "ShowManagementScreen postfix: " + GameScreens.NameOf(aScreen));
                EnsureSubscribedFallback();
            }
            catch (Exception ex)
            {
                DebugLogger.Error("ScreenChangePatches", ex);
            }
        }

        private static void EnsureSubscribedFallback()
        {
            if (_subscribed) return;
            try
            {
                if (Singleton<ScreenManager>.isShuttingDown) return;
                var sm = Singleton<ScreenManager>.instance;
                if (sm != null) SubscribeOnScreenChanged(sm);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("ScreenChangePatches.SubscribeFallback", ex);
            }
        }

        private static ScreenController _pendingScreen;
        private static int _pendingFrame;

        /// <summary>
        /// Fired after activeScreen is updated. Management screens defer one frame
        /// (a TutorialController may cover them the same frame); other phases announce
        /// immediately.
        /// </summary>
        private static void OnScreenChangedListener()
        {
            try
            {
                ScreenController active = GetActiveScreen();
                if (active == null) return;

                // TutorialController has its own announce path (added in later phases).
                // ResultsOverlay likewise. Skip them so the underlying screen, if any,
                // stays the pending screen and gets dropped when covered.
                if (active is TutorialController || active is ResultsOverlay)
                {
                    DebugLogger.Log("ScreenChange", "Skipping header for "
                        + active.GetType().Name + " (own announce path covers it)");
                    return;
                }

                if (ClassifyPhase(active) == GamePhase.Management)
                {
                    _pendingScreen = active;
                    _pendingFrame = Time.frameCount;
                    return;
                }

                AnnounceScreen(active);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("ScreenChangePatches.OnChanged", ex);
            }
        }

        public static void FlushPendingScreenChange()
        {
            try
            {
                if (_pendingScreen == null) return;
                if (Time.frameCount <= _pendingFrame) return;

                ScreenController pending = _pendingScreen;
                _pendingScreen = null;

                ScreenController active = GetActiveScreen();
                if (active == null || active != pending)
                {
                    DebugLogger.Log("ScreenChange", "Pending screen change dropped — covered");
                    return;
                }

                AnnounceScreen(active);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("ScreenChangePatches.FlushPending", ex);
            }
        }

        private static void AnnounceScreen(ScreenController active)
        {
            string header = GetPhaseHeaderIfNew(active);
            if (header != null)
            {
                DebugLogger.Log("ScreenChange", "Announcing header: " + header);
                ScreenReader.Say(header, interrupt: false);
            }

            if (ManagementScreenReader.TryReadSummary(active, interrupt: false))
                DebugLogger.Log("ScreenChange", "Management screen summary queued");
        }

        public static string GetPhaseHeaderIfNew(ScreenController active)
        {
            if (active == null) return null;
            GamePhase phase = ClassifyPhase(active);
            string screenLabel = ScreenLabelFor(active);

            if (phase == _lastPhase && screenLabel == _lastScreenLabel)
            {
                DebugLogger.Log("ScreenChange", "Same phase+screen, skipping header");
                return null;
            }

            bool samePhase = phase == _lastPhase && _lastPhase != GamePhase.Unknown;

            _lastPhase = phase;
            _lastScreenLabel = screenLabel;

            if (samePhase)
                return Loc.Get(screenLabel) + Loc.Get(" screen.");
            return Loc.Get(PhaseLabel(phase)) + Loc.Get(" phase. ")
                + Loc.Get(screenLabel) + Loc.Get(" screen.");
        }

        private static GamePhase ClassifyPhase(ScreenController active)
        {
            if (active == null) return GamePhase.Unknown;
            // BattleController is an InteractiveController, so check battle first.
            if (active is BattleController || active is BattleResultsController)
                return GamePhase.Battle;
            if (active is SacredTime)
                return GamePhase.SacredTime;
            if (active is VictoryController || active is GameOverController)
                return GamePhase.GameOver;
            if (active is InteractiveController)
                return GamePhase.Story;
            if (active is ManagementDialogController)
                return GamePhase.Dialog;
            if (active is ChooseLeaderDialog)
                return GamePhase.Dialog;
            // InfoDialogController and ManualDialogController inherit directly from
            // BaseController, not ManagementDialogController — so the check above
            // misses them and they otherwise fall through to "Unknown phase".
            if (active is InfoDialogController || active is ManualDialogController)
                return GamePhase.Dialog;
            if (active is ManagementController)
                return GamePhase.Management;
            if (active is MainMenu || active is ChooseGameController || active is ControlsOverlay)
                return GamePhase.Menu;
            return GamePhase.Unknown;
        }

        private static string PhaseLabel(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Menu: return "Menu";
                case GamePhase.Management: return "Management";
                case GamePhase.Dialog: return "Dialog";
                case GamePhase.Story: return "Story";
                case GamePhase.Battle: return "Battle";
                case GamePhase.SacredTime: return "Sacred time";
                case GamePhase.GameOver: return "Game over";
                default: return "Unknown";
            }
        }

        private static string ScreenLabelFor(ScreenController active)
        {
            try
            {
                if (active is BaseController bc && bc.screenIndex != 0)
                    return GameScreens.NameOf(bc.screenIndex);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("ScreenChangePatches.ScreenLabelFor", ex);
            }

            if (active is MainMenu) return "Main menu";
            if (active is ChooseGameController) return "Choose game";
            if (active is ControlsOverlay) return "Settings";
            if (active is BattleController) return "Battle";
            if (active is BattleResultsController) return "Battle results";
            if (active is VictoryController) return "Victory";
            if (active is GameOverController) return "Game over";
            if (active is SacredTime) return "Sacred time";
            if (active is ChooseLeaderDialog) return "Choose leader";

            string n = active.GetType().Name;
            if (n.EndsWith("Controller", StringComparison.Ordinal))
                n = n.Substring(0, n.Length - "Controller".Length);
            if (n.EndsWith("Dialog", StringComparison.Ordinal))
                n = n.Substring(0, n.Length - "Dialog".Length);
            return n;
        }

        private static ScreenController GetActiveScreen()
        {
            try
            {
                if (Singleton<ScreenManager>.isShuttingDown) return null;
                var sm = Singleton<ScreenManager>.instance;
                if (sm == null) return null;
                return sm.activeScreen;
            }
            catch (Exception ex)
            {
                DebugLogger.Error("ScreenChangePatches.GetActiveScreen", ex);
                return null;
            }
        }
    }
}
