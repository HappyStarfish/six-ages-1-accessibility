using System;
using UnityEngine;

namespace SixAgesAccessibility
{
    /// <summary>
    /// Reads the on-screen stat panel for screen-reader users.
    ///
    /// F2 cycles through four short views: season explanation → time →
    /// resources → reputation. The cycle position only resets when the user
    /// presses a non-F2 key (handled by KeyboardNavigationHandler via
    /// <see cref="ResetCycle"/>).
    /// </summary>
    public static class StatPanelReader
    {
        private static int _cycleIndex = -1;
        private const int CycleCount = 4;

        public static bool HasCycleState { get { return _cycleIndex >= 0; } }

        public static void ResetCycle() { _cycleIndex = -1; }

        public static void ReadSeasonAndYear()
        {
            try
            {
                string season = PluginImport.Game_SeasonName();
                string year = PluginImport.Game_YearName();

                if (string.IsNullOrEmpty(season) && string.IsNullOrEmpty(year))
                {
                    ScreenReader.Say("Time not available.");
                    return;
                }

                ScreenReader.Say(season + ", " + year);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("StatPanelReader.ReadSeasonAndYear", ex);
                ScreenReader.Say("Could not read time.");
            }
        }

        public static void ReadSeasonExplanation()
        {
            try
            {
                string shortName = Game.shortSeasonName;
                if (string.IsNullOrEmpty(shortName))
                {
                    ReadSeasonAndYear();
                    return;
                }

                string explanation = Localized.StringFromTable(shortName + "Explanation", "Text");
                int turnInYear = Game.turnInYear;
                if (turnInYear != 11)
                {
                    string prefix = (turnInYear % 2 == 1) ? "Early " : "Late ";
                    explanation = prefix + explanation;
                }

                if (string.IsNullOrEmpty(explanation))
                {
                    ReadSeasonAndYear();
                    return;
                }

                string season = PluginImport.Game_SeasonName();
                string year = PluginImport.Game_YearName();
                string header = (!string.IsNullOrEmpty(season) && !string.IsNullOrEmpty(year))
                    ? (season + ", " + year + ". ")
                    : "";
                ScreenReader.Say(header + StringHelpers.StripTags(explanation));
            }
            catch (Exception ex)
            {
                DebugLogger.Error("StatPanelReader.ReadSeasonExplanation", ex);
                ScreenReader.Say("Could not read season explanation.");
            }
        }

        public static void Cycle()
        {
            _cycleIndex = (_cycleIndex + 1) % CycleCount;

            switch (_cycleIndex)
            {
                case 0: ReadSeasonExplanation(); break;
                case 1: ReadSeasonAndYear(); break;
                case 2: ReadResources(); break;
                case 3: ReadReputation(); break;
            }
        }

        private static void ReadResources()
        {
            try
            {
                int herds = PluginImport.PC_Herds();
                int goods = PluginImport.PC_Goods();
                int warriors = PluginImport.PC_Warriors();
                int magic = PluginImport.PC_Magic();
                ScreenReader.Say("Herds " + herds + ", Goods " + goods + ", Warriors " + warriors + ", Magic " + magic);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("StatPanelReader.ReadResources", ex);
                ScreenReader.Say("Could not read resources.");
            }
        }

        private static void ReadReputation()
        {
            try
            {
                int like = PluginImport.PC_LikeCount();
                int hate = PluginImport.PC_HateCount();
                int fear = PluginImport.PC_FearCount();
                int mock = PluginImport.PC_MockCount();
                ScreenReader.Say("Like " + like + ", Hate " + hate + ", Fear " + fear + ", Mock " + mock);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("StatPanelReader.ReadReputation", ex);
                ScreenReader.Say("Could not read reputation.");
            }
        }
    }
}
