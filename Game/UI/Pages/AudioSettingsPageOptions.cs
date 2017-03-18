using Fusion.Core.Configuration;
using Fusion.Core.Mathematics;
using Fusion.Engine.Audio;
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
    public class AudioSettingsPageOptions : IPageOption
    {
        private readonly Game game;
        private readonly ShooterInterface ui;
        public SoundSystem soundSystem;
        public AudioSettingsPageOptions(Game game)
        {
            this.ui = (ShooterInterface)game.UserInterface.Instance;
            this.game = game;
            soundSystem = game.Config.GetConfig<SoundSystem>();
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

        }

        [NavigationButton(4, "VideoButton", "VIDEO")]
        [Description("Video Settings")]
        public void Video_Click()
        {

        }

        [DescriptionLabel(5, "Description", "Select[ENTER]   BACK[ESC]")]
        public void DescriptionLabel()
        {

        }


        [ButtonPointer(5, "ButtonPointer")]
        public void ButtonPointer()
        {

        }

        [Slider(6, "MusicSlider", "Music", MinValue = 0f, MaxValue = 1f)]
        [Description("Change a music volume")]
        public float MasterVolume
        {
            get { return soundSystem.MasterVolume; }
            set { soundSystem.MasterVolume = value; }
        }

        [Slider(7, "EffectsSlider", "Effects", MinValue = 0f, MaxValue = 1f)]
        [Description("Change a effects volume")]
        public float EffectsVolume
        {
            get;
            set;
        }

        [Slider(8, "SpeechSlider", "Speech", MinValue = 0f, MaxValue = 1f)]
        [Description("Change a speech volume")]
        public float SpeechVolume
        {
            get;
            set;
        }

#if EDITOR
        [NavigationButton(9999, "Option", "Editor")]
        [Description("Warning:: Creators mode")]
        public void Editor_Click()
        {
            game.Invoker.PushAndExecute("map testMap2 /edit");
        }
#endif
    }
}
