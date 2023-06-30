﻿#if UNITY_EDITOR
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.Profiling;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

namespace AnimatorAsAssembly
{
    /// <summary> Creates a text area with line numbers </summary>
    /// <remarks> Certain characters are not counted as a line, such as # </remarks>
    public class CodeAreaAttribute : PropertyAttribute
    {
        public GUIStyle lineStyle = new GUIStyle(EditorStyles.textArea);
        public GUIStyle codeStyle = new GUIStyle(EditorStyles.textArea);

        public CodeAreaAttribute()
        {
            InitStyles();
        }

        public void InitStyles()
        {
            codeStyle.wordWrap = false;
            codeStyle.alignment = TextAnchor.UpperLeft;
            lineStyle.alignment = TextAnchor.UpperRight;

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
            codeStyle.font = font;
            codeStyle.richText = true;
            lineStyle.font = font;

            //font size
            const int size = 14;
            codeStyle.fontSize = size;
            lineStyle.fontSize = size;
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

        string coloredText = "";
        private readonly Texture2D backgroundTexture = new Texture2D(1, 1);
        bool onVisible = false;
        CodeAreaAttribute codeAreaAttribute;
        GUIStyle invisibleStyle;
        private void OnEnable(SerializedProperty property)
        {
            coloredText = SyntaxHighlight(
                        property.stringValue,
                        SyntaxHighlighterSchemes.Themes.GetTheme(currentSelectedTheme)
                    );

            //get a reference to the attribute
            codeAreaAttribute = (CodeAreaAttribute)attribute;

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

            //initialize the invisible style
            invisibleStyle = new GUIStyle(codeAreaAttribute.codeStyle);
            //create a invisible texture
            Texture2D invisTexture = new Texture2D(1, 1);
            invisTexture.SetPixel(0, 0, Color.clear);
            invisTexture.Apply();
            invisibleStyle.normal.background = invisTexture;
            invisibleStyle.hover.background = invisTexture;
            invisibleStyle.active.background = invisTexture;
            invisibleStyle.focused.background = invisTexture;
            Color clear = Color.clear;
            invisibleStyle.normal.textColor = clear;
            invisibleStyle.hover.textColor = clear;
            invisibleStyle.active.textColor = clear;
            invisibleStyle.focused.textColor = clear;

            SetBackgroundTexture(currentSelectedTheme);
        }

        private void SetBackgroundTexture(SyntaxHighlighterSchemes.Themes.Enum theme)
        {
            backgroundTexture.SetPixel(
                0,
                0,
                ColorUtility.TryParseHtmlString(
                    SyntaxHighlighterSchemes.Themes.GetTheme(theme).background,
                    out Color color
                )
                    ? color
                    : Color.white
                );
            backgroundTexture.Apply();

            codeAreaAttribute.codeStyle.normal.background = backgroundTexture;
            codeAreaAttribute.codeStyle.hover.background = backgroundTexture;
            codeAreaAttribute.codeStyle.active.background = backgroundTexture;
            codeAreaAttribute.codeStyle.focused.background = backgroundTexture;

            codeAreaAttribute.lineStyle.normal.background = backgroundTexture;
            codeAreaAttribute.lineStyle.hover.background = backgroundTexture;
            codeAreaAttribute.lineStyle.active.background = backgroundTexture;
            codeAreaAttribute.lineStyle.focused.background = backgroundTexture;
        }

        // TODO: Figure out why TF this renders differently on a vertical monitor
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            //make a fake OnEnable function
            if (!onVisible)
            {
                OnEnable(property);
                onVisible = true;
            }

            EditorGUI.BeginChangeCheck();

            position.height = codeAreaAttribute.codeStyle.CalcHeight(
                new GUIContent(property.stringValue),
                0
            );

            const int lineNumberWidth = 30;

            string userText;

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

            int LineNumber = 0;
            totalLines = property.stringValue.Split('\n').Length;
            string[] lines = property.stringValue.Split('\n');
            for (int i = 0; i <= totalLines - 1; i++)
            {
                string line = lines[i];
                line = line.TrimStart(' ');
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

            //create a dropdown menu for the code style
            currentSelectedTheme = (SyntaxHighlighterSchemes.Themes.Enum)
                EditorGUILayout.EnumPopup(label: "Theme", selected: currentSelectedTheme);

            //ALL of this mess essentially just makes a label behind the text area the user interacts with that is highlighted.
            //The box the user is actually typing into is completely invisible
            EditorGUI.LabelField(
                textAreaRect,
                coloredText,
                codeAreaAttribute.codeStyle
            );
            userText = EditorGUI.TextArea(
                textAreaRect,
                property.stringValue,
                invisibleStyle
            );
            EditorGUI.LabelField(
                lineNumbersRect,
                lineNumbers,
                codeAreaAttribute.lineStyle
            );

            //if the user has changed the text, update whats needed
            if (EditorGUI.EndChangeCheck())
            {
                //remove any zero width spaces
                userText = userText.Replace("\u200B", "");
                //remove any carriage returns
                userText = userText.Replace("\r", "");
                property.stringValue = userText;
                coloredText = SyntaxHighlight(
                        property.stringValue,
                        SyntaxHighlighterSchemes.Themes.GetTheme(currentSelectedTheme)
                    );

                //save the selected theme to the editor prefs
                EditorPrefs.SetInt("CodeAreaTheme", (int)currentSelectedTheme);

                SetBackgroundTexture(currentSelectedTheme);
            }

            //create copy buttons for different visual styles
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy Rich Text"))
            {
                GUIUtility.systemCopyBuffer = coloredText;
            }
            if (GUILayout.Button("Copy Discord Text"))
            {
                GUIUtility.systemCopyBuffer = "```ansi\n" + RichTextToASCII(coloredText) + "\n```";
            }
            if (GUILayout.Button("Copy Plain Text"))
            {
                GUIUtility.systemCopyBuffer = userText;
            }
            EditorGUILayout.EndHorizontal();
        }

        internal string SyntaxHighlight(string text, SyntaxHighlighterSchemes.ColorTheme Theme)
        {
            Profiler.BeginSample("Syntax Highlighting");
            //loop through each line
            string[] lines = text.Split('\n');
            for (int i = 0; i <= lines.Length - 1; i++)
            {
                Profiler.BeginSample("Line Highlighting");
                string line = lines[i];
                string trimmedLine = line.TrimStart(' ');
                //trim any spaces from the start, remembering the amount trimmed
                int spacesTrimmed = line.Length - trimmedLine.Length;
                line = trimmedLine;

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
                                        Profiler.BeginSample("Reflection Based Colorization");
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
                                        string colorValue;
                                        //if the color is null, color it as error
                                        if (value == null)
                                        {
                                            colorValue = Theme.error;
                                        }
                                        else
                                        {
                                            colorValue = (string)value;
                                        }
                                        codeSplit[j] = Colorize(codeSplit[j], colorValue);
                                        Profiler.EndSample();
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
                            else
                            {
                                for (int j = 0; j <= codeSplit.Length - 1; j++)
                                {
                                    //colorize them as error
                                    codeSplit[j] = Colorize(codeSplit[j], Theme.error);
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
                line = line.PadLeft(line.Length + spacesTrimmed, ' ');
                lines[i] = line;
                Profiler.EndSample();
            }

            //recombine the lines
            text = "";
            foreach (string line in lines)
            {
                text += line + "\n";
            }
            Profiler.EndSample();
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

        /// <summary> This will take in a string with rich text color tags and return a string using ascii color codes instead </summary>
        private string RichTextToASCII(string text)
        {
            //use this regex to get just the color tags
            //the first group is the color hex itself
            Regex tag = new Regex("<color=#([a-zA-Z0-9]+)>");
            //remove all closing tags
            text = text.Replace("</color>", "");
            //get all the matches
            MatchCollection tagMatches = tag.Matches(text);
            //loop through each match
            foreach (Match match in tagMatches)
            {
                //get the color tag
                string colorTag = match.Groups[1].Value;

                //find the closest ascii color
                string asciiColor = HexToANSI(colorTag);

                //replace the color tag with the ascii color
                text = text.Replace(match.Value, asciiColor);
            }
            return text;
        }

        public static string HexToANSI(string hex)
        {
            //static dictionary of all the colors available and their equivalent RGB
            Dictionary<Color, int> colorTable = new Dictionary<Color, int>()
            {
                { DecimalColor(70, 70, 70), 30 }, //gray
                { DecimalColor(255,0,0), 31}, //red
                { DecimalColor(0,255,0), 32}, //green
                { DecimalColor(255,255,0), 33}, //yellow
                { DecimalColor(0,0,255), 34}, //blue
                { DecimalColor(247,15,232), 35}, //pink
                { DecimalColor(0,247,247), 34}, //cyan
                { DecimalColor(255,255,255), 35}, //white
            };
            //convert the hex to a color
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);

            //get the closest color
            int closestColor = ClosestColor(colorTable.Keys.ToList(), color);

            //get the int value of the color
            int colorValue = colorTable[colorTable.Keys.ToList()[closestColor]];

            //return the ascii color code
            return "\u001b[0;" + colorValue + "m";
        }

        // closed match in RGB space
        public static int ClosestColor(List<Color> colors, Color target)
        {
            var colorDiffs = colors.Select(n => ColorDiff(n, target)).Min(n => n);
            return colors.FindIndex(n => ColorDiff(n, target) == colorDiffs);
        }

        // distance in RGB space
        public static int ColorDiff(Color c1, Color c2)
        {
            int c1r = (int)(c1.r * 255);
            int c1g = (int)(c1.g * 255);
            int c1b = (int)(c1.b * 255);
            int c2r = (int)(c2.r * 255);
            int c2g = (int)(c2.g * 255);
            int c2b = (int)(c2.b * 255);
            return (int)Math.Sqrt(((c1r - c2r) * (c1r - c2r))
                                + ((c1g - c2g) * (c1g - c2g))
                                + ((c1b - c2b) * (c1b - c2b)));
        }

        private static Color DecimalColor(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                //get a reference to the attribute
                CodeAreaAttribute codeAreaAttribute = (CodeAreaAttribute)attribute;

                return codeAreaAttribute.codeStyle.CalcHeight(
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
