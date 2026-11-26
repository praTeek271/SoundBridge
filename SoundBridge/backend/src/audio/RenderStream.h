#pragma once
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>
#include <audioclient.h>
#include <mmdeviceapi.h>
#include <mmreg.h>
#include <wrl/client.h>

#include <atomic>
#include <deque>
#include <mutex>
#include <string>
#include <thread>

// Renders audio to one output device on its own dedicated thread.
// Audio is pushed in via Push() from the capture loop; an internal thread
// pulls from the queue and writes to the WASAPI render buffer.
//
// AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM lets every device accept the same
// PCM format the capture produces, regardless of the device's native rate.
// Windows resamples internally — no resampling code needed here.
class RenderStream {
public:
    // gain   — linear volume multiplier applied to each sample [0.0, 1.0].
    //          Applied only to 32-bit float PCM (the standard WASAPI mix
    //          format on Windows 10/11). Other bit depths pass through unchanged.
    // delayMs — silence pre-fill injected at stream start (latency compensation).
    //           Convert to bytes using nAvgBytesPerSec. Capped at 2000ms.
    // Throws std::runtime_error on WASAPI failure.
    RenderStream(
        Microsoft::WRL::ComPtr<IMMDevice> device,
        std::wstring name,
        const WAVEFORMATEX* format,
        float gain   = 1.0f,
        int   delayMs = 0);

    ~RenderStream();

    // Thread-safe. numBytes must be a multiple of nBlockAlign.
    void Push(const BYTE* data, size_t numBytes);

    // Update gain on a live stream (takes effect on next RenderLoop iteration).
    void SetGain(float gain) { m_gain.store(std::max(0.0f, std::min(1.0f, gain))); }

    const std::wstring& Name() const { return m_name; }

    // Approximate queued-but-not-yet-played audio in ms. Useful for sync diagnostics.
    double QueuedMs() const;

private:
    void RenderLoop();

    Microsoft::WRL::ComPtr<IAudioClient>       m_client;
    Microsoft::WRL::ComPtr<IAudioRenderClient> m_render;
    UINT32 m_bufferFrames = 0;

    // Full copy including WAVEFORMATEXTENSIBLE tail (cbSize bytes).
    WAVEFORMATEXTENSIBLE m_format{};

    std::wstring m_name;
    std::atomic<float> m_gain{1.0f};

    std::deque<BYTE>  m_queue;
    mutable std::mutex m_mutex;

    std::thread        m_thread;
    std::atomic<bool>  m_running{false};
};
