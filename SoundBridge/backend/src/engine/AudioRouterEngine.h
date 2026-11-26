#pragma once

#include "../audio/DeviceEnumerator.h"

#include <atomic>
#include <map>
#include <mutex>
#include <string>
#include <thread>
#include <vector>

class AudioRouterEngine {
public:
    AudioRouterEngine()  = default;
    ~AudioRouterEngine();

    AudioRouterEngine(const AudioRouterEngine&)            = delete;
    AudioRouterEngine& operator=(const AudioRouterEngine&) = delete;

    // ---- Device enumeration ------------------------------------------------
    std::vector<AudioDevice> GetRenderDevices() const;

    // ---- Configuration (must be called while NOT running) ------------------
    void SetSourceDevice(std::wstring deviceId);
    void SetOutputDevices(std::vector<std::wstring> deviceIds);

    // Per-output config — safe to call any time; applied on next Start().
    // Calling while running does NOT hot-reload; use UpdateRouting() for that.
    void SetOutputVolume(const std::wstring& deviceId, float gain);
    void SetOutputDelayMs(const std::wstring& deviceId, int ms);

    // ---- Lifecycle ---------------------------------------------------------
    void Start();
    void Stop();

    // Stop, apply new source/output config, and restart in one call.
    // Stored per-output volumes and delays are preserved unless overwritten
    // before calling this.
    void UpdateRouting(std::wstring sourceDeviceId, std::vector<std::wstring> outputDeviceIds);

    // ---- Status ------------------------------------------------------------
    bool   IsRunning()         const { return m_running.load(); }
    float  PeakLevel()         const { return m_peakLevel.load(); }
    size_t ActiveOutputCount() const { return m_activeOutputCount.load(); }

    // Returns the last error message from a failed Start()/RoutingLoop().
    // Empty string if no error has occurred or last operation succeeded.
    std::string GetLastError() const;

    // ---- Calibration -------------------------------------------------------
    // Generates 3×800Hz beeps and injects them into the running renderer.
    // Throws std::runtime_error if routing is not currently active.
    void PlayCalibrationTone();

private:
    void RoutingLoop(
        std::wstring                sourceDeviceId,
        std::vector<std::wstring>   outputDeviceIds,
        std::map<std::wstring,float> volumes,
        std::map<std::wstring,int>   delays);

    void StoreLastError(const std::string& msg);

    mutable std::mutex          m_configMutex;
    std::wstring                m_sourceDeviceId;
    std::vector<std::wstring>   m_outputDeviceIds;
    std::map<std::wstring,float> m_outputVolumes;  // deviceId → gain [0,1]
    std::map<std::wstring,int>   m_outputDelays;   // deviceId → ms  [0,2000]
    std::string                 m_lastError;

    std::thread        m_worker;
    std::atomic<bool>  m_running{false};
    std::atomic<float> m_peakLevel{0.0f};
    std::atomic<size_t> m_activeOutputCount{0};

    // Format info written by RoutingLoop so PlayCalibrationTone can generate
    // PCM in the correct format without needing access to the capture object.
    std::atomic<uint32_t> m_sampleRate{0};
    std::atomic<uint32_t> m_bitsPerSample{0};
    std::atomic<uint32_t> m_channels{0};
    std::atomic<uint32_t> m_blockAlign{0};

    // Calibration tone buffer — written by PlayCalibrationTone(),
    // consumed and cleared by RoutingLoop on the next iteration.
    std::mutex           m_calibMutex;
    std::vector<uint8_t> m_calibBuffer;
};
