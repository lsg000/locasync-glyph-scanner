using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace LocaSync.Editor.GlyphScanner
{
    public sealed class GlyphScannerWindow : EditorWindow
    {
        const int HeaderButtonsPerRow = 4;
        const int MaxDetailRows = 200;

        sealed class ColumnReport
        {
            public string Column;
            public TMP_FontAsset Font;
            public string Error;
            public List<string> ChainNames = new List<string>();
            public CoverageResult Coverage;
            public string MissingCharacters = string.Empty;
            public string MissingHexRanges = string.Empty;
            public bool Expanded = true;
            public bool ShowDetails;
        }

        [SerializeField] GlyphScanProfile _profile;

        SerializedObject _serializedProfile;
        Vector2 _scroll;
        bool _busy;
        string _status;
        MessageType _statusType = MessageType.None;
        string[] _headers;
        List<ColumnReport> _reports;

        [MenuItem("Window/LocaSync/Glyph Scanner")]
        public static void Open()
        {
            var window = GetWindow<GlyphScannerWindow>();
            window.titleContent = new GUIContent("Glyph Scanner");
            window.minSize = new Vector2(420, 300);
            window.Show();
        }

        void OnGUI()
        {
            DrawProfileSelector();
            if (_profile == null)
            {
                EditorGUILayout.HelpBox(
                    "Create or assign a Glyph Scan Profile. A profile says where your localization CSV/TSV comes from " +
                    "and which TextMeshPro font asset renders each language column.", MessageType.Info);
                return;
            }

            if (_serializedProfile == null || _serializedProfile.targetObject != _profile)
            {
                _serializedProfile = new SerializedObject(_profile);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawProfileSettings();
            EditorGUILayout.Space();
            DrawActions();
            DrawStatus();
            DrawReports();
            EditorGUILayout.EndScrollView();
        }

        // ---------------------------------------------------------------- profile

        void DrawProfileSelector()
        {
            EditorGUILayout.BeginHorizontal();
            var selected = (GlyphScanProfile)EditorGUILayout.ObjectField("Profile", _profile, typeof(GlyphScanProfile), false);
            if (selected != _profile) SetProfile(selected);
            if (GUILayout.Button("New", GUILayout.Width(50))) CreateProfile();
            EditorGUILayout.EndHorizontal();
        }

        void CreateProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create Glyph Scan Profile", "GlyphScanProfile", "asset", "Choose where to save the profile.");
            if (!string.IsNullOrEmpty(path))
            {
                var profile = CreateInstance<GlyphScanProfile>();
                AssetDatabase.CreateAsset(profile, path);
                AssetDatabase.SaveAssets();
                SetProfile(profile);
            }
            GUIUtility.ExitGUI(); // the save dialog breaks the current IMGUI layout pass
        }

        void SetProfile(GlyphScanProfile profile)
        {
            _profile = profile;
            _serializedProfile = profile != null ? new SerializedObject(profile) : null;
            _headers = null;
            _reports = null;
            SetStatus(null, MessageType.None);
        }

        void DrawProfileSettings()
        {
            _serializedProfile.Update();

            SerializedProperty kind = _serializedProfile.FindProperty(nameof(GlyphScanProfile.sourceKind));
            EditorGUILayout.PropertyField(kind, new GUIContent("Source"));
            switch ((GlyphScanProfile.SourceKind)kind.enumValueIndex)
            {
                case GlyphScanProfile.SourceKind.TextAsset:
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty(nameof(GlyphScanProfile.textAsset)), new GUIContent("CSV Asset"));
                    break;
                case GlyphScanProfile.SourceKind.FilePath:
                    DrawFilePathField(_serializedProfile.FindProperty(nameof(GlyphScanProfile.filePath)));
                    break;
                case GlyphScanProfile.SourceKind.SheetUrl:
                    EditorGUILayout.PropertyField(_serializedProfile.FindProperty(nameof(GlyphScanProfile.sheetUrl)), new GUIContent("Sheet URL"));
                    break;
            }

            EditorGUILayout.PropertyField(_serializedProfile.FindProperty(nameof(GlyphScanProfile.keyColumn)));
            SerializedProperty bindings = _serializedProfile.FindProperty(nameof(GlyphScanProfile.bindings));
            EditorGUILayout.PropertyField(bindings, new GUIContent("Column → Font"), true);
            DrawHeaderShortcuts(bindings);

            EditorGUILayout.PropertyField(_serializedProfile.FindProperty(nameof(GlyphScanProfile.stripRichTextTags)));
            EditorGUILayout.PropertyField(_serializedProfile.FindProperty(nameof(GlyphScanProfile.stripPlaceholders)));
            EditorGUILayout.PropertyField(_serializedProfile.FindProperty(nameof(GlyphScanProfile.includeTmpSettingsFallbacks)),
                new GUIContent("Include TMP Settings Fallbacks"));

            _serializedProfile.ApplyModifiedProperties();
        }

        void DrawFilePathField(SerializedProperty filePath)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(filePath, new GUIContent("File"));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string picked = EditorUtility.OpenFilePanel("Select CSV / TSV", ProjectRoot(), "csv,tsv,txt");
                if (!string.IsNullOrEmpty(picked))
                {
                    filePath.stringValue = ToProjectRelative(picked);
                    _serializedProfile.ApplyModifiedProperties();
                }
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>After columns are loaded, offers one-click "add binding" buttons for columns not bound yet.</summary>
        void DrawHeaderShortcuts(SerializedProperty bindings)
        {
            if (_headers == null || _headers.Length == 0) return;

            var bound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < bindings.arraySize; i++)
            {
                bound.Add(bindings.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ColumnFontBinding.column)).stringValue.Trim());
            }

            string keyColumn = string.IsNullOrWhiteSpace(_profile.keyColumn) ? _headers[0] : _profile.keyColumn.Trim();
            List<string> candidates = _headers
                .Where(h => h.Length > 0 && !h.StartsWith("#") && !h.StartsWith("_"))
                .Where(h => !h.Equals(keyColumn, StringComparison.OrdinalIgnoreCase) && !bound.Contains(h))
                .ToList();
            if (candidates.Count == 0) return;

            EditorGUILayout.LabelField("Add a binding for a column:", EditorStyles.miniLabel);
            for (int start = 0; start < candidates.Count; start += HeaderButtonsPerRow)
            {
                EditorGUILayout.BeginHorizontal();
                foreach (string header in candidates.Skip(start).Take(HeaderButtonsPerRow))
                {
                    if (GUILayout.Button("+ " + header, EditorStyles.miniButton)) AddBinding(bindings, header);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        static void AddBinding(SerializedProperty bindings, string header)
        {
            int index = bindings.arraySize;
            bindings.InsertArrayElementAtIndex(index);
            SerializedProperty element = bindings.GetArrayElementAtIndex(index);
            element.FindPropertyRelative(nameof(ColumnFontBinding.column)).stringValue = header;
            element.FindPropertyRelative(nameof(ColumnFontBinding.font)).objectReferenceValue = null;
        }

        // ---------------------------------------------------------------- actions

        void DrawActions()
        {
            using (new EditorGUI.DisabledScope(_busy))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Load Columns", GUILayout.Height(24))) LoadSource(LoadHeaders);
                if (GUILayout.Button(_busy ? "Working…" : "Scan", GUILayout.Height(24))) LoadSource(ScanText);
                EditorGUILayout.EndHorizontal();
            }
        }

        void LoadSource(Action<string> onLoaded)
        {
            SetStatus(null, MessageType.None);
            switch (_profile.sourceKind)
            {
                case GlyphScanProfile.SourceKind.TextAsset:
                    if (_profile.textAsset == null)
                    {
                        SetStatus("Assign a CSV asset to scan.", MessageType.Warning);
                        return;
                    }
                    onLoaded(_profile.textAsset.text);
                    return;

                case GlyphScanProfile.SourceKind.FilePath:
                    string fullPath = ResolvePath(_profile.filePath);
                    if (fullPath == null || !File.Exists(fullPath))
                    {
                        SetStatus("File not found: " + _profile.filePath, MessageType.Warning);
                        return;
                    }
                    string fileText;
                    try
                    {
                        fileText = File.ReadAllText(fullPath, Encoding.UTF8);
                    }
                    catch (IOException exception)
                    {
                        SetStatus("Could not read the file (is it open in Excel?): " + exception.Message, MessageType.Error);
                        return;
                    }
                    onLoaded(fileText);
                    return;

                case GlyphScanProfile.SourceKind.SheetUrl:
                    if (!SheetUrlNormalizer.TryNormalize(_profile.sheetUrl, out string url, out string error))
                    {
                        SetStatus(error, MessageType.Error);
                        return;
                    }
                    _busy = true;
                    SetStatus("Downloading " + url, MessageType.Info);
                    SheetDownloader.Download(url,
                        text =>
                        {
                            if (this == null) return;
                            _busy = false;
                            SetStatus(null, MessageType.None);
                            onLoaded(text);
                            Repaint();
                        },
                        message =>
                        {
                            if (this == null) return;
                            _busy = false;
                            SetStatus(message, MessageType.Error);
                            Repaint();
                        });
                    return;
            }
        }

        TextTable ParseTable(string text)
        {
            TextTable table = TextTable.FromRows(DelimitedTextParser.Parse(text, DelimitedTextParser.DetectDelimiter(text)));
            if (table == null)
            {
                SetStatus("The source has no rows.", MessageType.Warning);
                return null;
            }
            _headers = table.Headers.ToArray();
            return table;
        }

        void LoadHeaders(string text)
        {
            TextTable table = ParseTable(text);
            if (table == null) return;
            SetStatus($"{_headers.Length} columns, {table.Rows.Count} rows: {string.Join(", ", _headers)}", MessageType.Info);
        }

        void ScanText(string text)
        {
            _reports = null;
            TextTable table = ParseTable(text);
            if (table == null) return;

            int keyIndex = string.IsNullOrWhiteSpace(_profile.keyColumn) ? 0 : table.IndexOf(_profile.keyColumn);
            if (keyIndex < 0)
            {
                SetStatus($"Key column '{_profile.keyColumn}' not found. Columns: {string.Join(", ", _headers)}", MessageType.Error);
                return;
            }
            if (_profile.bindings == null || _profile.bindings.Count == 0)
            {
                SetStatus("Add at least one Column → Font binding (use Load Columns for one-click buttons).", MessageType.Warning);
                return;
            }

            ScanOptions options = _profile.ToScanOptions();
            var reports = new List<ColumnReport>();
            foreach (ColumnFontBinding binding in _profile.bindings)
            {
                if (binding == null) continue;
                reports.Add(ScanColumn(table, keyIndex, binding, options));
            }
            _reports = reports;

            int missing = reports.Where(r => r.Coverage != null).Sum(r => r.Coverage.Missing.Count);
            int failed = reports.Count(r => r.Error != null);
            if (failed > 0)
                SetStatus($"{failed} binding(s) could not be scanned. See below.", MessageType.Warning);
            else if (missing == 0)
                SetStatus("Every character is covered by its font chain.", MessageType.Info);
            else
                SetStatus($"{missing} missing character(s) across {reports.Count(r => r.Coverage != null && r.Coverage.Missing.Count > 0)} column(s).", MessageType.Warning);
        }

        ColumnReport ScanColumn(TextTable table, int keyIndex, ColumnFontBinding binding, ScanOptions options)
        {
            var report = new ColumnReport { Column = binding.column, Font = binding.font };

            int columnIndex = table.IndexOf(binding.column);
            if (columnIndex < 0)
            {
                report.Error = $"Column '{binding.column}' not found.";
                return report;
            }
            if (binding.font == null)
            {
                report.Error = "No font asset assigned.";
                return report;
            }

            var entries = new List<ScanEntry>();
            foreach (string[] row in table.Rows)
            {
                string key = row[keyIndex].Trim();
                if (key.Length == 0 || key.StartsWith("#")) continue;
                entries.Add(new ScanEntry(key, row[columnIndex]));
            }

            List<TMP_FontAsset> chain = TmpFontChain.Resolve(binding.font, _profile.includeTmpSettingsFallbacks);
            report.ChainNames = chain.Select(f => f.name).ToList();
            report.Coverage = GlyphCoverageScanner.Scan(entries, TmpFontChain.ToLookups(chain), options);
            report.MissingCharacters = CharacterSetFormatter.ToCharacters(report.Coverage.Missing);
            report.MissingHexRanges = CharacterSetFormatter.ToHexRanges(report.Coverage.Missing);
            return report;
        }

        // ---------------------------------------------------------------- results

        void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, _statusType);
        }

        void DrawReports()
        {
            if (_reports == null) return;
            foreach (ColumnReport report in _reports) DrawReport(report);
        }

        void DrawReport(ColumnReport report)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string fontName = report.Font != null ? report.Font.name : "(no font)";
            string summary = report.Error != null
                ? $"{report.Column} → {fontName}   (error)"
                : $"{report.Column} → {fontName}   {report.Coverage.Missing.Count} missing / {report.Coverage.UniqueCodepointCount} unique";
            report.Expanded = EditorGUILayout.Foldout(report.Expanded, summary, true);

            if (report.Expanded)
            {
                if (report.Error != null)
                {
                    EditorGUILayout.HelpBox(report.Error, MessageType.Error);
                }
                else
                {
                    DrawCoverage(report);
                }
            }

            EditorGUILayout.EndVertical();
        }

        void DrawCoverage(ColumnReport report)
        {
            CoverageResult coverage = report.Coverage;

            var chainParts = new List<string>();
            for (int i = 0; i < report.ChainNames.Count; i++)
            {
                chainParts.Add($"{report.ChainNames[i]} ({coverage.CoveredCountByChainIndex[i]})");
            }
            EditorGUILayout.LabelField("Font chain", string.Join("  →  ", chainParts), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("Entries scanned", coverage.EntryCount.ToString());

            if (coverage.Missing.Count == 0)
            {
                EditorGUILayout.LabelField("All characters are covered.", EditorStyles.boldLabel);
                return;
            }

            if (coverage.ChainHasDynamicFont)
            {
                EditorGUILayout.HelpBox(
                    "The chain contains a Dynamic font asset. Characters listed here are not in its atlas yet; they will " +
                    "still render at runtime if the font's source file contains them.", MessageType.Info);
            }

            float width = Mathf.Max(100f, position.width - 40f);
            float height = EditorStyles.textArea.CalcHeight(new GUIContent(report.MissingCharacters), width);
            EditorGUILayout.SelectableLabel(report.MissingCharacters, EditorStyles.textArea, GUILayout.Height(Mathf.Min(height, 160f)));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Characters")) Copy(report.MissingCharacters, "Characters copied");
            if (GUILayout.Button("Copy Hex Ranges")) Copy(report.MissingHexRanges, "Hex ranges copied");
            EditorGUILayout.EndHorizontal();

            report.ShowDetails = EditorGUILayout.Foldout(report.ShowDetails, "Where each character is used", true);
            if (!report.ShowDetails) return;

            int shown = 0;
            foreach (uint codepoint in coverage.Missing)
            {
                if (shown++ >= MaxDetailRows)
                {
                    EditorGUILayout.LabelField($"… and {coverage.Missing.Count - MaxDetailRows} more", EditorStyles.miniLabel);
                    break;
                }
                string glyph = CharacterSetFormatter.ToCharacters(new[] { codepoint });
                string keys = string.Join(", ", coverage.MissingKeys[codepoint]);
                EditorGUILayout.LabelField($"{glyph}   {CharacterSetFormatter.ToLabel(codepoint)}", keys);
            }
        }

        void Copy(string text, string notification)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent(notification));
        }

        void SetStatus(string message, MessageType type)
        {
            _status = message;
            _statusType = type;
        }

        // ---------------------------------------------------------------- paths

        static string ProjectRoot() => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            string trimmed = path.Trim();
            return Path.IsPathRooted(trimmed) ? trimmed : Path.GetFullPath(Path.Combine(ProjectRoot(), trimmed));
        }

        static string ToProjectRelative(string absolutePath)
        {
            string root = ProjectRoot().Replace('\\', '/').TrimEnd('/') + "/";
            string normalized = absolutePath.Replace('\\', '/');
            return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? normalized.Substring(root.Length) : normalized;
        }
    }
}
