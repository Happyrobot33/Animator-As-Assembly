using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimatorAsAssembly.SyntaxHighlighterSchemes
{
    public struct ColorTheme
    {
        public string name;
        public string background;
        public string base01;
        public string base02;
        public string comment;
        public string base04;
        public string command;
        public string base06;
        public string number;
        public string error;
        public string profilerName;
        public string register;
        public string base0B;
        public string label;
        public string instruction;
        public string subroutine;
        public string conditional;
    }

    public static class Themes
    {
        public static ColorTheme Forest
        {
            get
            {
                ColorTheme theme = new ColorTheme
                {
                    name = "Forest",
                    background = "#1b1918",
                    base01 = "#2c2421",
                    base02 = "#68615e",
                    comment = "#766e6b",
                    base04 = "#9c9491",
                    command = "#a8a19f",
                    base06 = "#e6e2e0",
                    number = "#f1efee",
                    error = "#f22c40",
                    profilerName = "#df5320",
                    register = "#c38418",
                    base0B = "#7b9726",
                    label = "#00ad9c",
                    instruction = "#407ee7",
                    subroutine = "#6666ea",
                    conditional = "#c33ff3"
                };
                return theme;
            }
        }

        public static ColorTheme Plateau
        {
            get
            {
                ColorTheme theme = new ColorTheme
                {
                    name = "Plateau",
                    background = "#1b1818",
                    base01 = "#292424",
                    base02 = "#585050",
                    comment = "#655d5d",
                    base04 = "#7e7777",
                    command = "#8a8585",
                    base06 = "#e7dfdf",
                    number = "#f4ecec",
                    error = "#ca4949",
                    profilerName = "#b45a3c",
                    register = "#a06e3b",
                    base0B = "#4b8b8b",
                    label = "#5485b6",
                    instruction = "#7272ca",
                    subroutine = "#8464c4",
                    conditional = "#bd5187"
                };
                return theme;
            }
        }

        public static ColorTheme SulphurPool
        {
            get
            {
                ColorTheme theme = new ColorTheme
                {
                    name = "Sulphur Pool",
                    background = "#202746",
                    base01 = "#293256",
                    base02 = "#5e6687",
                    comment = "#6b7394",
                    base04 = "#898ea4",
                    command = "#979db4",
                    base06 = "#dfe2f1",
                    number = "#f5f7ff",
                    error = "#c94922",
                    profilerName = "#c76b29",
                    register = "#c08b30",
                    base0B = "#ac9739",
                    label = "#22a2c9",
                    instruction = "#3d8fd1",
                    subroutine = "#6679cc",
                    conditional = "#9c637a"
                };
                return theme;
            }
        }

        public static ColorTheme Lakeside
        {
            get
            {
                ColorTheme theme = new ColorTheme
                {
                    name = "Lakeside",
                    background = "#161b1d",
                    base01 = "#1f292e",
                    base02 = "#516d7b",
                    comment = "#5a7b8c",
                    base04 = "#7195a8",
                    command = "#7ea2b4",
                    base06 = "#c1e4f6",
                    number = "#ebf8ff",
                    error = "#d22d72",
                    profilerName = "#935c25",
                    register = "#8a8a0f",
                    base0B = "#568c3b",
                    label = "#2d8f6f",
                    instruction = "#257fad",
                    subroutine = "#6b6bb8",
                    conditional = "#b72dd2"
                };
                return theme;
            }
        }

        public enum Enum
        {
            Forest,
            Plateau,
            SulphurPool,
            Lakeside
        }

        public static ColorTheme GetTheme(Enum theme)
        {
#pragma warning disable IDE0066 // Convert switch statement to expression
            switch (theme)
            {
                case Enum.Forest:
                    return Forest;
                case Enum.Plateau:
                    return Plateau;
                case Enum.SulphurPool:
                    return SulphurPool;
                case Enum.Lakeside:
                    return Lakeside;
                default:
                    return Forest;
            }
#pragma warning restore IDE0066 // Convert switch statement to expression
        }
    }
}
