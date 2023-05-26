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

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //get a reference to the attribute
            CodeAreaAttribute codeAreaAttribute = (CodeAreaAttribute)attribute;

            EditorGUI.BeginChangeCheck();

            position.height = codeAreaAttribute.codeStyleWrite.CalcHeight(
                new GUIContent(property.stringValue),
                0
            );

            //create some space on the left for the line numbers by making a text area rect
            Rect textAreaRect = new Rect(
                position.x + 30,
                position.y,
                position.width - 30,
                position.height
            );

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
                var text = EditorGUI.TextArea(
                    textAreaRect,
                    syntaxHighlight(property.stringValue),
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

                            //if in a rich text tag, move the cursor to the closest end of the tag
                            if (inRichTextTag)
                            {
                                if (cursorIndex - tagStartIndex > tagEndIndex - cursorIndex)
                                {
                                    //move the cursor to the start of the tag
                                    tEditor.cursorIndex = tagStartIndex;
                                    tEditor.selectIndex = tagStartIndex;
                                    //if the cursor was at the end of the tag, move it left one
                                    if (cursorIndex + 1 == tagEndIndex)
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
                                    if (tempCursor - 1 == tagStartIndex)
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
                EditorGUI.SelectableLabel(
                    textAreaRect,
                    property.stringValue,
                    codeAreaAttribute.codeStyleRead
                );
                EditorGUI.SelectableLabel(
                    lineNumbersRect,
                    lineNumbers,
                    codeAreaAttribute.lineStyleRead
                );
            }
            EditorGUI.EndChangeCheck();
            Profiler.EndSample();
        }

        string col_comment = "#9C9491";
        string col_opcode = "#569CD6";
        string col_operand = "#D69D85";
        string col_label = "#4EC9B0";
        string col_register = "#B5CEA8";
        string col_GPUregister = "#D7BA7D";
        string col_input_register = "#D7BA7D";
        string col_internal_register = "#6666EA";
        string col_number = "#7B9726";
        string col_subroutine = "#C586C0";
        string col_string = "#159393";

        internal string syntaxHighlight(string text)
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
                            codeSplit[0] = colorize(codeSplit[0], col_opcode);
                            //if there is operands, color them
                            if (codeSplit.Length > 1)
                            {
                                for (int j = 1; j <= codeSplit.Length - 1; j++)
                                {
                                    //switch based on the first character of the operand
                                    switch (codeSplit[j][0])
                                    {
                                        //if the first character is a number, color it as a number
                                        case '$':
                                            codeSplit[j] = colorize(codeSplit[j], col_register);
                                            break;
                                        case '%':
                                            codeSplit[j] = colorize(codeSplit[j], col_label);
                                            break;
                                        case '&':
                                            codeSplit[j] = colorize(
                                                codeSplit[j],
                                                col_internal_register
                                            );
                                            break;
                                        case '!':
                                            codeSplit[j] = colorize(
                                                codeSplit[j],
                                                col_input_register
                                            );
                                            break;
                                        case '*':
                                            codeSplit[j] = colorize(codeSplit[j], col_GPUregister);
                                            break;
                                        case ';':
                                            codeSplit[j] = colorize(codeSplit[j], col_subroutine);
                                            break;
                                        default:
                                            // check if it is a number
                                            if (codeSplit[j][0] >= '0' && codeSplit[j][0] <= '9')
                                            {
                                                codeSplit[j] = colorize(codeSplit[j], col_number);
                                            }
                                            else
                                            {
                                                //check if it is a string
                                                if (codeSplit[j][0] == '"')
                                                {
                                                    codeSplit[j] = colorize(
                                                        codeSplit[j],
                                                        col_string
                                                    );
                                                }
                                                else
                                                {
                                                    //color it as an operand
                                                    codeSplit[j] = colorize(
                                                        codeSplit[j],
                                                        col_operand
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
                        commentSplit[1] = colorize("#" + commentSplit[1], col_comment);
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
