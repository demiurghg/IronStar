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
    public class GameMenuPageOptions : IPageOption
    {
        private readonly Game game;
        private readonly ShooterInterface ui;
        public GameMenuPageOptions(Game game)
        {
            this.ui = (ShooterInterface)game.UserInterface.Instance;
            this.game = game;
        }


        [Background(0, "Background", @"ui\background")]
        public void Background() { }

        [NavigationButton(1, "NewGameButton", "CONTINUE")]
        [Description("Continue...")]
        public void NewGame_Click()
        {
            ui.ShowMenu = false;  
        }


        [NavigationButton(2, "OptionButton", "OPTIONS")]
        [Description("TODO: write a definition in MainMenuOptions.cs")]
        public void Options_Click()
        {
            ui.SetActiveMenu("SettingsMenu");
        }

        [NavigationButton(3, "ExtraButton", "EXTRAS")]
        [Description("TODO: write a definition in MainMenuOptions.cs")]
        public void Extra_Click()
        {

        }

        [NavigationButton(4, "ExitButton", "QUIT")]
        [Description("Close a game")]
        public void Exit_Click()
        {
            ui.RequestToExit();
        }

        
        [DescriptionLabel(5, "Description", "Select[ENTER]   BACK[ESC]")]
        public void DescriptionLabel()
        {

        }


        [ButtonPointer(6, "ButtonPointer")]
        public void ButtonPointer()
        {

        }
    }
}
