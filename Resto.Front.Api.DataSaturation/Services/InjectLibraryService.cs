using Resto.Front.Api.DataSaturation.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Resto.Front.Api.DataSaturation.Services
{
    public static class InjectLibraryService
    {
        private const string SettingsFileName = "settings.txt";

        public static void Inject()
        {
            try
            {
                try
                {
                    Process targetProcess = Process.GetProcessesByName(Constants.AppProcessName).SingleOrDefault();
                    if (targetProcess is null)
                    {
                        PluginContext.Log.Error($"Process {Constants.AppProcessName} not found");
                        return;
                    }
                    AddSettings();
                    PluginContext.Log.Info("Using 64-bit external injector...");
                    string pathToInjectedDll = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources", $"Injector64.exe");
                    PluginContext.Log.Info(pathToInjectedDll);
                    // Запускаем 64-битный инжектор
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = pathToInjectedDll,
                        Arguments = $"{targetProcess.Id}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    };

                    PluginContext.Log.Info($"Starting 64-bit injector: {startInfo.FileName} {startInfo.Arguments}");

                    using (Process injectorProcess = Process.Start(startInfo))
                    {
                        if (injectorProcess != null)
                        {
                            bool exited = injectorProcess.WaitForExit(15000); // 15 секунд таймаут
                            if (!exited)
                            {
                                PluginContext.Log.Warn("64-bit injector timeout, killing...");
                                injectorProcess.Kill();
                            }
                            else
                            {
                                PluginContext.Log.Info($"64-bit injector exited with code: {injectorProcess.ExitCode}");
                            }
                        }
                        else
                        {
                            PluginContext.Log.Error("Failed to start 64-bit injector process");
                        }
                    }
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"64-bit injection failed: {ex}");
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(InjectLibraryService)}|{nameof(Inject)}] Exception: {ex}");
            }
        }

        public static void Close()
        {
        }

        private static void AddSettings()
        {
            var resourcesPath = PluginContext.Integration.GetDataStorageDirectoryPath();
            if (!Directory.Exists(resourcesPath))
                return;

            var fileSettings = Path.Combine(resourcesPath, SettingsFileName);
            if (File.Exists(fileSettings))
                File.Delete(fileSettings);

            File.WriteAllText(fileSettings, Settings.Settings.Instance().SwitchMediaTime.ToString());
        }
    }
}
