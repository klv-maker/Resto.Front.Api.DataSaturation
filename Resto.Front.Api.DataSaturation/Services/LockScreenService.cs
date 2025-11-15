using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using Resto.Front.Api.DataSaturation.ViewModels;
using Resto.Front.Api.DataSaturation.Views;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class LockScreenService : ILockService
    {
        private readonly IScreensService screensService;
        private bool isOpenedWindow;
        private ILockViewModel lockViewModel;
        private WindowOwner windowOwner;
        private bool isDisposed = false;
        private int switchMediaTime;
        public LockScreenService(IScreensService screensService) 
        {
            this.screensService = screensService;
            this.screensService.LockScreenChanged += LockScreenChanged;
        }

        public void UpdateSwitchMediaTime(int newSwitchMediaTime)
        {
            this.switchMediaTime = newSwitchMediaTime;
        }

        private void LockScreenChanged(object sender, bool isOpened)
        {
            if (isDisposed)
                return;

            PluginContext.Log.Info($"[{nameof(LockScreenService)}|{nameof(LockScreenChanged)}] Get lock screen changed. Is opened {isOpened}");
            if (isOpened && !isOpenedWindow)
            {
                isOpenedWindow = true;
                if (lockViewModel is null)
                    lockViewModel = new LockViewModel();

                if (!lockViewModel.Update(switchMediaTime))
                {
                    PluginContext.Log.Error($"[{nameof(LockScreenService)}|{nameof(LockScreenChanged)}] No media for show.");
                    isOpenedWindow = false;
                    return;
                }

                //явно вызываем очистку
                if (windowOwner != null)
                {
                    windowOwner.Dispose();
                    windowOwner = null;
                }

                windowOwner = new WindowOwner();
                windowOwner.ShowDialog<LockWindow>(lockViewModel);
                return;
            }
            if (isOpened)
                return;

            CloseWindow();
        }
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            CloseWindow();
            lockViewModel?.Dispose();
            if (screensService != null)
                screensService.LockScreenChanged -= LockScreenChanged;
        }

        private void CloseWindow()
        {
            if (!isOpenedWindow)
                return;

            if (lockViewModel?.CloseAction is null)
            {
                isOpenedWindow = false;
                return; 
            }

            isOpenedWindow = false;
            lockViewModel?.CloseAction();
            windowOwner?.Dispose();
            windowOwner = null;
        }
    }
}
