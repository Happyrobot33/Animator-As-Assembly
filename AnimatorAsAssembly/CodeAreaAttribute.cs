#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

public class CodeAreaAttribute : PropertyAttribute
{
    public bool readOnly = false;

    public GUIStyle lineStyleRead = new GUIStyle(EditorStyles.label);
    public GUIStyle codeStyleRead = new GUIStyle(EditorStyles.label);
    public GUIStyle lineStyleWrite = new GUIStyle(EditorStyles.textArea);
    public GUIStyle codeStyleWrite = new GUIStyle(EditorStyles.textArea);

    public CodeAreaAttribute()
    {
        InitStyles();
    }

    public CodeAreaAttribute(bool readOnly)
    {
        InitStyles();
        this.readOnly = readOnly;
    }

    public void InitStyles()
    {
        codeStyleRead.wordWrap = false;
        codeStyleRead.alignment = TextAnchor.UpperLeft;
        lineStyleRead.alignment = TextAnchor.UpperRight;

        codeStyleWrite.wordWrap = false;
        codeStyleWrite.alignment = TextAnchor.UpperLeft;
        lineStyleWrite.alignment = TextAnchor.UpperRight;

        //font set
        //find FiraCode-Regular.ttf
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        Font font = null;
        foreach (Font f in fonts)
        {
            if (f.name == "FiraCode-Regular")
            {
                font = f;
                break;
            }
        }
        codeStyleRead.font = font;
        codeStyleWrite.font = font;
        lineStyleRead.font = font;
        lineStyleWrite.font = font;
        Debug.Log(font);
    }
}
//create a property drawer that is like TextArea but has line numbers to the left of the text
//should use [CodeArea] above the string in the inspector
[CustomPropertyDrawer(typeof(CodeAreaAttribute))]
public class CodeAreaDrawer : PropertyDrawer
{
    int totalLines = 0;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        //get a reference to the attribute
        CodeAreaAttribute codeAreaAttribute = (CodeAreaAttribute)attribute;

        EditorGUI.BeginChangeCheck();

        position.height = codeAreaAttribute.codeStyleWrite.CalcHeight(new GUIContent(property.stringValue), 0);

        //create some space on the left for the line numbers by making a text area rect
        Rect textAreaRect = new Rect(position.x + 30, position.y, position.width - 30, position.height);

        //create another rect for the line numbers
        Rect lineNumbersRect = new Rect(position.x, position.y, 30, position.height);
        string lineNumbers = "";
        
        Profiler.BeginSample("Line Numbering");
        int LineNumber = 0;
        totalLines = property.stringValue.Split('\n').Length;
        string[] lines = property.stringValue.Split('\n');
        for (int i = 0; i <= totalLines - 1; i++)
        {
            string line = lines[i];
            //only count and add a line number if the line does not have a #, or is not empty
            if (line.Length > 0 && line[0] != '#')
            {
                lineNumbers += LineNumber + "\n";
                LineNumber++;
            }
            else
            {
                lineNumbers += "\n";
            }
        }
        Profiler.EndSample();
        

        Profiler.BeginSample("Read Only Check");
        //if the attribute is not read only, make it editable
        if (!codeAreaAttribute.readOnly)
        {
            var text = EditorGUI.TextArea(textAreaRect, property.stringValue, codeAreaAttribute.codeStyleWrite);
            //remove any zero width spaces
            text = text.Replace("\u200B", "");
            //remove any carriage returns
            text = text.Replace("\r", "");
            property.stringValue = text;
            EditorGUI.LabelField(lineNumbersRect, lineNumbers, codeAreaAttribute.lineStyleWrite);
        }
        else
        {
            EditorGUI.SelectableLabel(textAreaRect, property.stringValue, codeAreaAttribute.codeStyleRead);
            EditorGUI.SelectableLabel(lineNumbersRect, lineNumbers, codeAreaAttribute.lineStyleRead);
        }
        EditorGUI.EndChangeCheck();
        Profiler.EndSample();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            //get a reference to the attribute
            CodeAreaAttribute codeAreaAttribute = (CodeAreaAttribute)attribute;

            return codeAreaAttribute.codeStyleWrite.CalcHeight(new GUIContent(property.stringValue), 1);
        }
        else
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
#endif
