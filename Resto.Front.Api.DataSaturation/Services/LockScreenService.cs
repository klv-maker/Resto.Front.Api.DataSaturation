using Resto.Front.Api.DataSaturation.Interfaces.Services;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class LockScreenService : ILockService
    {
        private bool isOpenedThread;
        private bool isDisposed = false;
        public LockScreenService() 
        {
            PluginContext.Log.Info($"[{nameof(LockScreenService)}|.ctor] Clean resourses");
            PluginContext.Log.Info($"[{nameof(LockScreenService)}|.ctor] Send lock screen showing. Service is created");
        }

        public void LockScreenChanged(object sender, bool isOpened)
        {
            if (isDisposed)
                return;

            PluginContext.Log.Info($"[{nameof(LockScreenService)}|{nameof(LockScreenChanged)}] Get lock changed event {isOpened}");
            if (isOpened && !isOpenedThread)
            {
                isOpenedThread = true;
                InjectLibraryService.Inject();
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            CloseWindow();
        }

        private void CloseWindow()
        {
            if (!isOpenedThread)
                return;

            if (isOpenedThread)
                InjectLibraryService.Close();
            isOpenedThread = false;
        }
    }
}
