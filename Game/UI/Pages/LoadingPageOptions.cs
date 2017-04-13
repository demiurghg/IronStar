﻿using Fusion.Core.Mathematics;
using Fusion.Engine.Common;
using Fusion.Engine.Graphics;
using IronStar.UI.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronStar.UI.Pages
{
    public class LoadingPageOptions : IPageOption
    {
        private readonly Game game;
        public LoadingPageOptions(Game game)
        {
            this.game = game;
        }

        [Background(0, "Background", @"ui\background")]
        public void Background() { }

        [Logo(1,"Logo", @"ui\logo")]
        public void Logo() { }

        [StartLabel(2,"Label", "Loading ... ")]
        public string Label;

    }
}