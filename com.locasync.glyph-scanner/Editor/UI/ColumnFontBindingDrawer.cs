using UnityEditor;
using UnityEngine;

namespace LocaSync.Editor.GlyphScanner
{
    /// <summary>Draws a binding on one line, "column → font", instead of a foldout per element.</summary>
    [CustomPropertyDrawer(typeof(ColumnFontBinding))]
    internal sealed class ColumnFontBindingDrawer : PropertyDrawer
    {
        const float ArrowWidth = 18f;
        const float Gap = 4f;

        static readonly GUIContent Arrow = new GUIContent("→");

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty column = property.FindPropertyRelative(nameof(ColumnFontBinding.column));
            SerializedProperty font = property.FindPropertyRelative(nameof(ColumnFontBinding.font));

            EditorGUI.BeginProperty(position, label, property);
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float columnWidth = Mathf.Clamp(position.width * 0.35f, 60f, 180f);
            var columnRect = new Rect(position.x, position.y, columnWidth, position.height);
            var arrowRect = new Rect(columnRect.xMax + Gap, position.y, ArrowWidth, position.height);
            var fontRect = new Rect(arrowRect.xMax + Gap, position.y, position.xMax - arrowRect.xMax - Gap, position.height);

            EditorGUI.PropertyField(columnRect, column, GUIContent.none);
            EditorGUI.LabelField(arrowRect, Arrow);
            EditorGUI.PropertyField(fontRect, font, GUIContent.none);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
