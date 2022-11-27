// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"

HMODULE mod;

typedef void(__cdecl* _OnInit)();
typedef void(__cdecl* _OnDispose)();

_OnInit on_init;
_OnDispose on_dispose;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
        break;
    case DLL_PROCESS_DETACH:
        if (mod) {
            on_dispose();
            break;
        }
    }

    return TRUE;
}

extern "C" __declspec(dllexport) void Init()
{
    mod = LoadLibraryA("MegaMixThumbnailManager.Core.dll");

    if (mod) {
        on_init = (_OnInit) GetProcAddress(mod, "OnInit");
        on_dispose = (_OnDispose)GetProcAddress(mod, "OnDispose");

        on_init();
    }
}
