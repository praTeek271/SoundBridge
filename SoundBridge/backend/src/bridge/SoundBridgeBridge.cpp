#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include "../engine/AudioRouterEngine.h"

#include <Windows.h>

#include <cstring>
#include <memory>
#include <string>
#include <vector>

// ---------------------------------------------------------------------------
namespace {

struct SoundBridgeHandle {
    AudioRouterEngine engine;
    std::string       lastError;  // UTF-8; written by Guard on exception
    std::vector<std::wstring> pendingOutputIds;
};

// Guard now captures the exception message into the handle's lastError,
// so callers can retrieve it via SoundBridge_GetLastError().
template <typename Fn>
int Guard(SoundBridgeHandle* handle, Fn&& fn) noexcept {
    try {
        fn();
        if (handle) handle->lastError.clear();
        return 1;
    } catch (const std::exception& e) {
        if (handle) handle->lastError = e.what();
        return 0;
    } catch (...) {
        if (handle) handle->lastError = "Unknown internal error.";
        return 0;
    }
}

} // namespace

// ---------------------------------------------------------------------------
extern "C" {

using SoundBridgeDeviceCallback =
    void(__stdcall*)(
        const wchar_t* id,
        const wchar_t* name,
        int            isDefault,
        int            sampleRateHz,
        int            channelCount,
        int            bitsPerSample,
        int            volumePercent,
        int            formFactor,
        const wchar_t* category,
        const wchar_t* iconPath,
        void*          userData);

// ---- Engine lifecycle ------------------------------------------------------

__declspec(dllexport) SoundBridgeHandle* __stdcall
SoundBridge_CreateEngine() noexcept {
    try   { return new SoundBridgeHandle(); }
    catch (...) { return nullptr; }
}

__declspec(dllexport) void __stdcall
SoundBridge_DestroyEngine(SoundBridgeHandle* handle) noexcept {
    delete handle;
}

// ---- Device enumeration ----------------------------------------------------

__declspec(dllexport) int __stdcall
SoundBridge_EnumerateRenderDevices(
    SoundBridgeHandle*       handle,
    SoundBridgeDeviceCallback callback,
    void*                    userData) noexcept
{
    if (!handle || !callback) return 0;
    return Guard(handle, [&] {
        auto devices = handle->engine.GetRenderDevices();
        for (const auto& d : devices)
            callback(
                d.id.c_str(),
                d.friendlyName.c_str(),
                d.isDefault ? 1 : 0,
                d.sampleRateHz,
                d.channelCount,
                d.bitsPerSample,
                d.volumePercent,
                d.formFactor,
                d.category.c_str(),
                d.iconPath.c_str(),
                userData);
    });
}

// ---- Source / output configuration ----------------------------------------

__declspec(dllexport) int __stdcall
SoundBridge_SetSourceDevice(SoundBridgeHandle* handle, const wchar_t* deviceId) noexcept {
    if (!handle || !deviceId) return 0;
    return Guard(handle, [&] { handle->engine.SetSourceDevice(deviceId); });
}

__declspec(dllexport) int __stdcall
SoundBridge_ClearOutputDevices(SoundBridgeHandle* handle) noexcept {
    if (!handle) return 0;
    return Guard(handle, [&] {
        handle->pendingOutputIds.clear();
        handle->engine.SetOutputDevices({});
    });
}

__declspec(dllexport) int __stdcall
SoundBridge_AddOutputDevice(SoundBridgeHandle* handle, const wchar_t* deviceId) noexcept {
    if (!handle || !deviceId) return 0;
    // Engine requires SetOutputDevices() for the full list; we rebuild it here.
    // The handle caches the list so we can append incrementally.
    return Guard(handle, [&] {
        // Re-read existing list from engine then append — engine owns the list.
        // Simplest approach: call SetOutputDevices with the running list + new id.
        // We store nothing extra in the handle; the engine is the source of truth.
        // Since AddOutputDevice is always called after ClearOutputDevices in a
        // reconfigure sequence, we shadow the list in a temporary and push.
        //
        // NOTE: We rely on the caller (NativeAudioRouterService) to call
        // ClearOutputDevices before issuing a set of AddOutputDevice calls.
        // The engine enforces that SetOutputDevices throws if routing is active,
        // so these are only valid while stopped.
        std::vector<std::wstring>& tl_pending = handle->pendingOutputIds;
        // On first call after a Clear, tl_pending may have stale data —
        // ClearOutputDevices resets the engine to empty, so we reset here too.
        // We detect a reset by checking the engine's own SetOutputDevices call.
        tl_pending.emplace_back(deviceId);
        handle->engine.SetOutputDevices(handle->pendingOutputIds);
    });
}

// ---- Per-output settings ---------------------------------------------------

__declspec(dllexport) int __stdcall
SoundBridge_SetOutputVolume(
    SoundBridgeHandle* handle,
    const wchar_t*     deviceId,
    float              gain) noexcept
{
    if (!handle || !deviceId) return 0;
    return Guard(handle, [&] { handle->engine.SetOutputVolume(deviceId, gain); });
}

__declspec(dllexport) int __stdcall
SoundBridge_SetOutputDelayMs(
    SoundBridgeHandle* handle,
    const wchar_t*     deviceId,
    int                delayMs) noexcept
{
    if (!handle || !deviceId) return 0;
    return Guard(handle, [&] { handle->engine.SetOutputDelayMs(deviceId, delayMs); });
}

// ---- Routing lifecycle -----------------------------------------------------

__declspec(dllexport) int __stdcall
SoundBridge_StartRouting(SoundBridgeHandle* handle) noexcept {
    if (!handle) return 0;
    return Guard(handle, [&] { handle->engine.Start(); });
}

__declspec(dllexport) void __stdcall
SoundBridge_StopRouting(SoundBridgeHandle* handle) noexcept {
    if (handle) handle->engine.Stop();
}

__declspec(dllexport) int __stdcall
SoundBridge_UpdateRouting(
    SoundBridgeHandle*  handle,
    const wchar_t*      sourceId,
    const wchar_t**     outputIds,
    int                 outputCount) noexcept
{
    if (!handle || !sourceId || (!outputIds && outputCount > 0)) return 0;
    return Guard(handle, [&] {
        std::vector<std::wstring> ids;
        ids.reserve(static_cast<size_t>(outputCount));
        for (int i = 0; i < outputCount; ++i)
            if (outputIds[i]) ids.emplace_back(outputIds[i]);
        handle->engine.UpdateRouting(sourceId, std::move(ids));
    });
}

// ---- Calibration -----------------------------------------------------------

__declspec(dllexport) int __stdcall
SoundBridge_PlayCalibrationTone(SoundBridgeHandle* handle) noexcept {
    if (!handle) return 0;
    return Guard(handle, [&] { handle->engine.PlayCalibrationTone(); });
}

// ---- Status ----------------------------------------------------------------

__declspec(dllexport) int __stdcall
SoundBridge_IsRoutingActive(SoundBridgeHandle* handle) noexcept {
    return (handle && handle->engine.IsRunning()) ? 1 : 0;
}

__declspec(dllexport) float __stdcall
SoundBridge_GetPeakLevel(SoundBridgeHandle* handle) noexcept {
    return handle ? handle->engine.PeakLevel() : 0.0f;
}

__declspec(dllexport) int __stdcall
SoundBridge_GetActiveOutputCount(SoundBridgeHandle* handle) noexcept {
    return handle ? static_cast<int>(handle->engine.ActiveOutputCount()) : 0;
}

// ---- Error reporting -------------------------------------------------------

// Copies the last error message (UTF-8) into buf.
// Returns the number of bytes written (0 = no error or handle is null).
__declspec(dllexport) int __stdcall
SoundBridge_GetLastError(
    SoundBridgeHandle* handle,
    char*              buf,
    int                bufSize) noexcept
{
    if (!handle || !buf || bufSize <= 0) return 0;

    // Also check the engine's internal last error (e.g. from RoutingLoop crash)
    std::string engineErr = handle->engine.GetLastError();
    const std::string& err = engineErr.empty() ? handle->lastError : engineErr;

    if (err.empty()) { buf[0] = '\0'; return 0; }

    int len = static_cast<int>(
        std::min(err.size(), static_cast<size_t>(bufSize - 1)));
    std::memcpy(buf, err.c_str(), static_cast<size_t>(len));
    buf[len] = '\0';
    return len;
}

} // extern "C"
