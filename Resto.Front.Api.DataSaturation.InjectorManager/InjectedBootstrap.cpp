//не лезь - убьет


#include "pch.h"
#include <Windows.h>
#include <ShlObj.h>
#include <stdio.h>
#include <fstream>
#include <string>
#include <msclr/marshal_cppstd.h>
#include <metahost.h>
#include <mscoree.h>

#pragma comment(lib, "Shell32.lib")
#pragma comment(lib, "mscoree.lib")
#pragma comment(lib, "Ole32.lib")

using namespace System;
using namespace System::Reflection;
using namespace System::IO;
using namespace msclr::interop;

// Глобальные переменные
static HANDLE g_hWorkerThread = NULL;
static HANDLE g_hShutdownEvent = NULL;
static volatile bool g_IsRunning = false;
static HMODULE g_hModule = NULL;
static std::wstring g_LogFilePath;

std::wstring GetAppDataRoamingPath()
{
    PWSTR path = nullptr;
    // FOLDERID_RoamingAppData соответствует папке AppData\Roaming
    if (SUCCEEDED(SHGetKnownFolderPath(FOLDERID_RoamingAppData, 0, nullptr, &path)))
    {
        std::wstring result(path);
        CoTaskMemFree(path);
        return result;
    }
    return L"";
}

bool CreateDirectoryRecursive(const std::wstring& path)
{
    size_t pos = 0;
    std::wstring dir = path;

    if (dir[dir.length() - 1] != L'\\') {
        dir += L"\\";
    }

    while ((pos = dir.find_first_of(L'\\', pos)) != std::wstring::npos)
    {
        std::wstring subdir = dir.substr(0, pos);
        CreateDirectoryW(subdir.c_str(), NULL);
        pos++;
    }

    return true;
}

std::wstring GetLogFilePath()
{
    std::wstring appDataPath = GetAppDataRoamingPath();
    std::wstring logDir = appDataPath + L"\\iiko\\CashServer\\Logs";
    CreateDirectoryRecursive(logDir);
    return logDir + L"\\injection.log";
}

void Log(const char* message)
{
    try
    {
        if (g_LogFilePath.empty())
        {
            g_LogFilePath = GetLogFilePath();
        }

        std::wofstream file(g_LogFilePath, std::ios::app);
        if (file.is_open())
        {
            SYSTEMTIME st;
            GetLocalTime(&st);

            wchar_t timeBuffer[64];
            swprintf_s(timeBuffer, L"[%04d-%02d-%02d %02d:%02d:%02d.%03d]",
                st.wYear, st.wMonth, st.wDay,
                st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);

            file << timeBuffer << L" " << message << std::endl;
            file.close();
        }
    }
    catch (...)
    {
        // Резервное логирование
        try
        {
            std::wofstream file(L"C:\\Temp\\iiko_injection_fallback.log", std::ios::app);
            if (file.is_open())
            {
                file << L"Fallback: " << message << std::endl;
                file.close();
            }
        }
        catch (...) {}
    }
}

void Log(String^ message)
{
    std::string msg = marshal_as<std::string>(message);
    Log(msg.c_str());
}

// Функция для получения пути к текущей DLL
String^ GetCurrentDllDirectory()
{
    try
    {
        wchar_t dllPath[MAX_PATH];
        if (GetModuleFileNameW(g_hModule, dllPath, MAX_PATH) > 0)
        {
            String^ path = gcnew String(dllPath);
            return Path::GetDirectoryName(path);
        }
    }
    catch (...) {}
    return nullptr;
}

// Обработчик для разрешения загрузки сборок
Assembly^ AssemblyResolveHandler(Object^ sender, ResolveEventArgs^ args)
{
    try
    {
        String^ currentDir = GetCurrentDllDirectory();
        if (String::IsNullOrEmpty(currentDir))
            return nullptr;

        String^ assemblyName = args->Name;
        int commaIndex = assemblyName->IndexOf(',');
        if (commaIndex > 0)
        {
            assemblyName = assemblyName->Substring(0, commaIndex);
        }

        Log(String::Format("Trying to resolve assembly: {0}", assemblyName));

        array<String^>^ extensions = { ".dll", ".exe" };

        for each (String ^ extension in extensions)
        {
            String^ assemblyPath = Path::Combine(currentDir, assemblyName + extension);
            if (File::Exists(assemblyPath))
            {
                Log(String::Format("Loading assembly: {0}", assemblyPath));
                return Assembly::LoadFrom(assemblyPath);
            }
        }

        array<String^>^ searchPaths = {
            currentDir,
            Path::Combine(currentDir, "x64"),
            Path::Combine(currentDir, "lib"),
            Path::Combine(currentDir, "bin")
        };

        for each (String ^ searchPath in searchPaths)
        {
            if (!Directory::Exists(searchPath))
                continue;

            for each (String ^ extension in extensions)
            {
                String^ assemblyPath = Path::Combine(searchPath, assemblyName + extension);
                if (File::Exists(assemblyPath))
                {
                    Log(String::Format("Loading assembly from subfolder: {0}", assemblyPath));
                    return Assembly::LoadFrom(assemblyPath);
                }
            }
        }
    }
    catch (Exception^ ex)
    {
        Log(String::Format("Assembly resolve error: {0}", ex->Message));
    }

    Log(String::Format("Failed to resolve assembly: {0}", args->Name));
    return nullptr;
}

// Инициализация управляемого кода - только загрузка и вызов Bootstrap.Run()
void InitializeManagedCode()
{
    try
    {
        // Загружаем ConnectionLib
        String^ connectionLibPath = Path::Combine(GetCurrentDllDirectory(),
            "Resto.Front.Api.DataSaturation.ConnectionLib.dll");

        Log(String::Format("Loading ConnectionLib from: {0}", connectionLibPath));

        if (File::Exists(connectionLibPath))
        {
            Assembly^ connectionLib = Assembly::LoadFrom(connectionLibPath);
            Log("ConnectionLib loaded successfully");

            // Вызываем Bootstrap.Run()
            Type^ bootstrapType = connectionLib->GetType(
                "Resto.Front.Api.DataSaturation.ConnectionLib.Bootstrap");

            if (bootstrapType != nullptr)
            {
                MethodInfo^ runMethod = bootstrapType->GetMethod("Run",
                    BindingFlags::Public | BindingFlags::Static);

                if (runMethod != nullptr)
                {
                    Log("Calling Bootstrap.Run()");
                    runMethod->Invoke(nullptr, nullptr);
                    Log("Bootstrap.Run() completed");
                }
                else
                {
                    Log("Bootstrap.Run method not found");
                }
            }
            else
            {
                Log("Bootstrap type not found");
            }
        }
        else
        {
            Log("ConnectionLib DLL not found at expected path");
        }
    }
    catch (Exception^ ex)
    {
        Log(String::Format("InitializeManagedCode error: {0}", ex));
    }
}

// Основной рабочий поток
DWORD WINAPI ManagedThreadProc(LPVOID lpParam)
{
    Log("ManagedThreadProc: Starting");
    g_IsRunning = true;

    try
    {
        // Регистрируем обработчик для разрешения загрузки сборок
        AppDomain::CurrentDomain->AssemblyResolve +=
            gcnew ResolveEventHandler(AssemblyResolveHandler);

        // Инициализируем управляемый код
        InitializeManagedCode();
    }
    catch (Exception^ ex)
    {
        Log(String::Format("ManagedThreadProc exception: {0}", ex));
    }

    Log("ManagedThreadProc: Exiting");
    return 0;
}

// Очистка ресурсов
void CleanupResources()
{
    Log("CleanupResources: Starting cleanup");

    g_IsRunning = false;

    // Сигнализируем о завершении
    if (g_hShutdownEvent)
    {
        SetEvent(g_hShutdownEvent);
    }

    // Ждем завершения рабочего потока
    if (g_hWorkerThread)
    {
        Log("Waiting for worker thread to finish...");
        WaitForSingleObject(g_hWorkerThread, 10000);
        CloseHandle(g_hWorkerThread);
        g_hWorkerThread = NULL;
    }

    // Закрываем handle события
    if (g_hShutdownEvent)
    {
        CloseHandle(g_hShutdownEvent);
        g_hShutdownEvent = NULL;
    }

    Log("CleanupResources: Cleanup completed");
}

// Точка входа DLL
BOOL APIENTRY DllMain(HMODULE hModule,
    DWORD  ul_reason_for_call,
    LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    {
        Log("DllMain: DLL_PROCESS_ATTACH");
        g_hModule = hModule;

        // Отключаем вызовы DllMain для потоков
        DisableThreadLibraryCalls(hModule);

        // Инициализируем путь к лог-файлу
        g_LogFilePath = GetLogFilePath();
        Log("Log system initialized");

        // Создаем событие для завершения
        g_hShutdownEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
        if (!g_hShutdownEvent)
        {
            Log("Failed to create shutdown event");
            return FALSE;
        }

        // Создаем рабочий поток
        DWORD threadId;
        g_hWorkerThread = CreateThread(
            nullptr,
            0,
            ManagedThreadProc,
            nullptr,
            0,
            &threadId);

        if (g_hWorkerThread)
        {
            Log("Worker thread created successfully");
        }
        else
        {
            Log("Failed to create worker thread");
            CloseHandle(g_hShutdownEvent);
            g_hShutdownEvent = NULL;
            return FALSE;
        }
    }
    break;

    case DLL_PROCESS_DETACH:
        Log("DllMain: DLL_PROCESS_DETACH");
        CleanupResources();
        break;

    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    }

    return TRUE;
}

// Экспортируемые функции
extern "C" __declspec(dllexport) void Shutdown()
{
    Log("Shutdown: Called");
    g_IsRunning = false;
}

extern "C" __declspec(dllexport) bool IsRunning()
{
    return g_IsRunning;
}