using System.Collections.Generic;

public enum TrialNarrationCue { Entry, Hesitation, FirstDeath, RepeatedDeath, Progress, ColorDiscovery }

public sealed class TrialNarrationProfile
{
    public readonly string SceneName;
    public readonly bool ProgressOnColorSwitch;
    private readonly string[] lines;

    private TrialNarrationProfile(string scene, bool progressOnColorSwitch, params string[] captions)
    {
        SceneName = scene;
        ProgressOnColorSwitch = progressOnColorSwitch;
        lines = captions;
    }

    public string TextFor(TrialNarrationCue cue) => (int)cue >= 0 && (int)cue < lines.Length ? lines[(int)cue] : null;
    public bool HasLine(TrialNarrationCue cue) => !string.IsNullOrWhiteSpace(TextFor(cue));
    public bool HasDeathResponses => HasLine(TrialNarrationCue.FirstDeath) || HasLine(TrialNarrationCue.RepeatedDeath);

    private static readonly Dictionary<string, TrialNarrationProfile> Profiles = new Dictionary<string, TrialNarrationProfile>
    {
        ["BabyTutorial"] = new TrialNarrationProfile("BabyTutorial", true,
            "The subject arrives. Color may prove useful.",
            null,
            null,
            null,
            "A color change. The subject makes progress.",
            "The subject tests patience. Perhaps a different color?"),
        ["Level01"] = new TrialNarrationProfile("Level01", false,
            "The subject approaches. The spikes seem optimistic.",
            "The subject waits. The spikes remain.",
            "A close inspection. The spikes passed.",
            "The subject jumps late. The spikes approve.",
            "The subject clears the spikes. Progress, apparently."),
        ["Level02"] = new TrialNarrationProfile("Level02", true,
            "The subject trusts the floor. Bold.",
            "Same gap, same color. Curious.",
            "Confidence proved insufficiently solid.",
            "Same jump, same color. The subject expects improvement.",
            "The subject changes color. The floor cooperates."),
        ["Level03"] = new TrialNarrationProfile("Level03", false,
            "The subject arrives. The platforms keep moving.",
            "The subject waits. A full cycle might help.",
            "Excellent confidence. Unfortunate timing.",
            "The subject jumps first. The blue platform goes unobserved.",
            "The subject finds the rhythm. Eventually."),
        ["Level04"] = new TrialNarrationProfile("Level04", false,
            "The subject returns. Several lessons allegedly learned.",
            "The subject considers jumping. Color and timing await.",
            "The platform moved. The subject's plan can too.",
            "Color, then timing. The subject prefers improvisation.",
            "The subject combines the lessons. How inconvenient.")
    };

    public static bool TryGet(string sceneName, out TrialNarrationProfile profile)
    {
        if (sceneName == null)
        {
            profile = null;
            return false;
        }
        return Profiles.TryGetValue(sceneName, out profile);
    }
}
