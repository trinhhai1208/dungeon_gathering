using DungeonGathering.Catalog;
using UnityEditor;
using UnityEngine;

namespace DungeonGathering.EditorTools
{
    /// <summary>
    /// Vẽ <see cref="Overridable{T}"/>: [✓] nhãn [giá trị] trên một dòng (ô giá trị mờ khi chưa bật).
    /// Giá trị nhiều field (vd MeleeHitbox) vẽ thụt vào bên dưới khi bật.
    /// </summary>
    [CustomPropertyDrawer(typeof(Overridable<>))]
    public sealed class OverridableDrawer : PropertyDrawer
    {
        static bool IsCompound(SerializedProperty value) => value.propertyType == SerializedPropertyType.Generic && value.hasVisibleChildren;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var value = property.FindPropertyRelative("value");
            var enabled = property.FindPropertyRelative("enabled");
            float h = EditorGUIUtility.singleLineHeight;
            if (value != null && IsCompound(value) && enabled.boolValue)
                h += EditorGUI.GetPropertyHeight(value, GUIContent.none, true) + EditorGUIUtility.standardVerticalSpacing;
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enabled = property.FindPropertyRelative("enabled");
            var value = property.FindPropertyRelative("value");
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            var lineRect = new Rect(position.x, position.y, position.width, line);
            var labelRect = EditorGUI.IndentedRect(new Rect(lineRect.x, lineRect.y, EditorGUIUtility.labelWidth, line));
            var toggleRect = new Rect(labelRect.x, labelRect.y, 18f, line);
            var nameRect = new Rect(labelRect.x + 18f, labelRect.y, Mathf.Max(0f, labelRect.width - 18f), line);
            var valueRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                                     position.width - EditorGUIUtility.labelWidth, line);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            enabled.boolValue = EditorGUI.Toggle(toggleRect, enabled.boolValue);
            EditorGUI.LabelField(nameRect, new GUIContent(label.text, "Bật để ghi đè giá trị mặc định của SO gốc"));

            if (value != null && IsCompound(value))
            {
                if (!enabled.boolValue)
                    EditorGUI.LabelField(valueRect, "(mặc định của SO gốc)", EditorStyles.miniLabel);
                else
                {
                    EditorGUI.indentLevel = indent + 1;
                    var body = new Rect(position.x, position.y + line + EditorGUIUtility.standardVerticalSpacing,
                                        position.width, EditorGUI.GetPropertyHeight(value, GUIContent.none, true));
                    // Vẽ từng field con (không vẽ foldout của struct)
                    var it = value.Copy();
                    var end = value.GetEndProperty();
                    float y = body.y;
                    if (it.NextVisible(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(it, end)) break;
                            float h = EditorGUI.GetPropertyHeight(it, true);
                            EditorGUI.PropertyField(new Rect(body.x, y, body.width, h), it, true);
                            y += h + EditorGUIUtility.standardVerticalSpacing;
                        } while (it.NextVisible(false));
                    }
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(!enabled.boolValue))
                    EditorGUI.PropertyField(valueRect, value, GUIContent.none);
            }
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
}
