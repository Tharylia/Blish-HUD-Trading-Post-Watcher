namespace Estreya.BlishHUD.TradingPostWatcher
{
    using Blish_HUD;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.TradingPostWatcher.Models;
    using Estreya.BlishHUD.TradingPostWatcher.Resources;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using static Blish_HUD.ContentService;

    public class ModuleSettings
    {
        private static readonly Logger Logger = Logger.GetLogger<ModuleSettings>();

        private Gw2Sharp.WebApi.V2.Models.Color _defaultColor;
        public Gw2Sharp.WebApi.V2.Models.Color DefaultGW2Color { get => this._defaultColor; private set => this._defaultColor = value; }

        public event EventHandler<ModuleSettingsChangedEventArgs> ModuleSettingsChanged;

        private SettingCollection Settings { get; set; }

        #region Global Settings
        private const string GLOBAL_SETTINGS = "global-settings";
        public SettingCollection GlobalSettings { get; private set; }
        public SettingEntry<bool> GlobalEnabled { get; private set; }
        public SettingEntry<KeyBinding> GlobalEnabledHotkey { get; private set; }
        public SettingEntry<bool> RegisterCornerIcon { get; private set; }
        public SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> BackgroundColor { get; private set; }
        public SettingEntry<float> BackgroundColorOpacity { get; private set; }
        public SettingEntry<bool> HideOnMissingMumbleTicks { get; private set; }
        public SettingEntry<bool> HideInCombat { get; private set; }
        public SettingEntry<bool> HideOnOpenMap { get; private set; }
        public SettingEntry<bool> HideInWvW { get; private set; }
        public SettingEntry<bool> HideInPvP { get; private set; }
        public SettingEntry<bool> DebugEnabled { get; private set; }
        public SettingEntry<BuildDirection> BuildDirection { get; private set; }
        public SettingEntry<float> Opacity { get; private set; }
        public SettingEntry<FontSize> FontSize { get; private set; }
        #endregion

        #region Transactions
        private const string TRANSACTION_SETTINGS = "transaction-settings";
        public SettingCollection TransactionSettings { get; private set; }
        public SettingEntry<int> MaxTransactions { get; private set; }
        public SettingEntry<bool> ShowBuyTransactions { get; private set; }
        public SettingEntry<bool> ShowSellTransactions { get; private set; }
        public SettingEntry<bool> ShowHighestTransactions { get; private set; }
        public SettingEntry<bool> ShowPrice { get; private set; }
        public SettingEntry<bool> ShowPriceAsTotal { get; private set; }
        public SettingEntry<bool> ShowRemaining { get; private set; }
        public SettingEntry<bool> ShowCreated { get; private set; }
        #endregion

        #region Location
        private const string LOCATION_SETTINGS = "location-settings";
        public SettingCollection LocationSettings { get; private set; }
        public SettingEntry<int> LocationX { get; private set; }
        public SettingEntry<int> LocationY { get; private set; }
        public SettingEntry<int> Width { get; private set; }
        #endregion

        public ModuleSettings(SettingCollection settings)
        {
            this.Settings = settings;

            this.BuildDefaultColor();

            this.InitializeGlobalSettings(settings);
            this.InitializeLocationSettings(settings);
            this.InitializeTransactionSettings(settings);

        }

        private void BuildDefaultColor()
        {
            this._defaultColor = new Gw2Sharp.WebApi.V2.Models.Color()
            {
                Name = "Dye Remover",
                Id = 1,
                BaseRgb = new List<int>() { 128, 26, 26 },
                Cloth = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = 15,
                    Contrast = 1.25,
                    Hue = 38,
                    Saturation = 0.28125,
                    Lightness = 1.44531,
                    Rgb = new List<int>() { 124, 108, 83 }
                },
                Leather = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = -8,
                    Contrast = 1.0,
                    Hue = 34,
                    Saturation = 0.3125,
                    Lightness = 1.09375,
                    Rgb = new List<int>() { 65, 49, 29 }
                },
                Metal = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = 5,
                    Contrast = 1.05469,
                    Hue = 38,
                    Saturation = 0.101563,
                    Lightness = 1.36719,
                    Rgb = new List<int>() { 96, 91, 83 }
                },
                Fur = new Gw2Sharp.WebApi.V2.Models.ColorMaterial()
                {
                    Brightness = 15,
                    Contrast = 1.25,
                    Hue = 38,
                    Saturation = 0.28125,
                    Lightness = 1.44531,
                    Rgb = new List<int>() { 124, 108, 83 }
                },
            };
        }

        public async Task LoadAsync()
        {
            try
            {
                this.DefaultGW2Color = await TradingPostWatcherModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.GetAsync(1);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Could not load default gw2 color: {ex.Message}");
            }
        }

        private void InitializeGlobalSettings(SettingCollection settings)
        {
            this.GlobalSettings = settings.AddSubCollection(GLOBAL_SETTINGS);

            this.GlobalEnabled = this.GlobalSettings.DefineSetting(nameof(this.GlobalEnabled), true, () => Strings.Setting_GlobalEnabled_Name, () => Strings.Setting_GlobalEnabled_Description);
            this.GlobalEnabled.SettingChanged += this.SettingChanged;

            this.GlobalEnabledHotkey = this.GlobalSettings.DefineSetting(nameof(this.GlobalEnabledHotkey), new KeyBinding(Microsoft.Xna.Framework.Input.ModifierKeys.Alt, Microsoft.Xna.Framework.Input.Keys.T), () => Strings.Setting_GlobalEnabledHotkey_Name, () => Strings.Setting_GlobalEnabledHotkey_Description);
            this.GlobalEnabledHotkey.SettingChanged += this.SettingChanged;
            this.GlobalEnabledHotkey.Value.Enabled = true;
            this.GlobalEnabledHotkey.Value.Activated += (s, e) => this.GlobalEnabled.Value = !this.GlobalEnabled.Value;
            this.GlobalEnabledHotkey.Value.BlockSequenceFromGw2 = true;

            this.RegisterCornerIcon = this.GlobalSettings.DefineSetting(nameof(this.RegisterCornerIcon), true, () => Strings.Setting_RegisterCornerIcon_Name, () => Strings.Setting_RegisterCornerIcon_Description);
            this.RegisterCornerIcon.SettingChanged += this.SettingChanged;

            this.HideOnOpenMap = this.GlobalSettings.DefineSetting(nameof(this.HideOnOpenMap), true, () => Strings.Setting_HideOnMap_Name, () => Strings.Setting_HideOnMap_Description);
            this.HideOnOpenMap.SettingChanged += this.SettingChanged;

            this.HideOnMissingMumbleTicks = this.GlobalSettings.DefineSetting(nameof(this.HideOnMissingMumbleTicks), true, () => Strings.Setting_HideOnMissingMumbleTicks_Name, () => Strings.Setting_HideOnMissingMumbleTicks_Description);
            this.HideOnMissingMumbleTicks.SettingChanged += this.SettingChanged;

            this.HideInCombat = this.GlobalSettings.DefineSetting(nameof(this.HideInCombat), false, () => Strings.Setting_HideInCombat_Name, () => Strings.Setting_HideInCombat_Description);
            this.HideInCombat.SettingChanged += this.SettingChanged;

            this.HideInWvW = this.GlobalSettings.DefineSetting(nameof(this.HideInWvW), false, () => "Hide in WvW", () => "Whether the event table should hide when in world vs. world.");
            this.HideInWvW.SettingChanged += this.SettingChanged;

            this.HideInPvP = this.GlobalSettings.DefineSetting(nameof(this.HideInPvP), false, () => "Hide in PvP", () => "Whether the event table should hide when in player vs. player.");
            this.HideInPvP.SettingChanged += this.SettingChanged;

            this.BackgroundColor = this.GlobalSettings.DefineSetting(nameof(this.BackgroundColor), this.DefaultGW2Color, () => Strings.Setting_BackgroundColor_Name, () => Strings.Setting_BackgroundColor_Description);
            this.BackgroundColor.SettingChanged += this.SettingChanged;

            this.BackgroundColorOpacity = this.GlobalSettings.DefineSetting(nameof(this.BackgroundColorOpacity), 0.0f, () => Strings.Setting_BackgroundColorOpacity_Name, () => Strings.Setting_BackgroundColorOpacity_Description);
            this.BackgroundColorOpacity.SetRange(0.0f, 1f);
            this.BackgroundColorOpacity.SettingChanged += this.SettingChanged;

            this.DebugEnabled = this.GlobalSettings.DefineSetting(nameof(this.DebugEnabled), false, () => Strings.Setting_DebugEnabled_Name, () => Strings.Setting_DebugEnabled_Description);
            this.DebugEnabled.SettingChanged += this.SettingChanged;

            this.BuildDirection = this.GlobalSettings.DefineSetting(nameof(this.BuildDirection), TradingPostWatcher.Models.BuildDirection.Top, () => Strings.Setting_BuildDirection_Name, () => Strings.Setting_BuildDirection_Description);
            this.BuildDirection.SettingChanged += this.SettingChanged;

            this.Opacity = this.GlobalSettings.DefineSetting(nameof(this.Opacity), 1f, () => Strings.Setting_Opacity_Name, () => Strings.Setting_Opacity_Description);
            this.Opacity.SetRange(0.1f, 1f);
            this.Opacity.SettingChanged += this.SettingChanged;

            this.FontSize = this.GlobalSettings.DefineSetting(nameof(this.FontSize), ContentService.FontSize.Size16, () => Strings.Setting_FontSize_Name, () => Strings.Setting_FontSize_Description);
            this.FontSize.SettingChanged += this.SettingChanged;
        }

        private void InitializeLocationSettings(SettingCollection settings)
        {
            this.LocationSettings = settings.AddSubCollection(LOCATION_SETTINGS);

            int height = 1080;
            int width = 1920;

            this.LocationX = this.LocationSettings.DefineSetting(nameof(this.LocationX), (int)(width * 0.1), () => Strings.Setting_LocationX_Name, () => Strings.Setting_LocationX_Description);
            this.LocationX.SetRange(0, width);
            this.LocationX.SettingChanged += this.SettingChanged;

            this.LocationY = this.LocationSettings.DefineSetting(nameof(this.LocationY), (int)(height * 0.1), () => Strings.Setting_LocationY_Name, () => Strings.Setting_LocationY_Description);
            this.LocationY.SetRange(0, height);
            this.LocationY.SettingChanged += this.SettingChanged;

            this.Width = this.LocationSettings.DefineSetting(nameof(this.Width), (int)(width * 0.5), () => Strings.Setting_Width_Name, () => Strings.Setting_Width_Description);
            this.Width.SetRange(0, width);
            this.Width.SettingChanged += this.SettingChanged;
        }

        private void InitializeTransactionSettings(SettingCollection settings)
        {
            this.TransactionSettings = settings.AddSubCollection(TRANSACTION_SETTINGS);

            this.MaxTransactions = this.TransactionSettings.DefineSetting(nameof(this.MaxTransactions), 10, () => "Max Transactions", () => "Defines the max number of transactions shown.");
            this.MaxTransactions.SetRange(1, 50);
            this.MaxTransactions.SettingChanged += this.SettingChanged;

            this.ShowBuyTransactions = this.TransactionSettings.DefineSetting(nameof(this.ShowBuyTransactions), true, () => "Show Buy Transactions", () => "Whether buy transactions should be shown.");
            this.ShowBuyTransactions.SettingChanged += this.SettingChanged;

            this.ShowSellTransactions = this.TransactionSettings.DefineSetting(nameof(this.ShowSellTransactions), true, () => "Show Sell Transactions", () => "Whether sell transactions should be shown.");
            this.ShowSellTransactions.SettingChanged += this.SettingChanged;

            this.ShowHighestTransactions = this.TransactionSettings.DefineSetting(nameof(this.ShowHighestTransactions), true, () => "Show Highest Buy/Sell Transactions", () => "Whether the highest buy/sell transactions should be shown or only outbid ones.");
            this.ShowHighestTransactions.SettingChanged += this.SettingChanged;

            this.ShowPrice = this.TransactionSettings.DefineSetting(nameof(this.ShowPrice), true, () => "Show Price", () => "Whether the price of the transaction should be shown.");
            this.ShowPrice.SettingChanged += this.SettingChanged;

            this.ShowPriceAsTotal = this.TransactionSettings.DefineSetting(nameof(this.ShowPriceAsTotal), true, () => "Show Price as Total", () => "Whether the price of the transaction should be shown as the total price.");
            this.ShowPriceAsTotal.SettingChanged += this.SettingChanged;

            this.ShowRemaining = this.TransactionSettings.DefineSetting(nameof(this.ShowRemaining), true, () => "Show Remaining Quantity", () => "Whether the remaining quantity of the transaction should be shown.");
            this.ShowRemaining.SettingChanged += this.SettingChanged;

            this.ShowCreated = this.TransactionSettings.DefineSetting(nameof(this.ShowCreated), false, () => "Show Created Date", () => "Whether the created date of the transaction should be shown.");
            this.ShowCreated.SettingChanged += this.SettingChanged;

        }

        private void SettingChanged<T>(object sender, ValueChangedEventArgs<T> e)
        {
            SettingEntry<T> settingEntry = (SettingEntry<T>)sender;
            string prevValue = e.PreviousValue.GetType() == typeof(string) ? e.PreviousValue.ToString() : JsonConvert.SerializeObject(e.PreviousValue);
            string newValue = e.NewValue.GetType() == typeof(string) ? e.NewValue.ToString() : JsonConvert.SerializeObject(e.NewValue);
            Logger.Debug($"Changed setting \"{settingEntry.EntryKey}\" from \"{prevValue}\" to \"{newValue}\"");

            ModuleSettingsChanged?.Invoke(this, new ModuleSettingsChangedEventArgs() { Name = settingEntry.EntryKey, Value = e.NewValue });
        }

        public class ModuleSettingsChangedEventArgs
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
    }
}
