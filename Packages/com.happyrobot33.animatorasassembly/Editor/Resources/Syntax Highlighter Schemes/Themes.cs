using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimatorAsAssembly.SyntaxHighlighterSchemes
{
    public struct ColorTheme
    {
        public string name;
        public Color background;
        public Color base01;
        public Color base02;
        public Color comment;
        public Color base04;
        public Color command;
        public Color base06;
        public Color number;
        public Color error;
        public Color profilerName;
        public Color register;
        public Color base0B;
        public Color label;
        public Color instruction;
        public Color subroutine;
        public Color base0F;
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
                    background = "#1b1918".HexToColor(),
                    base01 = "#2c2421".HexToColor(),
                    base02 = "#68615e".HexToColor(),
                    comment = "#766e6b".HexToColor(),
                    base04 = "#9c9491".HexToColor(),
                    command = "#a8a19f".HexToColor(),
                    base06 = "#e6e2e0".HexToColor(),
                    number = "#f1efee".HexToColor(),
                    error = "#f22c40".HexToColor(),
                    profilerName = "#df5320".HexToColor(),
                    register = "#c38418".HexToColor(),
                    base0B = "#7b9726".HexToColor(),
                    label = "#00ad9c".HexToColor(),
                    instruction = "#407ee7".HexToColor(),
                    subroutine = "#6666ea".HexToColor(),
                    base0F = "#c33ff3".HexToColor()
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
                    background = "#1b1818".HexToColor(),
                    base01 = "#292424".HexToColor(),
                    base02 = "#585050".HexToColor(),
                    comment = "#655d5d".HexToColor(),
                    base04 = "#7e7777".HexToColor(),
                    command = "#8a8585".HexToColor(),
                    base06 = "#e7dfdf".HexToColor(),
                    number = "#f4ecec".HexToColor(),
                    error = "#ca4949".HexToColor(),
                    profilerName = "#b45a3c".HexToColor(),
                    register = "#a06e3b".HexToColor(),
                    base0B = "#4b8b8b".HexToColor(),
                    label = "#5485b6".HexToColor(),
                    instruction = "#7272ca".HexToColor(),
                    subroutine = "#8464c4".HexToColor(),
                    base0F = "#bd5187".HexToColor()
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
                    background = "#202746".HexToColor(),
                    base01 = "#293256".HexToColor(),
                    base02 = "#5e6687".HexToColor(),
                    comment = "#6b7394".HexToColor(),
                    base04 = "#898ea4".HexToColor(),
                    command = "#979db4".HexToColor(),
                    base06 = "#dfe2f1".HexToColor(),
                    number = "#f5f7ff".HexToColor(),
                    error = "#c94922".HexToColor(),
                    profilerName = "#c76b29".HexToColor(),
                    register = "#c08b30".HexToColor(),
                    base0B = "#ac9739".HexToColor(),
                    label = "#22a2c9".HexToColor(),
                    instruction = "#3d8fd1".HexToColor(),
                    subroutine = "#6679cc".HexToColor(),
                    base0F = "#9c637a".HexToColor()
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
                    background = "#161b1d".HexToColor(),
                    base01 = "#1f292e".HexToColor(),
                    base02 = "#516d7b".HexToColor(),
                    comment = "#5a7b8c".HexToColor(),
                    base04 = "#7195a8".HexToColor(),
                    command = "#7ea2b4".HexToColor(),
                    base06 = "#c1e4f6".HexToColor(),
                    number = "#ebf8ff".HexToColor(),
                    error = "#d22d72".HexToColor(),
                    profilerName = "#935c25".HexToColor(),
                    register = "#8a8a0f".HexToColor(),
                    base0B = "#568c3b".HexToColor(),
                    label = "#2d8f6f".HexToColor(),
                    instruction = "#257fad".HexToColor(),
                    subroutine = "#6b6bb8".HexToColor(),
                    base0F = "#b72dd2".HexToColor()
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

    //Convert hex to color
    public static class ColorExtensions
    {
        public static Color HexToColor(this string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
