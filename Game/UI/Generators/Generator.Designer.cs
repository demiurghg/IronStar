﻿using Fusion.Core.Mathematics;
using Fusion.Core;
using Fusion.Engine.Frames;
using Fusion.Engine.Graphics;
using IronStar.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Core.Content;

namespace IronStar.UI.Generators
{
    public partial class MenuGenerator
    {
        public static string MainFont = "armata";
        public static string AdditionalFont = "opensans";

        private static Dictionary<Size2, int> MainFontSize = new Dictionary<Size2, int>()
        {
            { new Size2(1920, 1080), 36 },
            { new Size2(1600, 900), 36 },
            { new Size2(1366, 768), 24 },
            { new Size2(854, 480), 20 },
        };

        private static Dictionary<Size2, int> AdditionalFontSize = new Dictionary<Size2, int>()
        {
            { new Size2(1920, 1080), 24 },
            { new Size2(1600, 900), 24 },
            { new Size2(1366, 768), 20 },
            { new Size2(854, 480), 14 },
        };

        public struct StartMenuInfo
        {
            public static string LogoPosition = "center;40%";
            public static string LogoWidth = "50%";
            public static string LogoTexture = @"ui\logo";
            public static string BackgroundTexture = @"ui\background";
            public static string LabelPosition = "center;70%";
            public static string LabelWidth = "30%";
        }



        public struct MainMenuInfo
        {
            public static string LeftOffset = "10.5%";
            public static string TopOffset = "55%";
            public static string ButtonWidth = "16%";
            public static string ButtonHeight = "6.25%";
            public static string DescriptionPosition = "5%;95%";
            internal static float currentPosition = 0;

        }

        public struct SettingsMenuInfo
        {
            public static string LeftOffset = "35.8%";
            public static string TopOffset = "55%";
            public static string Width = "61%";
            public static string Height = "6.6%";
            internal static float currentPosition = 0;
        }

        public struct ButtonPointerInfo
        {
            public static string Width = "0.5%";
            public static string LeftOffset = "8.45%";
        }


        /// <summary>
        /// Get a size for main font which satisfied for resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int GetMainFontSize(int width, int height)
        {
            return GetFontSize(MainFontSize, width, height);
        }

        /// <summary>
        /// Get a size for additional font which satisfied for resolution
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static int GetAdditionalFontSize(int width, int height)
        {
            return GetFontSize(AdditionalFontSize, width, height);
        }

        private static int GetFontSize(Dictionary<Size2, int> map, int width, int height)
        {
            if (map.ContainsKey(new Size2(width, height)))
            {
                return map[new Size2(width, height)];
            }
            foreach (var size in map.Keys)
            {
                if (size.Height < height)
                {
                    return map[size];
                }
            }
            return map[map.Keys.Last()];
        }

        private static Size2 Autosize(Frame frame)
        {
            int w = 0, h = 0;

            if (frame is Image)
            {
                var image = frame as Image;
                w = image.Image.Width;
                h = image.Image.Height;
            }
            else
            if (frame is Label)
            {
                var label = frame as Label;
                var font = frame.Font;
                var r = font.MeasureString(frame.Text);
                w = r.Width;
                h = r.Height;
            }
            return new Size2(w, h);
        }


        private static Size2 GetLocation(string value, int width, int elementWidth, int height, int elementHeight)
        {
            var arr = value.Split(';');
            return new Size2(ParseLocation(arr[0], width, elementWidth), ParseLocation(arr[1], height, elementHeight));
        }

        private static Size2 GetBounds(string value, int width, int height)
        {
            var arr = value.Split(';');
            return new Size2(ParseBounds(arr[0], width), ParseBounds(arr[1], height));
        }

        private static int ParseLocation(string value, int side, int elementSide)
        {
            value = value.ToLowerInvariant();
            if (char.IsDigit(value[0]))
            {
                if (char.IsDigit(value[value.Length - 1]))
                {
                    return int.Parse(value);
                }
                else
                {
                    float modificator = float.Parse(value.Substring(0, value.Length - 1)) / 100f;
                    return (int)(side * modificator);
                }
            }
            else
            {
                if (value.Equals("center"))
                {
                    return side / 2 - elementSide / 2;
                }
                else if (value.Equals("left") || value.Equals("bottom"))
                {
                    return side - elementSide;
                }
                else //right
                {
                    return 0;
                }
            }
        }

        private static int ParseBounds(string value, int side)
        {
            if (char.IsDigit(value[0]))
            {
                if (char.IsDigit(value[value.Length - 1]))
                {
                    return int.Parse(value);
                }
                else
                {
                    float modificator = float.Parse(value.Substring(0, value.Length - 1)) / 100f;
                    return (int)(side * modificator);
                }
            }
            else
            {
                return -1;
            }
        }

    }
}