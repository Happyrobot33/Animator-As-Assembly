#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using System;

namespace AnimatorAsAssembly
{
    /// <summary> Creates a text area with line numbers </summary>
    /// <remarks> Certain characters are not counted as a line, such as # </remarks>
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
            //first load all resources in the resources folder
            //this is needed because unity just doesn't load them otherwise
            Resources.LoadAll("Fonts");
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
            codeStyleWrite.richText = true;
            lineStyleRead.font = font;
            lineStyleWrite.font = font;

            //font size
            const int size = 14;
            codeStyleRead.fontSize = size;
            codeStyleWrite.fontSize = size;
            lineStyleRead.fontSize = size;
            lineStyleWrite.fontSize = size;
            //Debug.Log(font);
        }
    }

    //create a property drawer that is like TextArea but has line numbers to the left of the text
    //should use [CodeArea] above the string in the inspector
    [CustomPropertyDrawer(typeof(CodeAreaAttribute))]
    public class CodeAreaDrawer : PropertyDrawer
    {
        int totalLines = 0;

        SyntaxHighlighterSchemes.Themes.Enum currentSelectedTheme = SyntaxHighlighterSchemes
            .Themes
            .Enum
            .Forest;

        // TODO: Figure out why TF this renders differently on a vertical monitor
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //get a reference to the attribute
            CodeAreaAttribute codeAreaAttribute = (CodeAreaAttribute)attribute;

            EditorGUI.BeginChangeCheck();

            position.height = codeAreaAttribute.codeStyleWrite.CalcHeight(
                new GUIContent(property.stringValue),
                0
            );

            const int lineNumberWidth = 30;

            //create some space on the left for the line numbers by making a text area rect
            Rect textAreaRect = new Rect(
                position.x + lineNumberWidth,
                position.y,
                position.width - lineNumberWidth,
                position.height
            );

            //create another rect for the line numbers
            Rect lineNumbersRect = new Rect(
                textAreaRect.x - lineNumberWidth,
                textAreaRect.y,
                lineNumberWidth,
                textAreaRect.height
            );
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
                //check if the editorpref exists
                if (!EditorPrefs.HasKey("CodeAreaTheme"))
                {
                    //if it doesnt, set it to the default theme
                    EditorPrefs.SetInt(
                        "CodeAreaTheme",
                        (int)SyntaxHighlighterSchemes.Themes.Enum.Forest
                    );
                }

                //set the current theme to the one saved in the editor prefs
                currentSelectedTheme = (SyntaxHighlighterSchemes.Themes.Enum)
                    EditorPrefs.GetInt("CodeAreaTheme", (int)currentSelectedTheme);

                //create a dropdown menu for the code style
                currentSelectedTheme = (SyntaxHighlighterSchemes.Themes.Enum)
                    EditorGUILayout.EnumPopup(label: "Theme", selected: currentSelectedTheme);

                //save the selected theme to the editor prefs
                EditorPrefs.SetInt("CodeAreaTheme", (int)currentSelectedTheme);

                //create a dummy fill texture2d with the background color of the text area
                Texture2D fillTexture = new Texture2D(1, 1);
                fillTexture.SetPixel(
                    0,
                    0,
                    SyntaxHighlighterSchemes.Themes.GetTheme(currentSelectedTheme).background
                );
                fillTexture.Apply();

                codeAreaAttribute.codeStyleWrite.normal.background = fillTexture;
                codeAreaAttribute.codeStyleWrite.hover.background = fillTexture;
                codeAreaAttribute.codeStyleWrite.active.background = fillTexture;
                codeAreaAttribute.codeStyleWrite.focused.background = fillTexture;

                codeAreaAttribute.lineStyleWrite.normal.background = fillTexture;
                codeAreaAttribute.lineStyleWrite.hover.background = fillTexture;
                codeAreaAttribute.lineStyleWrite.active.background = fillTexture;
                codeAreaAttribute.lineStyleWrite.focused.background = fillTexture;

                //ALL of this mess essentially just makes a label behind the text area the user interacts with that is highlighted.
                //The box the user is actually typing into is completely invisible
                EditorGUI.LabelField(
                    textAreaRect,
                    SyntaxHighlight(
                        property.stringValue,
                        SyntaxHighlighterSchemes.Themes.GetTheme(currentSelectedTheme)
                    ),
                    codeAreaAttribute.codeStyleWrite
                );
                GUIStyle invis = new GUIStyle(codeAreaAttribute.codeStyleWrite);
                //create a invisible texture
                Texture2D invisTexture = new Texture2D(1, 1);
                invisTexture.SetPixel(0, 0, Color.clear);
                invisTexture.Apply();
                invis.normal.background = invisTexture;
                invis.hover.background = invisTexture;
                invis.active.background = invisTexture;
                invis.focused.background = invisTexture;
                Color clear = Color.clear;
                invis.normal.textColor = clear;
                invis.hover.textColor = clear;
                invis.active.textColor = clear;
                invis.focused.textColor = clear;
                string text = EditorGUI.TextArea(
                    textAreaRect,
                    property.stringValue,
                    invis
                );

                //remove any zero width spaces
                text = text.Replace("\u200B", "");
                //remove any carriage returns
                text = text.Replace("\r", "");
                //GUI.contentColor = Random.ColorHSV();
                property.stringValue = text;
                EditorGUI.LabelField(
                    lineNumbersRect,
                    lineNumbers,
                    codeAreaAttribute.lineStyleWrite
                );
            }
            else
            {
                EditorGUI.LabelField(
                    textAreaRect,
                    property.stringValue,
                    codeAreaAttribute.codeStyleRead
                );
                EditorGUI.LabelField(lineNumbersRect, lineNumbers, codeAreaAttribute.lineStyleRead);
            }
            EditorGUI.EndChangeCheck();
            Profiler.EndSample();
        }

        internal string SyntaxHighlight(string text, SyntaxHighlighterSchemes.ColorTheme Theme)
        {
            //loop through each line
            string[] lines = text.Split('\n');
            for (int i = 0; i <= lines.Length - 1; i++)
            {
                string line = lines[i];
                //if the line is not empty
                if (line.Length > 0)
                {
                    //split the line into two parts, the code and the comment
                    string[] commentSplit = line.Split('#');
                    //if there is a code part, color it
                    if (commentSplit.Length > 0)
                    {
                        //split the code part into parts, the opcode and the operands
                        string[] codeSplit = commentSplit[0].Split(' ');
                        //if there is an opcode part, use reflection to get it
                        if (codeSplit.Length > 0)
                        {
                            //get the opcode
                            string opcode = codeSplit[0];
                            //get the Commands namespace
                            const string nameSpace = "AnimatorAsAssembly.Commands.";

                            //create the relevant states based on the instruction type using reflection
                            Type type = Type.GetType(nameSpace + opcode);
                            if (type != null)
                            {
                                //call the static method GetColoration
                                MethodInfo method = type.GetMethod("GetColoration");
                                if (method != null)
                                {
                                    //get the coloration
                                    string[] coloration = (string[])method.Invoke(null, null);
                                    //colorize each part of the code
                                    for (int j = 0; j <= codeSplit.Length - 1; j++)
                                    {
                                        //color index
                                        string color;
                                        try
                                        {
                                            color = coloration[j];
                                        }
                                        catch (IndexOutOfRangeException)
                                        {
                                            //color it as error
                                            color = "error";
                                        }
                                        //get the color itself
                                        object value = Theme.GetType().GetField(color).GetValue(Theme);
                                        Color colorValue;
                                        //if the color is null, color it as error
                                        if (value == null)
                                        {
                                            colorValue = Theme.error;
                                        }
                                        else
                                        {
                                            colorValue = (Color)value;
                                        }
                                        codeSplit[j] = Colorize(codeSplit[j], colorValue);
                                    }
                                }
                                else
                                {
                                    for (int j = 0; j <= codeSplit.Length - 1; j++)
                                    {
                                        //colorize them as error
                                        codeSplit[j] = Colorize(codeSplit[j], Theme.error);
                                    }
                                }
                            }
                        }
                        //recombine the code parts
                        commentSplit[0] = "";
                        foreach (string codePart in codeSplit)
                        {
                            commentSplit[0] += codePart + " ";
                        }
                        //exclude the last space
                        commentSplit[0] = commentSplit[0].Substring(0, commentSplit[0].Length - 1);
                    }
                    //if there is a comment part, color it
                    if (commentSplit.Length > 1)
                    {
                        //color the comment part
                        commentSplit[1] = Colorize("#" + commentSplit[1], Theme.comment);
                    }

                    //recombine the comment parts
                    line = "";
                    foreach (string commentPart in commentSplit)
                    {
                        line += commentPart;
                    }
                }
                //replace the line with the colored line
                lines[i] = line;
            }

            //recombine the lines
            text = "";
            foreach (string line in lines)
            {
                text += line + "\n";
            }
            //exclude the last newline
            return text.Substring(0, text.Length - 1);
        }

        internal string Colorize(string text, string color)
        {
            if (text.Length > 0)
            {
                return "<color=" + color + ">" + text + "</color>";
            }
            return text;
        }

        internal string Colorize(string text, Color color)
        {
            return Colorize(text, "#" + ColorUtility.ToHtmlStringRGB(color));
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                //get a reference to the attribute
                CodeAreaAttribute codeAreaAttribute = (CodeAreaAttribute)attribute;

                return codeAreaAttribute.codeStyleWrite.CalcHeight(
                    new GUIContent(property.stringValue),
                    1
                );
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }
    }
}
#endif
