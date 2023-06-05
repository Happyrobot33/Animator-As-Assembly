#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;

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
            //Debug.Log(font);
        }
    }

    //create a property drawer that is like TextArea but has line numbers to the left of the text
    //should use [CodeArea] above the string in the inspector
    [CustomPropertyDrawer(typeof(CodeAreaAttribute))]
    public class CodeAreaDrawer : PropertyDrawer
    {
        int totalLines = 0;
        int previousCursorIndex = 0;

        SyntaxHighlighterSchemes.Themes.Enum currentSelectedTheme = SyntaxHighlighterSchemes
            .Themes
            .Enum
            .Forest;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //get a reference to the attribute
            CodeAreaAttribute codeAreaAttribute = (CodeAreaAttribute)attribute;

            EditorGUI.BeginChangeCheck();

            position.height = codeAreaAttribute.codeStyleWrite.CalcHeight(
                new GUIContent(property.stringValue),
                0
            );

            int lineNumberWidth = 30;

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
                    SyntaxHighlighterSchemes.Themes.GetTheme(currentSelectedTheme).base00
                );
                fillTexture.Apply();

                codeAreaAttribute.codeStyleWrite.normal.background = fillTexture;
                codeAreaAttribute.lineStyleWrite.normal.background = fillTexture;

                var text = EditorGUI.TextArea(
                    textAreaRect,
                    syntaxHighlight(
                        property.stringValue,
                        SyntaxHighlighterSchemes.Themes.GetTheme(currentSelectedTheme)
                    ),
                    codeAreaAttribute.codeStyleWrite
                );

                //get the active text editor
                TextEditor tEditor =
                    typeof(EditorGUI)
                        .GetField("activeEditor", BindingFlags.Static | BindingFlags.NonPublic)
                        .GetValue(null) as TextEditor;
                //check if the selected text editor is the one we are using
                if (tEditor != null)
                {
                    //check if the text editor is this one
                    if (tEditor.text == text)
                    {
                        //if multi selecting, dont do anything
                        if (tEditor.cursorIndex == tEditor.selectIndex)
                        {
                            //determine if the user cursor is in a rich text tag
                            int cursorIndex = tEditor.cursorIndex;

                            //check if the cursor is in a rich text tag
                            string textBeforeCursor = tEditor.text.Substring(0, cursorIndex);

                            bool inRichTextTag = true;
                            int tagStartIndex = 0;
                            int tagEndIndex = 0;
                            //check if the cursor is in a rich text tag, and if it is, find the start and end of the tag
                            for (int i = textBeforeCursor.Length - 1; i >= 0; i--)
                            {
                                if (textBeforeCursor[i] == '<')
                                {
                                    tagStartIndex = i;
                                    for (int j = cursorIndex; j < text.Length; j++)
                                    {
                                        if (text[j] == '>')
                                        {
                                            tagEndIndex = j + 1;
                                            break;
                                        }
                                    }
                                    break;
                                }
                                else if (textBeforeCursor[i] == '>')
                                {
                                    inRichTextTag = false;
                                    break;
                                }
                            }

                            //determine the cursor index in the current line
                            int cursorIndexInLine = 0;
                            for (int i = cursorIndex - 1; i >= 0; i--)
                            {
                                if (text[i] == '\n')
                                {
                                    break;
                                }
                                else
                                {
                                    cursorIndexInLine++;
                                }
                            }

                            //check if the line the cursor is on is the same as the line the cursor was on before
                            bool inSameLine = false;
                            int currentLine = 0;
                            int previousLine = 0;
                            for (int i = 0; i < cursorIndex; i++)
                            {
                                if (text[i] == '\n')
                                {
                                    currentLine++;
                                }
                            }
                            for (int i = 0; i < previousCursorIndex; i++)
                            {
                                if (text[i] == '\n')
                                {
                                    previousLine++;
                                }
                            }
                            if (currentLine == previousLine)
                            {
                                inSameLine = true;
                            }

                            //if in a rich text tag, move the cursor to the closest end of the tag
                            if (inRichTextTag)
                            {
                                if (cursorIndex - tagStartIndex > tagEndIndex - cursorIndex)
                                {
                                    //move the cursor to the start of the tag
                                    tEditor.cursorIndex = tagStartIndex;
                                    tEditor.selectIndex = tagStartIndex;
                                    //if the cursor was at the end of the tag, move it left one
                                    if (cursorIndex + 1 == tagEndIndex && inSameLine)
                                    {
                                        tEditor.cursorIndex--;
                                        tEditor.selectIndex--;
                                    }
                                }
                                else
                                {
                                    //remember where the cursor was before moving it
                                    int tempCursor = tEditor.cursorIndex;
                                    //move the cursor to the end of the tag, dont drag select
                                    tEditor.cursorIndex = tagEndIndex;
                                    tEditor.selectIndex = tagEndIndex;
                                    //if the cursor was at the start of the tag, move it right one
                                    if (tempCursor - 1 == tagStartIndex && inSameLine)
                                    {
                                        tEditor.cursorIndex++;
                                        tEditor.selectIndex++;
                                    }
                                }
                                /* Debug.Log(
                                    tagStartIndex
                                        + " "
                                        + cursorIndex
                                        + " "
                                        + tagEndIndex
                                        + " "
                                        + closerIndex
                                ); */
                            }

                            previousCursorIndex = tEditor.cursorIndex;
                        }
                    }
                }
                //remove all rich color tags
                //use this regex \<[^\>]+\>
                text = System.Text.RegularExpressions.Regex.Replace(text, @"\<[^\>]+\>", "");
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

        internal string syntaxHighlight(string text, SyntaxHighlighterSchemes.ColorTheme Theme)
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
                        //if there is an opcode part, color it
                        if (codeSplit.Length > 0)
                        {
                            //color the opcode part
                            codeSplit[0] = colorize(codeSplit[0], Theme.base0C);
                            //if there is operands, color them
                            if (codeSplit.Length > 1)
                            {
                                for (int j = 1; j <= codeSplit.Length - 1; j++)
                                {
                                    //check to see if the string has any length
                                    if (codeSplit[j].Length == 0)
                                    {
                                        continue;
                                    }
                                    //switch based on the first character of the operand
                                    switch (codeSplit[j][0])
                                    {
                                        //if the first character is a number, color it as a number
                                        case '$':
                                            codeSplit[j] = colorize(codeSplit[j], Theme.base08);
                                            break;
                                        case '%':
                                            codeSplit[j] = colorize(codeSplit[j], Theme.base09);
                                            break;
                                        case '&':
                                            codeSplit[j] = colorize(codeSplit[j], Theme.base0A);
                                            break;
                                        case '!':
                                            codeSplit[j] = colorize(codeSplit[j], Theme.base0B);
                                            break;
                                        case '*':
                                            codeSplit[j] = colorize(codeSplit[j], Theme.base0D);
                                            break;
                                        case ';':
                                            codeSplit[j] = colorize(codeSplit[j], Theme.base0E);
                                            break;
                                        default:
                                            // check if it is a number
                                            if (codeSplit[j][0] >= '0' && codeSplit[j][0] <= '9')
                                            {
                                                codeSplit[j] = colorize(codeSplit[j], Theme.base06);
                                            }
                                            else
                                            {
                                                //check if it is a string
                                                if (codeSplit[j][0] == '"')
                                                {
                                                    codeSplit[j] = colorize(
                                                        codeSplit[j],
                                                        Theme.base0F
                                                    );
                                                }
                                                else
                                                {
                                                    //color it as an operand
                                                    codeSplit[j] = colorize(
                                                        codeSplit[j],
                                                        Theme.base07
                                                    );
                                                }
                                            }
                                            break;
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
                        commentSplit[1] = colorize("#" + commentSplit[1], Theme.base03);
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
            text = text.Substring(0, text.Length - 1);
            return text;
        }

        internal string colorize(string text, string color)
        {
            if (text.Length > 0)
            {
                return "<color=" + color + ">" + text + "</color>";
            }
            return text;
        }

        internal string colorize(string text, Color color)
        {
            string colorstring = colorize(text, "#" + ColorUtility.ToHtmlStringRGB(color));
            return colorstring;
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
