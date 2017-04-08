using Fusion.Core.Mathematics;
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
    public class SettingsPageOptions : IPageOption
    {
        private readonly Game game;
        private readonly ShooterInterface ui;
        public SettingsPageOptions(Game game)
        {
            this.ui = (ShooterInterface)game.UserInterface.Instance;
            this.game = game;
        }


        [Background(0, "Background", @"ui\background")]
        public void Background() { }

        [NavigationButton(1, "GamePlayButton", "GAMEPLAY")]
        [Description("Gameplay Settings")]
        public void GamePlay_Click()
        {

        }

        [NavigationButton(2, "InputButton", "KEYBOARD & MOUSE")]
        [Description("Gameplay Settings")]
        public void Input_Click()
        {

        }

        [NavigationButton(3, "AudioButton", "AUDIO")]
        [Description("Audio Settings")]
        public void Audio_Click()
        {
            ui.SetActiveMenu("AudioSettingsMenu");
        }

        [NavigationButton(4, "VideoButton", "VIDEO")]
        [Description("Video Settings")]
        public void Video_Click()
        {

        }

        [DescriptionLabel(5, "Description", "Select[ENTER]    BACK[ESC]")]
        public void DescriptionLabel()
        {

        }


        [ButtonPointer(5, "ButtonPointer")]
        public void ButtonPointer()
        {

        }

#if EDITOR
        [NavigationButton(9999, "Option", "Editor")]
        [Description("Warning:: Creators mode")]
        public void Editor_Click()
        {
            game.Invoker.ExecuteCommand("map testMap2 /edit");
        }
#endif
    }
}
