using UnityEditor;
using UnityEngine;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>
    /// Compiled only when TextMeshPro is NOT installed (the scanner assembly is excluded in that case), so the
    /// package explains what is missing instead of failing with compile errors.
    /// </summary>
    [InitializeOnLoad]
    internal static class TmpRequirementNotice
    {
        const string SessionKey = "LocaSync.GlyphScanner.MissingTmpNoticeShown";

        const string Message =
            "LocaSync Glyph Scanner needs TextMeshPro, which is not installed in this project.\n\n" +
            "Unity 6: install 'Unity UI' (com.unity.ugui 2.0 or newer) from the Package Manager.\n" +
            "Unity 2022.3: install 'TextMeshPro' (com.unity.textmeshpro).\n\n" +
            "The Glyph Scanner window appears once TextMeshPro is installed.";

        static TmpRequirementNotice()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            Debug.LogWarning(Message);
        }

        [MenuItem("Window/LocaSync/Glyph Scanner")]
        static void Explain()
        {
            EditorUtility.DisplayDialog("LocaSync Glyph Scanner", Message, "OK");
        }
    }
}
