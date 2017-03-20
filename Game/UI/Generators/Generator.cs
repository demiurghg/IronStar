using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fusion.Engine.Frames;
using Fusion.Engine.Common;
using IronStar.UI.Controls;
using System.Reflection;
using IronStar.UI.Attributes;
using Fusion.Engine.Graphics;
using Fusion.Core.Mathematics;

namespace IronStar.UI.Generators
{
    public partial class MenuGenerator : IMenuGenerator
    {
        public class GeneratorContext
        {
            public ControlAttribute ControlAttribute;
            public DescriptionAttribute DecsriptionAttribute;
            public MemberInfo MemberInfo;
            public IPageOption PageOption;
            public GeneratorContext(IPageOption pageOption, ControlAttribute ca, DescriptionAttribute decsription, MemberInfo methodInfo)
            {
                ControlAttribute = ca;
                DecsriptionAttribute = decsription;
                MemberInfo = methodInfo;
                this.PageOption = pageOption;
            }
        }

        private readonly Game game;
        delegate void GeneratorAction(FrameProcessor fp, Page page, GeneratorContext context);
        Dictionary<Type, GeneratorAction> actions = new Dictionary<Type, GeneratorAction>();

        public MenuGenerator(Game game)
        {
            this.game = game;

            this.GetType().GetMethods().Where(w => w.GetCustomAttribute<GeneratorApplicabilityAttribute>() != null).ToList().ForEach(
                m =>
                {
                    var t = m.GetCustomAttribute<GeneratorApplicabilityAttribute>().Types;
                    foreach (var type in t)
                    {
                        actions[type] = (GeneratorAction)Delegate.CreateDelegate(typeof(GeneratorAction), m);
                    }
                }
            );
        }

        private void Reset()
        {
            MainMenuInfo.currentPosition = 0;
            SettingsMenuInfo.currentPosition = 0;
        }

        public Page CreateMenu(string name, IPageOption pageOption)
        {
            Reset();
            var page = new Page(game.Frames);
            page.Name = name;
            page.Y = 0;
            page.X = 0;
            page.Width = game.RenderSystem.DisplayBounds.Width;
            page.Height = game.RenderSystem.DisplayBounds.Height;

            var members = pageOption.GetType().GetMembers().Where(t => t.GetCustomAttribute<ControlAttribute>(true) != null).ToList();
            members.Sort(Comparer<MemberInfo>.Create(
                (a, b) => 
                {
                    return a.GetCustomAttribute<ControlAttribute>().Order.CompareTo(b.GetCustomAttribute<ControlAttribute>().Order);
                }));

            foreach (var m in members)
            {
                var ca = m.GetCustomAttribute<ControlAttribute>(true);
                GeneratorContext gc = new GeneratorContext(pageOption, ca, m.GetCustomAttribute<DescriptionAttribute>(), m);
                if (actions.ContainsKey(ca.GetType()))
                {
                    actions[ca.GetType()]?.Invoke(game.Frames, page, gc);
                } else
                {
                    foreach (var i in ca.GetType().GetInterfaces())
                    {
                        if (actions.ContainsKey(i))
                        {
                            actions[i]?.Invoke(game.Frames, page, gc);
                            break;
                        }
                    }
                }
            }


            ///Set a description label
            DescriptionLabel dl = null;
            foreach (var frame in page.Children)
            {
                if (frame is DescriptionLabel)
                {
                    dl = (DescriptionLabel)frame;
                    break;
                }
            }


            if (dl != null)
            {
                Dictionary<string, Frame> help = page.Children.ToDictionary(t => t.Name, t => t);
                foreach (var m in members)
                {
                    var description = m.GetCustomAttribute<DescriptionAttribute>();
                    if (description != null)
                    {
                        var ca = m.GetCustomAttribute<ControlAttribute>();
                        help[ca.Name].MouseIn += (s, e) =>
                        {
                            dl.AdditionalDescription = description.Decsription;
                        };
                    }
                }
            }

            ButtonPointer bp = null;
            foreach (var frame in page.Children)
            {
                if (frame is ButtonPointer)
                {
                    bp = (ButtonPointer)frame;
                    break;
                }
            }
            if (bp != null) {
                foreach (var frame in page.Children)
                {
                    if (frame is NavigationButton)
                    {
                        var nb = frame as NavigationButton;
                        nb.MouseIn += (s, e) =>
                        {
                            if (!bp.IsAppeared)
                            {
                                bp.Y = nb.Y;
                                bp.X = ParseLocation(ButtonPointerInfo.LeftOffset, page.Width, 0);
                                bp.RunTransition("Width", ParseBounds(ButtonPointerInfo.Width, page.Width), 0, bp.AppearedTime);
                                bp.RunTransition("Height", nb.Height, 0, bp.AppearedTime);
                                bp.IsAppeared = true;
                            }
                            else
                            {
                                bp.RunTransition("Y", nb.Y, 0, bp.MoveTime);
                            }
                        };
                    }
                }
            }
            return page;
        }


        [GeneratorApplicability(typeof(LogoAttribute))]
        public static void GenerateLogo(FrameProcessor fp, Page page, GeneratorContext context)
        {
            Image image = new Image(fp);
            image.Name = context.ControlAttribute.Name;
            image.Image = fp.Game.Content.Load<DiscTexture>(StartMenuInfo.LogoTexture);
            image.ImageMode = FrameImageMode.Stretched;

            int width = ParseBounds(StartMenuInfo.LogoWidth, page.Width);
            image.Width = width;
            image.Height = (int)((float)image.Width / (float)image.Image.Width * image.Image.Height);

            Size2 pos = GetLocation(StartMenuInfo.LogoPosition, page.Width, image.Width, page.Height, image.Height);

            image.X = pos.Width;
            image.Y = pos.Height;

            page.Add(image);
        }

        [GeneratorApplicability(typeof(BackgroundAttribute))]
        public static void GenerateBackground(FrameProcessor fp, Page page, GeneratorContext context)
        {
            Image image = new Image(fp);
            image.Name = context.ControlAttribute.Name;

            image.Image = fp.Game.Content.Load<DiscTexture>(StartMenuInfo.BackgroundTexture);
            image.Width = page.Width;

            image.X = 0;
            image.Y = 0;

            image.Height = page.Height;
            image.ImageMode = FrameImageMode.Stretched;

            page.Add(image);
        }

        [GeneratorApplicability(typeof(StartLabelAttribute))]
        public static void GenerateStartLabel(FrameProcessor fp, Page page, GeneratorContext context)
        {
            StartLabel label = new StartLabel(fp, context.ControlAttribute.Name);

            label.Text = ((StartLabelAttribute)context.ControlAttribute).Text;
            int width = ParseBounds(StartMenuInfo.LabelWidth, page.Width);
            label.Font = page.Game.Content.Load<SpriteFont>($@"fonts\\{MainFont}{GetMainFontSize(page.Width, page.Height)}");

            var r = label.Font.MeasureString(label.Text);
            label.Width = r.Width;
            label.Height = r.Height;

            Size2 pos = GetLocation(StartMenuInfo.LabelPosition, page.Width, label.Width, page.Height, label.Height);

            label.X = pos.Width;
            label.Y = pos.Height;

            page.Add(label);
        }


        [GeneratorApplicability(typeof(NavigationButtonAttribute))]
        public static void GenerateNavigationButton(FrameProcessor fp, Page page, GeneratorContext context)
        {
            NavigationButton button = new NavigationButton(fp);
            button.HoverColor = Color.White;
            button.ClickColor = new Color(255, 255, 255, 160);
            button.DefaultBackColor = Color.White;

            button.Name = context.ControlAttribute.Name;

            button.Click += (s, e) => {
                ((MethodInfo)context.MemberInfo)?.Invoke(context.PageOption, null);
            };

            NavigationButtonAttribute atr = (NavigationButtonAttribute)context.ControlAttribute;

            button.Text = atr.Text;


            button.Font = page.Game.Content.Load<SpriteFont>($@"fonts\\{MainFont}{GetMainFontSize(page.Width, page.Height)}");

            var r = button.Font.MeasureString(button.Text);

            button.Width = ParseBounds(MainMenuInfo.ButtonWidth, page.Width);
            button.Height = ParseBounds(MainMenuInfo.ButtonHeight, page.Height);

            button.TextAlignment = Alignment.MiddleLeft;

            button.Y = ParseLocation(MainMenuInfo.TopOffset, page.Height, button.Height) + (int)MainMenuInfo.currentPosition;
            MainMenuInfo.currentPosition += button.Height;
            button.X = ParseLocation(MainMenuInfo.LeftOffset, page.Width, button.Width);

             

            page.Add(button);
        }

        [GeneratorApplicability(typeof(DescriptionLabelAttribute))]
        public static void GenerateDescriptionLabel(FrameProcessor fp, Page page, GeneratorContext context)
        {
            var font = page.Game.Content.Load<SpriteFont>($@"fonts\\{AdditionalFont}{GetAdditionalFontSize(page.Width, page.Height)}");

            DescriptionLabel label = new DescriptionLabel(fp, font,((DescriptionLabelAttribute)context.ControlAttribute).Text);
            label.Name = context.ControlAttribute.Name;
            label.ForeColor = new Color(128, 128, 128, 255);

            int width = ParseBounds("25%", page.Width);

            var pos = GetLocation(MainMenuInfo.DescriptionPosition, page.Width, label.Width, page.Height, label.Height);
            label.X = pos.Width;
            label.Y = pos.Height;
            page.Add(label);
        }

        [GeneratorApplicability(typeof(ButtonPointerAttribute))]
        public static void GenerateButtonPointer(FrameProcessor fp, Page page, GeneratorContext context)
        {
            ButtonPointer frame = new ButtonPointer(fp, "ButtonPointer", Color.White, 300, 300);
            page.Add(frame);
        }


        [GeneratorApplicability(typeof(ISettingsControlAttribute))]
        public static void GenerateSettingsControl(FrameProcessor fp, Page page, GeneratorContext context)
        {
            var font = page.Game.Content.Load<SpriteFont>($@"fonts\\{AdditionalFont}{GetMainFontSize(page.Width, page.Height)}");
            var frame = ((ISettingsControlAttribute)context.ControlAttribute).GetFrame(fp, context.ControlAttribute.Name, font);
            frame.Width = ParseBounds(SettingsMenuInfo.Width, page.Width);
            frame.Height = ParseBounds(SettingsMenuInfo.Height, page.Height);
            //Console.WriteLine($"Height{frame.Height}");

            frame.X = ParseLocation(SettingsMenuInfo.LeftOffset, page.Width, frame.Width);
            frame.Y = (int)(SettingsMenuInfo.currentPosition + ParseLocation(SettingsMenuInfo.TopOffset, page.Height, frame.Height));


            if (frame is IValuableControl)
            {
                var mb = (PropertyInfo)context.MemberInfo;
                frame.GetType().GetProperty("Value").SetValue(frame, mb.GetValue(context.PageOption));
                ((IValuableControl)frame).ValueChanged += (s, e) => {
                    mb.SetValue(context.PageOption, ((IValuableControl)frame).Value);
                };
            }

            frame.MouseIn += (s, e) => frame.RunTransition("BackColor", new Color(255, 255, 255, 60), 0, 300);
            frame.MouseOut += (s, e) => frame.RunTransition("BackColor", new Color(255, 255, 255, 0), 0, 300);

            SettingsMenuInfo.currentPosition += frame.Height;

            page.Add(frame);
        }

    }
}
