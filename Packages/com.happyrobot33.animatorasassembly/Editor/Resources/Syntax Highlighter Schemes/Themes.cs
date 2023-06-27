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
                ColorTheme theme = new ColorTheme();
                theme.name = "Forest";
                theme.background = "#1b1918".HexToColor();
                theme.base01 = "#2c2421".HexToColor();
                theme.base02 = "#68615e".HexToColor();
                theme.comment = "#766e6b".HexToColor();
                theme.base04 = "#9c9491".HexToColor();
                theme.command = "#a8a19f".HexToColor();
                theme.base06 = "#e6e2e0".HexToColor();
                theme.number = "#f1efee".HexToColor();
                theme.error = "#f22c40".HexToColor();
                theme.profilerName = "#df5320".HexToColor();
                theme.register = "#c38418".HexToColor();
                theme.base0B = "#7b9726".HexToColor();
                theme.label = "#00ad9c".HexToColor();
                theme.instruction = "#407ee7".HexToColor();
                theme.subroutine = "#6666ea".HexToColor();
                theme.base0F = "#c33ff3".HexToColor();
                return theme;
            }
        }

        public static ColorTheme Plateau
        {
            get
            {
                ColorTheme theme = new ColorTheme();
                theme.name = "Plateau";
                theme.background = "#1b1818".HexToColor();
                theme.base01 = "#292424".HexToColor();
                theme.base02 = "#585050".HexToColor();
                theme.comment = "#655d5d".HexToColor();
                theme.base04 = "#7e7777".HexToColor();
                theme.command = "#8a8585".HexToColor();
                theme.base06 = "#e7dfdf".HexToColor();
                theme.number = "#f4ecec".HexToColor();
                theme.error = "#ca4949".HexToColor();
                theme.profilerName = "#b45a3c".HexToColor();
                theme.register = "#a06e3b".HexToColor();
                theme.base0B = "#4b8b8b".HexToColor();
                theme.label = "#5485b6".HexToColor();
                theme.instruction = "#7272ca".HexToColor();
                theme.subroutine = "#8464c4".HexToColor();
                theme.base0F = "#bd5187".HexToColor();
                return theme;
            }
        }

        public static ColorTheme SulphurPool
        {
            get
            {
                ColorTheme theme = new ColorTheme();
                theme.name = "Sulphur Pool";
                theme.background = "#202746".HexToColor();
                theme.base01 = "#293256".HexToColor();
                theme.base02 = "#5e6687".HexToColor();
                theme.comment = "#6b7394".HexToColor();
                theme.base04 = "#898ea4".HexToColor();
                theme.command = "#979db4".HexToColor();
                theme.base06 = "#dfe2f1".HexToColor();
                theme.number = "#f5f7ff".HexToColor();
                theme.error = "#c94922".HexToColor();
                theme.profilerName = "#c76b29".HexToColor();
                theme.register = "#c08b30".HexToColor();
                theme.base0B = "#ac9739".HexToColor();
                theme.label = "#22a2c9".HexToColor();
                theme.instruction = "#3d8fd1".HexToColor();
                theme.subroutine = "#6679cc".HexToColor();
                theme.base0F = "#9c637a".HexToColor();
                return theme;
            }
        }

        public static ColorTheme Lakeside
        {
            get
            {
                ColorTheme theme = new ColorTheme();
                theme.name = "Lakeside";
                theme.background = "#161b1d".HexToColor();
                theme.base01 = "#1f292e".HexToColor();
                theme.base02 = "#516d7b".HexToColor();
                theme.comment = "#5a7b8c".HexToColor();
                theme.base04 = "#7195a8".HexToColor();
                theme.command = "#7ea2b4".HexToColor();
                theme.base06 = "#c1e4f6".HexToColor();
                theme.number = "#ebf8ff".HexToColor();
                theme.error = "#d22d72".HexToColor();
                theme.profilerName = "#935c25".HexToColor();
                theme.register = "#8a8a0f".HexToColor();
                theme.base0B = "#568c3b".HexToColor();
                theme.label = "#2d8f6f".HexToColor();
                theme.instruction = "#257fad".HexToColor();
                theme.subroutine = "#6b6bb8".HexToColor();
                theme.base0F = "#b72dd2".HexToColor();
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
        }
    }

    //Convert hex to color
    public static class ColorExtensions
    {
        public static Color HexToColor(this string hex)
        {
            Color color = new Color();
            ColorUtility.TryParseHtmlString(hex, out color);
            return color;
        }
    }
}
