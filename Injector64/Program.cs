using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Injector64
{
    internal class Program
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        const uint MEM_COMMIT = 0x1000;
        const uint MEM_RESERVE = 0x2000;
        const uint PAGE_READWRITE = 0x04;
        const uint PROCESS_ALL_ACCESS = 0x001F0FFF;

        static void Main(string[] args)
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string logFile = Path.Combine(appdata, "iiko\\CashServer\\Logs", "injector64_log.txt");
            File.WriteAllText(logFile, $"Starting injection at {DateTime.Now}\n");

            if (args.Length < 1)
            {
                Console.WriteLine("Usage: Injector64 <PID>");
                return;
            }

            int pid = int.Parse(args[0]);
            string dllPath = Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"Resto.Front.Api.DataSaturation.InjectorManager.dll");
            try
            {
                Inject(pid, dllPath);
                File.AppendAllText(logFile, "\nInjection completed successfully");
            }
            catch (Exception ex)
            {
                File.AppendAllText(logFile, $"\nInjection failed: {ex}");
            }
        }

        static void Inject(int processId, string dllPath)
        {
            if (!File.Exists(dllPath))
                throw new FileNotFoundException($"DLL not found: {dllPath}");

            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);
            if (hProcess == IntPtr.Zero)
                throw new Exception($"OpenProcess failed. Error: {Marshal.GetLastWin32Error()}");

            try
            {
                // Выделяем память для пути к DLL
                byte[] pathBytes = Encoding.Unicode.GetBytes(dllPath + "\0");
                IntPtr remoteMemory = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)pathBytes.Length,
                    MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (remoteMemory == IntPtr.Zero)
                    throw new Exception($"VirtualAllocEx failed. Error: {Marshal.GetLastWin32Error()}");

                try
                {
                    // Записываем путь в память
                    bool writeResult = WriteProcessMemory(hProcess, remoteMemory, pathBytes,
                        (uint)pathBytes.Length, out UIntPtr bytesWritten);

                    if (!writeResult)
                        throw new Exception($"WriteProcessMemory failed. Error: {Marshal.GetLastWin32Error()}");

                    // Получаем адрес LoadLibraryW
                    IntPtr kernel32 = GetModuleHandle("kernel32.dll");
                    IntPtr loadLibrary = GetProcAddress(kernel32, "LoadLibraryW");

                    if (loadLibrary == IntPtr.Zero)
                        throw new Exception("GetProcAddress failed for LoadLibraryW");

                    // Создаем удаленный поток
                    IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibrary,
                        remoteMemory, 0, IntPtr.Zero);

                    if (hThread == IntPtr.Zero)
                        throw new Exception($"CreateRemoteThread failed. Error: {Marshal.GetLastWin32Error()}");

                    try
                    {
                        // Ждем завершения
                        uint waitResult = WaitForSingleObject(hThread, 10000);
                        if (waitResult != 0)
                            throw new Exception($"Thread wait failed. Result: {waitResult}");
                    }
                    finally
                    {
                        CloseHandle(hThread);
                    }
                }
                finally
                {
                    // Освобождаем память
                    VirtualFreeEx(hProcess, remoteMemory, UIntPtr.Zero, 0x8000); // MEM_RELEASE
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }
    }
}
