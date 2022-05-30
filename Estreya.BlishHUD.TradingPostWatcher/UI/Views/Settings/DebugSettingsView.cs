namespace Estreya.BlishHUD.TradingPostWatcher.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.Shared.State;
    using Estreya.BlishHUD.Shared.UI.Views;
    using Estreya.BlishHUD.TradingPostWatcher;
    using Microsoft.Xna.Framework;
    using System;
    using System.Threading.Tasks;

    public class DebugSettingsView : BaseSettingsView
    {
        public DebugSettingsView() : base()
        {
        }

        protected override void BuildView(Panel parent)
        {
            foreach (ManagedState state in TradingPostWatcherModule.ModuleInstance.States)
            {
                this.RenderLabel(parent, $"{state.GetType().Name} running:", state.Running.ToString(), textColorValue: state.Running ? Color.Green : Color.Red);
            }
        }

        protected override Task<bool> DoLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
