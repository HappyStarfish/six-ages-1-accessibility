using System;
using System.Text;

namespace SixAgesAccessibility
{
    /// <summary>
    /// Builds a screen-reader person dossier — name, age, skills, deity, location
    /// and health — in the active output language. Faithful port of RLTW's
    /// <c>Person.AttributedTextFor</c>, every English literal routed through
    /// <see cref="Loc"/> so the future German translation mod can override it.
    ///
    /// RLTW differs from SA2 on this surface: no <c>nickname</c>, no <c>isDead</c>
    /// (use <c>location == kUnderworld</c>), <c>ineligible</c> is bool not int,
    /// and PersonLocation omits kVanished/kDeparted/kOccupied.
    /// </summary>
    internal static class PersonBio
    {
        private const int BitName     = 0x001;
        private const int BitDeity    = 0x002;
        private const int BitSkills   = 0x004;
        private const int BitAge      = 0x008;
        private const int BitLocation = 0x010;
        private const int BitFamily   = 0x020;
        private const int BitHealth   = 0x040;
        private const int BitNameRing = 0x200;
        private const int BitMojara   = 0x800;

        public static string Localized(Person person, int aWhat)
        {
            try
            {
                return Build(person, aWhat);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("PersonBio.Localized", ex);
                try { return person.name; }
                catch { return string.Empty; }
            }
        }

        private static string Build(Person person, int aWhat)
        {
            var text = new StringBuilder();

            if ((aWhat & BitName) != 0 || (aWhat & BitNameRing) != 0)
                text.Append(person.name);

            if ((aWhat & BitAge) != 0)
            {
                text.Append(text.Length == 0 ? Loc.Get("Age: ") : Loc.Get(", age "));
                text.Append(person.age);
            }

            if ((aWhat & BitFamily) != 0 && person.familyKey != -1)
            {
                AppendNewline(text);
                text.Append(Loc.Get("Family: ")).Append(person.familyName);
            }

            if ((aWhat & BitSkills) != 0)
            {
                for (Skill skill = Skill.skill_Bargaining; skill <= Skill.skill_Magic; skill++)
                {
                    double rating = person.PersonSkill(skill);
                    if (rating <= 1.4) continue;
                    int adj = (int)rating - 1;
                    if (adj < 0) adj = 0;
                    if (adj > 5) adj = 5;
                    AppendNewline(text);
                    text.Append(Loc.Get(SkillNameKey(skill))).Append(": ")
                        .Append(Loc.Get(AdjectiveKey(adj)));
                }
            }

            if ((aWhat & BitDeity) != 0)
            {
                AppendNewline(text);
                text.Append(Loc.Get("Worships: "));
                text.Append(Game.NameOfDeity(person.deity));
            }

            if ((aWhat & BitMojara) != 0 && Game.ClanPersonVariable("majora") == person.index)
            {
                AppendNewline(text);
                text.Append(Loc.Get("Mojara’s Mask"));
            }

            if ((aWhat & BitLocation) != 0)
                AppendLocation(text, person);

            if ((aWhat & BitHealth) != 0)
                AppendHealth(text, person);

            if ((aWhat & BitNameRing) != 0 && person.ringPosition > 0)
                text.Append(" ®").Append(person.ringPosition);

            string result = text.ToString().Replace("\n", ". ");
            return string.IsNullOrEmpty(result) ? person.name : result;
        }

        private static void AppendLocation(StringBuilder text, Person person)
        {
            if (!person.isHome)
            {
                AppendNewline(text);
                text.Append(AwayStatus(person.location));
            }
            else
            {
                if (PluginImport.PersonList_SetFrom_Variable("absentAdvisor") > 0
                    && PluginImport.PersonList_Contains(person.index))
                {
                    AppendNewline(text);
                    text.Append(Loc.Get("Absent"));
                }
            }
        }

        private static void AppendHealth(StringBuilder text, Person person)
        {
            PersonLocation loc = person.location;
            if (person.isHealthy
                || loc == PersonLocation.kUnderworld
                || loc == PersonLocation.kEnslaved)
                return;

            AppendNewline(text);
            if (person.isSick)
                text.Append(SickWord());
            else if (person.isWounded)
                text.Append(Loc.Get("Wounded"));
        }

        private static string AwayStatus(PersonLocation loc)
        {
            switch (loc)
            {
                case PersonLocation.kUnderworld:      return Loc.Get("Dead");
                case PersonLocation.kMarriedOut:      return Loc.Get("(Married outside the clan)");
                case PersonLocation.kOtherworld:      return Loc.Get("(In the Otherworld)");
                case PersonLocation.kExploring:       return Loc.Get("(Exploring)");
                case PersonLocation.kCapturingHorses: return Loc.Get("(Searching for horses)");
                case PersonLocation.kForaging:        return Loc.Get("(Foraging)");
                case PersonLocation.kSearching:       return Loc.Get("(Searching for spirits)");
                default:                              return Loc.Get("(Away from the clan)");
            }
        }

        private static string SickWord()
        {
            return Loc.GermanActive ? "Krank" : "Sick";
        }

        private static string SkillNameKey(Skill skill)
        {
            switch (skill)
            {
                case Skill.skill_Bargaining: return "Bargaining";
                case Skill.skill_Combat:     return "Combat";
                case Skill.skill_Diplomacy:  return "Diplomacy";
                case Skill.skill_Food:       return "Food";
                case Skill.skill_Leadership: return "Leadership";
                case Skill.skill_Lore:       return "Lore";
                case Skill.skill_Magic:      return "Magic";
                default:                     return "";
            }
        }

        private static string AdjectiveKey(int adj)
        {
            switch (adj)
            {
                case 0:  return "Fair";
                case 1:  return "Good";
                case 2:  return "Very Good";
                case 3:  return "Excellent";
                case 4:  return "Renowned";
                default: return "Heroic";
            }
        }

        private static void AppendNewline(StringBuilder sb)
        {
            if (sb.Length > 0) sb.Append('\n');
        }
    }
}
