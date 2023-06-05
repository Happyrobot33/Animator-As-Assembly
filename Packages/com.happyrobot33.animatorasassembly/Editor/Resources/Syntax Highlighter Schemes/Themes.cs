using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AnimatorAsAssembly.SyntaxHighlighterSchemes
{
    public struct ColorTheme
    {
        public string name;
        public Color base00;
        public Color base01;
        public Color base02;
        public Color base03;
        public Color base04;
        public Color base05;
        public Color base06;
        public Color base07;
        public Color base08;
        public Color base09;
        public Color base0A;
        public Color base0B;
        public Color base0C;
        public Color base0D;
        public Color base0E;
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
                theme.base00 = "#1b1918".HexToColor();
                theme.base01 = "#2c2421".HexToColor();
                theme.base02 = "#68615e".HexToColor();
                theme.base03 = "#766e6b".HexToColor();
                theme.base04 = "#9c9491".HexToColor();
                theme.base05 = "#a8a19f".HexToColor();
                theme.base06 = "#e6e2e0".HexToColor();
                theme.base07 = "#f1efee".HexToColor();
                theme.base08 = "#f22c40".HexToColor();
                theme.base09 = "#df5320".HexToColor();
                theme.base0A = "#c38418".HexToColor();
                theme.base0B = "#7b9726".HexToColor();
                theme.base0C = "#00ad9c".HexToColor();
                theme.base0D = "#407ee7".HexToColor();
                theme.base0E = "#6666ea".HexToColor();
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
                theme.base00 = "#1b1818".HexToColor();
                theme.base01 = "#292424".HexToColor();
                theme.base02 = "#585050".HexToColor();
                theme.base03 = "#655d5d".HexToColor();
                theme.base04 = "#7e7777".HexToColor();
                theme.base05 = "#8a8585".HexToColor();
                theme.base06 = "#e7dfdf".HexToColor();
                theme.base07 = "#f4ecec".HexToColor();
                theme.base08 = "#ca4949".HexToColor();
                theme.base09 = "#b45a3c".HexToColor();
                theme.base0A = "#a06e3b".HexToColor();
                theme.base0B = "#4b8b8b".HexToColor();
                theme.base0C = "#5485b6".HexToColor();
                theme.base0D = "#7272ca".HexToColor();
                theme.base0E = "#8464c4".HexToColor();
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
                theme.base00 = "#202746".HexToColor();
                theme.base01 = "#293256".HexToColor();
                theme.base02 = "#5e6687".HexToColor();
                theme.base03 = "#6b7394".HexToColor();
                theme.base04 = "#898ea4".HexToColor();
                theme.base05 = "#979db4".HexToColor();
                theme.base06 = "#dfe2f1".HexToColor();
                theme.base07 = "#f5f7ff".HexToColor();
                theme.base08 = "#c94922".HexToColor();
                theme.base09 = "#c76b29".HexToColor();
                theme.base0A = "#c08b30".HexToColor();
                theme.base0B = "#ac9739".HexToColor();
                theme.base0C = "#22a2c9".HexToColor();
                theme.base0D = "#3d8fd1".HexToColor();
                theme.base0E = "#6679cc".HexToColor();
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
                theme.base00 = "#161b1d".HexToColor();
                theme.base01 = "#1f292e".HexToColor();
                theme.base02 = "#516d7b".HexToColor();
                theme.base03 = "#5a7b8c".HexToColor();
                theme.base04 = "#7195a8".HexToColor();
                theme.base05 = "#7ea2b4".HexToColor();
                theme.base06 = "#c1e4f6".HexToColor();
                theme.base07 = "#ebf8ff".HexToColor();
                theme.base08 = "#d22d72".HexToColor();
                theme.base09 = "#935c25".HexToColor();
                theme.base0A = "#8a8a0f".HexToColor();
                theme.base0B = "#568c3b".HexToColor();
                theme.base0C = "#2d8f6f".HexToColor();
                theme.base0D = "#257fad".HexToColor();
                theme.base0E = "#6b6bb8".HexToColor();
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
