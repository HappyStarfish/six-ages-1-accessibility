using System;
using System.Text;

namespace SixAgesAccessibility
{
    /// <summary>
    /// Owns reading of the floating <see cref="TutorialView"/> hint card. The game
    /// splits long hints across pages with prev/next buttons; this handler flattens
    /// that — every hint is read as one string regardless of length, for both the
    /// auto announcement and the manual H key.
    /// </summary>
    public class TutorialHintHandler
    {
        private static TutorialHintHandler _instance;
        public static TutorialHintHandler Instance
        {
            get
            {
                if (_instance == null) _instance = new TutorialHintHandler();
                return _instance;
            }
        }

        public bool HasActiveHint()
        {
            try
            {
                TutorialView tv = TutorialView.instance;
                return tv != null && tv.gameObject.activeSelf;
            }
            catch (Exception ex)
            {
                DebugLogger.Error("TutorialHintHandler.HasActiveHint", ex);
                return false;
            }
        }

        public string BuildFullHintText()
        {
            try
            {
                TutorialView tv = TutorialView.instance;
                if (tv == null || !tv.gameObject.activeSelf)
                    return "";

                int total = SafeTextCount();
                if (total <= 0)
                    return "";

                int savedIndex = TutorialCard.textIndex;
                StringBuilder sb = new StringBuilder();
                try
                {
                    for (int i = 0; i < total; i++)
                    {
                        TutorialCard.textIndex = i;
                        string page = TutorialCard.currentText;
                        if (!string.IsNullOrEmpty(page))
                        {
                            if (sb.Length > 0) sb.Append(' ');
                            sb.Append(page.Trim());
                        }
                    }
                }
                finally
                {
                    TutorialCard.textIndex = savedIndex;
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                DebugLogger.Error("TutorialHintHandler.BuildFullHintText", ex);
                return "";
            }
        }

        public void HandleHKey()
        {
            try
            {
                string full = BuildFullHintText();
                if (string.IsNullOrEmpty(full))
                {
                    ScreenReader.Say(Loc.Get("No hint on this screen."));
                    return;
                }
                ScreenReader.Say(Loc.Get("Hint: ") + full);
            }
            catch (Exception ex)
            {
                DebugLogger.Error("TutorialHintHandler.HandleHKey", ex);
            }
        }

        private int SafeTextCount()
        {
            try { return TutorialCard.textCount; }
            catch { return 0; }
        }
    }
}
