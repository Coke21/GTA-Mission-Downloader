using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;
using GTAMissionDownloader.ViewModels;

namespace GTAMissionDownloader
{
    class Bootstrapper : BootstrapperBase
    {
        public Bootstrapper()
        {
            Initialize();
        }
        protected override void Configure()
        {
            MessageBinder.SpecialValues.Add("$mousepoint", ctx =>
            {
                var e = ctx.EventArgs as MouseEventArgs;

                return e?.GetPosition(ctx.Source);
            });
        }
        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<MainViewModel>();
        }
    }
}
