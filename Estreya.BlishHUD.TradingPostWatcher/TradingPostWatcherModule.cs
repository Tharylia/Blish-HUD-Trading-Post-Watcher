﻿namespace Estreya.BlishHUD.TradingPostWatcher
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Modules;
    using Blish_HUD.Modules.Managers;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.Utils;
    using Estreya.BlishHUD.TradingPostWatcher.Controls;
    using Estreya.BlishHUD.TradingPostWatcher.Models;
    using Estreya.BlishHUD.TradingPostWatcher.Resources;
    using Estreya.BlishHUD.TradingPostWatcher.State;
    using Gw2Sharp.Models;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    [Export(typeof(Blish_HUD.Modules.Module))]
    public class TradingPostWatcherModule : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<TradingPostWatcherModule>();

        public const string WEBSITE_ROOT_URL = "https://blishhud.estreya.de";
        public const string WEBSITE_FILE_ROOT_URL = "https://files.blishhud.estreya.de";
        public const string WEBSITE_MODULE_URL = $"{WEBSITE_ROOT_URL}/modules/trading-post-watcher";

        internal static TradingPostWatcherModule ModuleInstance;

        public bool IsPrerelease => !string.IsNullOrWhiteSpace(this.Version?.PreRelease);

        private WebClient _webclient;

        private TradingPostWatcherDrawer Drawer { get; set; }

        #region Service Managers
        internal SettingsManager SettingsManager => this.ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => this.ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => this.ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => this.ModuleParameters.Gw2ApiManager;
        #endregion

        internal ModuleSettings ModuleSettings { get; private set; }

        private CornerIcon CornerIcon { get; set; }

        internal TabbedWindow2 SettingsWindow { get; private set; }

        internal bool Debug => this.ModuleSettings.DebugEnabled.Value;

        private BitmapFont _font;

        internal BitmapFont Font
        {
            get
            {
                if (this._font == null)
                {
                    this._font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, this.ModuleSettings.FontSize.Value, ContentService.FontStyle.Regular);
                }

                return this._font;
            }
        }

        internal DateTime DateTimeNow => DateTime.Now;

        #region States
        private readonly AsyncLock _stateLock = new AsyncLock();
        internal Collection<ManagedState> States { get; set; } = new Collection<ManagedState>();

        public IconState IconState { get; private set; }
        public TradingPostState TradingPostState { get; private set; }
        #endregion

        [ImportingConstructor]
        public TradingPostWatcherModule([Import("ModuleParameters")] ModuleParameters moduleParameters) : base(moduleParameters)
        {
            ModuleInstance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            this.ModuleSettings = new ModuleSettings(settings);
        }

        protected override void Initialize()
        {
            this.Drawer = new TradingPostWatcherDrawer()
            {
                Parent = GameService.Graphics.SpriteScreen,
                Opacity = 0f,
                Visible = false,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                HeightSizingMode = SizingMode.AutoSize,
            };

            GameService.Overlay.UserLocaleChanged += (s, e) =>
            {
            };
        }

        protected override async Task LoadAsync()
        {
            Logger.Debug("Load module settings.");
            await this.ModuleSettings.LoadAsync();

            Logger.Debug("Initialize states");
            await this.InitializeStates();

            await this.Drawer.LoadAsync();

            this.ModuleSettings.ModuleSettingsChanged += (sender, eventArgs) =>
            {
                switch (eventArgs.Name)
                {
                    case nameof(this.ModuleSettings.Width):
                        this.Drawer.UpdateSize(this.ModuleSettings.Width.Value, -1);
                        break;
                    case nameof(this.ModuleSettings.GlobalEnabled):
                        this.ToggleContainer(this.ModuleSettings.GlobalEnabled.Value);
                        break;
                    case nameof(this.ModuleSettings.FontSize):
                        this._font = null;
                        break;
                    case nameof(this.ModuleSettings.RegisterCornerIcon):
                        this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);
                        break;
                    case nameof(this.ModuleSettings.BackgroundColor):
                    case nameof(this.ModuleSettings.BackgroundColorOpacity):
                        this.Drawer.UpdateBackgroundColor();
                        break;
                    case nameof(this.ModuleSettings.MaxTransactions):
                    case nameof(this.ModuleSettings.ShowBuyTransactions):
                    case nameof(this.ModuleSettings.ShowSellTransactions):
                    case nameof(this.ModuleSettings.ShowHighestTransactions):
                        this.AddTransactions();
                        break;
                    default:
                        break;
                }
            };

        }

        private void TradingPostState_TransactionsUpdated(object sender, EventArgs e)
        {
            this.AddTransactions();
        }

        private void AddTransactions()
        {
            this.Drawer.ClearChildren();

            var filteredTransactions = this.TradingPostState.Transactions.Where(transaction =>
            {
                if (!this.ModuleSettings.ShowBuyTransactions.Value && transaction.Type == CommerceTransaction.TransactionType.Buy)
                {
                    return false;
                }

                if (!this.ModuleSettings.ShowSellTransactions.Value && transaction.Type == CommerceTransaction.TransactionType.Sell)
                {
                    return false;
                }

                if (!this.ModuleSettings.ShowHighestTransactions.Value && transaction.IsHighest)
                {
                    return false;
                }

                return true;
            });

            foreach (CommerceTransaction transaction in filteredTransactions.Take(this.ModuleSettings.MaxTransactions.Value))
            {
                new Transaction(transaction, 
                    () => this.ModuleSettings.Opacity.Value, 
                    () => this.ModuleSettings.ShowPrice.Value, 
                    () => this.ModuleSettings.ShowPriceAsTotal.Value, 
                    () => this.ModuleSettings.ShowRemaining.Value, 
                    () => this.ModuleSettings.ShowCreated.Value)
                {
                    Parent = this.Drawer,
                    HeightSizingMode = SizingMode.AutoSize,
                    WidthSizingMode = SizingMode.Fill
                };
            }
        }

        private async Task InitializeStates()
        {
            string directory = this.DirectoriesManager.GetFullDirectoryPath("tradingPost");

            using (await this._stateLock.LockAsync())
            {
                this.IconState = new IconState(this.ContentsManager, directory);
                this.TradingPostState = new TradingPostState(this.Gw2ApiManager);
                this.TradingPostState.TransactionsUpdated += this.TradingPostState_TransactionsUpdated;

                this.States.Add(this.IconState);
                this.States.Add(this.TradingPostState);

                // Only start states not already running
                foreach (ManagedState state in this.States.Where(state => !state.Running))
                {
                    try
                    {
                        // Order is important
                        if (state.AwaitLoad)
                        {
                            await state.Start();
                        }
                        else
                        {
                            _ = state.Start().ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    Logger.Error(task.Exception, "Not awaited state start failed for \"{0}\"", state.GetType().Name);
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed starting state \"{0}\"", state.GetType().Name);
                    }
                }
            }
        }


        private void HandleCornerIcon(bool show)
        {
            if (show)
            {
                this.CornerIcon = new CornerIcon()
                {
                    IconName = "Trading Post Watcher",
                    Icon = this.ContentsManager.GetTexture(@"images\tradingpost.png"),
                };

                this.CornerIcon.Click += (s, ea) =>
                {
                    this.SettingsWindow.ToggleWindow();
                };
            }
            else
            {
                if (this.CornerIcon != null)
                {
                    this.CornerIcon.Dispose();
                    this.CornerIcon = null;
                }
            }
        }

        private void ToggleContainer(bool show)
        {
            if (this.Drawer == null)
            {
                return;
            }

            if (!this.ModuleSettings.GlobalEnabled.Value)
            {
                if (this.Drawer.Visible)
                {
                    this.Drawer.Hide();
                }

                return;
            }

            if (show)
            {
                if (!this.Drawer.Visible)
                {
                    this.Drawer.Show();
                }
            }
            else
            {
                if (this.Drawer.Visible)
                {
                    this.Drawer.Hide();
                }
            }
        }

        public override IView GetSettingsView()
        {
            Shared.UI.Views.ModuleSettingsView view = new Shared.UI.Views.ModuleSettingsView(Strings.SettingsView_OpenSettings);
            view.OpenClicked += (s, e) => this.SettingsWindow.ToggleWindow();

            return view;
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            // Base handler must be called
            base.OnModuleLoaded(e);

            this.Drawer.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value);
            this.Drawer.UpdateSize(this.ModuleSettings.Width.Value, -1);

            //this.ManageEventTab = GameService.Overlay.BlishHudWindow.AddTab("Event Table", this.ContentsManager.GetIcon(@"images\event_boss.png"), () => new UI.Views.ManageEventsView(this._eventCategories, this.ModuleSettings.AllEvents));

            Logger.Debug("Start building settings window.");

            Texture2D windowBackground = this.IconState.GetIcon(@"images\502049.png", false);

            Rectangle settingsWindowSize = new Rectangle(35, 26, 1100, 714);
            int contentRegionPaddingY = settingsWindowSize.Y - 15;
            int contentRegionPaddingX = settingsWindowSize.X + 46;
            Rectangle contentRegion = new Rectangle(contentRegionPaddingX, contentRegionPaddingY, settingsWindowSize.Width - 52, settingsWindowSize.Height - contentRegionPaddingY);

            this.SettingsWindow = new TabbedWindow2(windowBackground, settingsWindowSize, contentRegion)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Title = Strings.SettingsWindow_Title,
                Emblem = this.IconState.GetIcon(@"images\tradingpost.png"),
                Subtitle = Strings.SettingsWindow_Subtitle,
                SavesPosition = true,
                Id = $"{nameof(TradingPostWatcherModule)}_6bd04be4-dc19-4914-a2c3-8160ce76818b"
            };

            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"156736"), () => new UI.Views.Settings.GeneralSettingsView() { APIManager = this.Gw2ApiManager, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GeneralSettings_Title));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\tradingpost.png"), () => new UI.Views.Settings.TransactionSettingsView() { APIManager = this.Gw2ApiManager, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Transactions"));
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"images\graphics_settings.png"), () => new UI.Views.Settings.GraphicsSettingsView() { APIManager = this.Gw2ApiManager, DefaultColor = this.ModuleSettings.DefaultGW2Color }, Strings.SettingsWindow_GraphicSettings_Title));
#if DEBUG
            this.SettingsWindow.Tabs.Add(new Tab(this.IconState.GetIcon(@"155052"), () => new UI.Views.Settings.DebugSettingsView() { APIManager = this.Gw2ApiManager, DefaultColor = this.ModuleSettings.DefaultGW2Color }, "Debug"));
#endif

            Logger.Debug("Finished building settings window.");

            this.HandleCornerIcon(this.ModuleSettings.RegisterCornerIcon.Value);

            if (this.ModuleSettings.GlobalEnabled.Value)
            {
                this.ToggleContainer(true);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            this.CheckMumble();
            this.Drawer.UpdatePosition(this.ModuleSettings.LocationX.Value, this.ModuleSettings.LocationY.Value); // Handle windows resize

            this.CheckContainerSizeAndPosition();

            using (this._stateLock.Lock())
            {
                foreach (ManagedState state in this.States)
                {
                    state.Update(gameTime);
                }
            }

        }

        private void CheckContainerSizeAndPosition()
        {
            bool buildFromBottom = this.ModuleSettings.BuildDirection.Value == BuildDirection.Bottom;
            int maxResX = (int)(GameService.Graphics.Resolution.X / GameService.Graphics.UIScaleMultiplier);
            int maxResY = (int)(GameService.Graphics.Resolution.Y / GameService.Graphics.UIScaleMultiplier);

            int minLocationX = 0;
            int maxLocationX = maxResX - this.Drawer.Width;
            int minLocationY = buildFromBottom ? this.Drawer.Height : 0;
            int maxLocationY = buildFromBottom ? maxResY : maxResY - this.Drawer.Height;
            int minWidth = 0;
            int maxWidth = maxResX - this.ModuleSettings.LocationX.Value;

            this.ModuleSettings.LocationX.SetRange(minLocationX, maxLocationX);
            this.ModuleSettings.LocationY.SetRange(minLocationY, maxLocationY);
            this.ModuleSettings.Width.SetRange(minWidth, maxWidth);

            /*
            if (this.ModuleSettings.LocationX.Value < minLocationX)
            {
                Logger.Debug($"LocationX unter min, set to: {minLocationX}");
                this.ModuleSettings.LocationX.Value = minLocationX;
            }

            if (this.ModuleSettings.LocationX.Value > maxLocationX)
            {
                Logger.Debug($"LocationX over max, set to: {maxLocationX}");
                this.ModuleSettings.LocationX.Value = maxLocationX;
            }

            if (this.ModuleSettings.LocationY.Value < minLocationY)
            {
                Logger.Debug($"LocationY unter min, set to: {minLocationY}");
                this.ModuleSettings.LocationY.Value = minLocationY;
            }

            if (this.ModuleSettings.LocationY.Value > maxLocationY)
            {
                Logger.Debug($"LocationY over max, set to: {maxLocationY}");
                this.ModuleSettings.LocationY.Value = maxLocationY;
            }

            if (this.ModuleSettings.Width.Value < minWidth)
            {
                Logger.Debug($"Width under min, set to: {minWidth}");
                this.ModuleSettings.Width.Value = minWidth;
            }

            if (this.ModuleSettings.Width.Value > maxWidth)
            {
                Logger.Debug($"Width over max, set to: {maxWidth}");
                this.ModuleSettings.Width.Value = maxWidth;
            }
            */
        }

        private void CheckMumble()
        {
            if (GameService.Gw2Mumble.IsAvailable)
            {
                if (this.Drawer != null)
                {
                    bool show = true;

                    if (this.ModuleSettings.HideOnOpenMap.Value)
                    {
                        show &= !GameService.Gw2Mumble.UI.IsMapOpen;
                    }

                    if (this.ModuleSettings.HideOnMissingMumbleTicks.Value)
                    {
                        show &= GameService.Gw2Mumble.TimeSinceTick.TotalSeconds < 0.5;
                    }

                    if (this.ModuleSettings.HideInCombat.Value)
                    {
                        show &= !GameService.Gw2Mumble.PlayerCharacter.IsInCombat;
                    }

                    if (this.ModuleSettings.HideInWvW.Value)
                    {
                        MapType[] wvwMapTypes = new[] { MapType.EternalBattlegrounds, MapType.GreenBorderlands, MapType.RedBorderlands, MapType.BlueBorderlands, MapType.EdgeOfTheMists };

                        show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && wvwMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
                    }

                    if (this.ModuleSettings.HideInPvP.Value)
                    {
                        MapType[] pvpMapTypes = new[] { MapType.Pvp, MapType.Tournament };

                        show &= !(GameService.Gw2Mumble.CurrentMap.IsCompetitiveMode && pvpMapTypes.Any(type => type == GameService.Gw2Mumble.CurrentMap.Type));
                    }

                    //show &= GameService.Gw2Mumble.CurrentMap.Type != MapType.CharacterCreate;

                    this.ToggleContainer(show);
                }
            }
        }

        public WebClient GetWebClient()
        {
            if (this._webclient == null)
            {
                this._webclient = new WebClient();

                this._webclient.Headers.Add("user-agent", $"Trading Post Watcher {this.Version}");
            }

            return this._webclient;
        }

        /// <inheritdoc />
        protected override void Unload()
        {
            Logger.Debug("Unload module.");

            Logger.Debug("Unload base.");

            base.Unload();


            Logger.Debug("Unload drawer.");

            if (this.Drawer != null)
            {
                this.Drawer.Dispose();
            }

            Logger.Debug("Unloaded drawer.");

            Logger.Debug("Unload settings window.");

            if (this.SettingsWindow != null)
            {
                this.SettingsWindow.Hide();
            }

            Logger.Debug("Unloaded settings window.");

            Logger.Debug("Unload corner icon.");

            this.HandleCornerIcon(false);

            Logger.Debug("Unloaded corner icon.");

            Logger.Debug("Unloading states...");
            this.TradingPostState.TransactionsUpdated -= this.TradingPostState_TransactionsUpdated;

            using (this._stateLock.Lock())
            {
                this.States.ToList().ForEach(state => state.Dispose());
            }

            Logger.Debug("Finished unloading states.");
        }

        internal async Task ReloadStates()
        {
            using (await this._stateLock.LockAsync())
            {
                await Task.WhenAll(this.States.Select(state => state.Reload()));
            }
        }

        internal async Task ClearStates()
        {
            using (await this._stateLock.LockAsync())
            {
                await Task.WhenAll(this.States.Select(state => state.Clear()));
            }
        }
    }
}

