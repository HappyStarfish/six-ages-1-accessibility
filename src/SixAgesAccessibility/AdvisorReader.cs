using System;
using System.Collections.Generic;
using System.Text;

namespace SixAgesAccessibility
{
    /// <summary>
    /// Reads advisor advice (F3) and advisor info (Shift+F3) for ring members in scenes
    /// and management screens, plus the expedition leader during expedition mode.
    /// </summary>
    public static class AdvisorReader
    {
        private static int _cycleIndex = -1;

        public static bool IsCycling { get { return _cycleIndex > 0; } }

        public static bool HasCycleState { get { return _cycleIndex >= 0; } }

        public static void ResetCycle() { _cycleIndex = -1; }

        public static void ReadAdviceOrCycle(ScreenController screen)
        {
            if (_cycleIndex < 0)
            {
                ReadAllAdvice(screen);
                _cycleIndex = 0;
            }
            else
            {
                ReadSingleAdvice(screen, _cycleIndex);
            }
        }

        public static void ReadInfo(ScreenController screen)
        {
            if (_cycleIndex > 0)
                ReadCurrentInfo(screen);
            else
                ReadAllInfo(screen);
        }

        private static void ReadAllAdvice(ScreenController screen)
        {
            try
            {
                bool isScene = screen is InteractiveController;
                bool isManagement = screen is ManagementController;

                if (!isScene && !isManagement)
                {
                    ScreenReader.Say(Loc.Get("Advisor advice is available during scenes and management."));
                    return;
                }

                var sb = new StringBuilder();
                int advisorCount = 0;

                bool expeditionAdvice = false;
                try { expeditionAdvice = Game.BooleanVariable("expeditionAdvice"); }
                catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionAdvice", ex); }

                if (expeditionAdvice && isScene)
                {
                    string advice = PluginImport.Script_AdviceForAdvisor(1);
                    if (!string.IsNullOrEmpty(advice))
                    {
                        int expLeader = 0;
                        try { expLeader = Game.IntegerVariable("expeditionLeader"); }
                        catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionLeader", ex); }
                        string name = expLeader > 0 ? PluginImport.PC_PersonName(expLeader) : Loc.Get("Expedition leader");
                        bool highlighted;
                        string spoken = ExtractAdviceHighlight(advice, out highlighted);
                        sb.Append(name).Append(highlighted ? Loc.Get(", highlighted: ") : ": ").Append(spoken);
                        AppendSuggestedResponses(sb, 1);
                        advisorCount = 1;
                    }
                }
                else
                {
                    HashSet<int> absentSet, sulkingSet;
                    CollectAdvisorBlockers(out absentSet, out sulkingSet);

                    for (int pos = 1; pos <= 7; pos++)
                    {
                        int personIndex = PluginImport.PC_RingPerson(pos);
                        if (personIndex <= 0) continue;

                        string name = PluginImport.PC_PersonName(personIndex);
                        if (string.IsNullOrEmpty(name)) continue;

                        string advice = isScene
                            ? PluginImport.Script_AdviceForAdvisor(pos)
                            : PluginImport.PC_AdviceForAdvisor(pos);

                        if (string.IsNullOrEmpty(advice))
                        {
                            string blocker = null;
                            if (absentSet.Contains(personIndex)) blocker = Loc.Get("absent this season");
                            else if (sulkingSet.Contains(personIndex)) blocker = Loc.Get("sulking, not advising");
                            if (blocker == null) continue;

                            if (advisorCount > 0) sb.Append(" ... ");
                            sb.Append(name).Append(": ").Append(blocker).Append('.');
                            advisorCount++;
                            continue;
                        }

                        if (advisorCount > 0) sb.Append(" ... ");
                        bool highlighted;
                        string spoken = ExtractAdviceHighlight(advice, out highlighted);
                        sb.Append(name).Append(highlighted ? Loc.Get(", highlighted: ") : ": ").Append(spoken);

                        if (isScene)
                            AppendSuggestedResponses(sb, pos);

                        advisorCount++;
                    }
                }

                ScreenReader.Say(advisorCount == 0 ? Loc.Get("No advisor advice available.") : sb.ToString());
                DebugLogger.Log("AdvisorReader", advisorCount + " advisors read");
            }
            catch (Exception ex)
            {
                DebugLogger.Error("AdvisorReader.ReadAllAdvice", ex);
                ScreenReader.Say(Loc.Get("Could not read advisor advice."));
            }
        }

        private static void ReadSingleAdvice(ScreenController screen, int startIndex)
        {
            try
            {
                bool isScene = screen is InteractiveController;
                bool isManagement = screen is ManagementController;

                if (!isScene && !isManagement)
                {
                    _cycleIndex = -1;
                    ScreenReader.Say(Loc.Get("Advisor advice is available during scenes and management."));
                    return;
                }

                bool expeditionAdvice = false;
                try { expeditionAdvice = Game.BooleanVariable("expeditionAdvice"); }
                catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionAdvice", ex); }

                if (expeditionAdvice && isScene)
                {
                    string advice = PluginImport.Script_AdviceForAdvisor(1);
                    if (!string.IsNullOrEmpty(advice))
                    {
                        int expLeader = 0;
                        try { expLeader = Game.IntegerVariable("expeditionLeader"); }
                        catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionLeader", ex); }
                        string name = expLeader > 0 ? PluginImport.PC_PersonName(expLeader) : Loc.Get("Expedition leader");
                        bool highlighted;
                        string spoken = ExtractAdviceHighlight(advice, out highlighted);
                        var sb = new StringBuilder();
                        sb.Append(name).Append(highlighted ? Loc.Get(", highlighted: ") : ": ").Append(spoken);
                        AppendSuggestedResponses(sb, 1);
                        ScreenReader.Say(sb.ToString());
                    }
                    else
                    {
                        ScreenReader.Say(Loc.Get("No advice available."));
                    }
                    return;
                }

                var positions = new List<int>();
                var names = new List<string>();
                var adviceTexts = new List<string>();

                HashSet<int> absentSet, sulkingSet;
                CollectAdvisorBlockers(out absentSet, out sulkingSet);

                for (int pos = 1; pos <= 7; pos++)
                {
                    int personIndex = PluginImport.PC_RingPerson(pos);
                    if (personIndex <= 0) continue;

                    string pName = PluginImport.PC_PersonName(personIndex);
                    if (string.IsNullOrEmpty(pName)) continue;

                    string advice = isScene
                        ? PluginImport.Script_AdviceForAdvisor(pos)
                        : PluginImport.PC_AdviceForAdvisor(pos);

                    if (string.IsNullOrEmpty(advice))
                    {
                        string blocker = null;
                        if (absentSet.Contains(personIndex)) blocker = Loc.Get("absent this season");
                        else if (sulkingSet.Contains(personIndex)) blocker = Loc.Get("sulking, not advising");
                        if (blocker == null) continue;

                        positions.Add(pos);
                        names.Add(pName);
                        adviceTexts.Add(blocker);
                        continue;
                    }

                    positions.Add(pos);
                    names.Add(pName);
                    adviceTexts.Add(advice);
                }

                if (positions.Count == 0)
                {
                    _cycleIndex = -1;
                    ScreenReader.Say(Loc.Get("No advisor advice available."));
                    return;
                }

                if (startIndex >= positions.Count) startIndex = 0;

                bool singleHighlighted;
                string singleSpoken = ExtractAdviceHighlight(adviceTexts[startIndex], out singleHighlighted);

                var output = new StringBuilder();
                output.Append(startIndex + 1).Append(Loc.Get(" of ")).Append(positions.Count).Append(": ");
                output.Append(names[startIndex])
                    .Append(singleHighlighted ? Loc.Get(", highlighted: ") : ": ")
                    .Append(singleSpoken);

                if (isScene)
                    AppendSuggestedResponses(output, positions[startIndex]);

                ScreenReader.Say(output.ToString());

                _cycleIndex = startIndex + 1;
                if (_cycleIndex >= positions.Count) _cycleIndex = 0;
            }
            catch (Exception ex)
            {
                DebugLogger.Error("AdvisorReader.ReadSingleAdvice", ex);
                ScreenReader.Say(Loc.Get("Could not read advisor advice."));
                _cycleIndex = -1;
            }
        }

        private static void CollectAdvisorBlockers(out HashSet<int> absent, out HashSet<int> sulking)
        {
            absent = new HashSet<int>();
            sulking = new HashSet<int>();
            try
            {
                PersonDataList absentList = PersonDataList.FromVariable("absentAdvisor");
                for (int i = 0; i < absentList.count; i++)
                    absent.Add(absentList[i].personIndex);

                PersonDataList sulkingList = PersonDataList.FromVariable("wontAdvise");
                for (int i = 0; i < sulkingList.count; i++)
                    sulking.Add(sulkingList[i].personIndex);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("AdvisorReader.CollectAdvisorBlockers", ex);
            }
        }

        private static string ExtractAdviceHighlight(string raw, out bool highlighted)
        {
            highlighted = false;
            if (string.IsNullOrEmpty(raw)) return raw;
            if (raw.IndexOf('*') < 0) return raw;

            int asteriskCount = 0;
            for (int i = 0; i < raw.Length; i++) if (raw[i] == '*') asteriskCount++;
            if (asteriskCount < 2 || (asteriskCount % 2) != 0) return raw;

            highlighted = true;
            return raw.Replace("*", string.Empty);
        }

        private static void AppendSuggestedResponses(StringBuilder sb, int position)
        {
            try
            {
                int bitmask = PluginImport.Script_SuggestionsForAdvisor(position);
                if (bitmask == 0) return;

                int responseCount = PluginImport.Script_ResponseCount();
                if (responseCount <= 0) return;

                sb.Append(Loc.Get(" Suggests: "));
                bool first = true;
                for (int i = 0; i < responseCount; i++)
                {
                    if ((bitmask & (1 << i)) != 0)
                    {
                        string respText = PluginImport.Script_ResponseText(i + 1);
                        if (!string.IsNullOrEmpty(respText))
                        {
                            if (!first) sb.Append(", ");
                            sb.Append(i + 1).Append(") ").Append(respText);
                            first = false;
                        }
                    }
                }
                sb.Append(".");
            }
            catch (Exception ex)
            {
                DebugLogger.Error("AdvisorReader.Suggestions", ex);
            }
        }

        private static void ReadAllInfo(ScreenController screen)
        {
            try
            {
                bool isScene = screen is InteractiveController;
                bool isManagement = screen is ManagementController;

                if (!isScene && !isManagement)
                {
                    ScreenReader.Say(Loc.Get("Advisor info is available during scenes and management."));
                    return;
                }

                bool expeditionAdvice = false;
                try { expeditionAdvice = Game.BooleanVariable("expeditionAdvice"); }
                catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionAdvice", ex); }

                var sb = new StringBuilder();
                int count = 0;

                if (expeditionAdvice)
                {
                    int expLeader = 0;
                    try { expLeader = Game.IntegerVariable("expeditionLeader"); }
                    catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionLeader", ex); }
                    if (expLeader > 0)
                    {
                        Person person = PlayerClan.PersonWithIndex(expLeader);
                        if (person != null)
                        {
                            string info = PersonBio.Localized(person, 111);
                            if (!string.IsNullOrEmpty(info))
                            {
                                sb.Append(info);
                                count = 1;
                            }
                        }
                    }
                }
                else
                {
                    for (int pos = 1; pos <= 7; pos++)
                    {
                        int personIndex = PluginImport.PC_RingPerson(pos);
                        if (personIndex <= 0) continue;

                        Person person = PlayerClan.PersonWithIndex(personIndex);
                        if (person == null) continue;

                        string info = PersonBio.Localized(person, 127);
                        if (string.IsNullOrEmpty(info)) continue;

                        if (count > 0) sb.Append(" ... ");
                        sb.Append(info);
                        count++;
                    }
                }

                ScreenReader.Say(count == 0 ? Loc.Get("No advisor info available.") : sb.ToString());
                DebugLogger.Log("AdvisorReader", count + " advisor infos read");
            }
            catch (Exception ex)
            {
                DebugLogger.Error("AdvisorReader.ReadAllInfo", ex);
                ScreenReader.Say(Loc.Get("Could not read advisor info."));
            }
        }

        private static void ReadCurrentInfo(ScreenController screen)
        {
            try
            {
                bool isScene = screen is InteractiveController;
                bool isManagement = screen is ManagementController;

                if (!isScene && !isManagement)
                {
                    ScreenReader.Say(Loc.Get("Advisor info is available during scenes and management."));
                    return;
                }

                bool expeditionAdvice = false;
                try { expeditionAdvice = Game.BooleanVariable("expeditionAdvice"); }
                catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionAdvice", ex); }

                if (expeditionAdvice && isScene)
                {
                    int expLeader = 0;
                    try { expLeader = Game.IntegerVariable("expeditionLeader"); }
                    catch (Exception ex) { DebugLogger.Error("AdvisorReader.expeditionLeader", ex); }
                    if (expLeader > 0)
                    {
                        Person person = PlayerClan.PersonWithIndex(expLeader);
                        if (person != null)
                        {
                            ScreenReader.Say(PersonBio.Localized(person, 111));
                            DebugLogger.Log("AdvisorReader", "Read info for " + person.name);
                            return;
                        }
                    }
                    ScreenReader.Say(Loc.Get("No advisor info available."));
                    return;
                }

                var personIndices = new List<int>();

                for (int pos = 1; pos <= 7; pos++)
                {
                    int personIndex = PluginImport.PC_RingPerson(pos);
                    if (personIndex <= 0) continue;

                    string pName = PluginImport.PC_PersonName(personIndex);
                    if (string.IsNullOrEmpty(pName)) continue;

                    string advice = isScene
                        ? PluginImport.Script_AdviceForAdvisor(pos)
                        : PluginImport.PC_AdviceForAdvisor(pos);

                    if (string.IsNullOrEmpty(advice)) continue;

                    personIndices.Add(personIndex);
                }

                int lastIndex = _cycleIndex - 1;
                if (lastIndex < 0 || lastIndex >= personIndices.Count)
                {
                    ScreenReader.Say(Loc.Get("No advisor info available."));
                    return;
                }

                Person p = PlayerClan.PersonWithIndex(personIndices[lastIndex]);
                if (p != null)
                {
                    ScreenReader.Say(PersonBio.Localized(p, 127));
                    DebugLogger.Log("AdvisorReader", "Read info for " + p.name);
                }
                else
                {
                    ScreenReader.Say(Loc.Get("No advisor info available."));
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Error("AdvisorReader.ReadCurrentInfo", ex);
                ScreenReader.Say(Loc.Get("Could not read advisor info."));
            }
        }
    }
}
