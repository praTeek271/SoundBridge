#pragma once
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>
#include <mmdeviceapi.h>
#include <audioclient.h>
#include <wrl/client.h>

#include <vector>

// Captures whatever audio Windows is currently sending to a given render
// (output) device, via WASAPI loopback. This is how we "tap into" the
// Virtual Audio Device's output stream.
class LoopbackCapture {
public:
    // `device` is the endpoint to capture from (e.g. the Virtual Audio
    // Driver, once it's set as the Windows default output).
    // Throws std::runtime_error on failure.
    explicit LoopbackCapture(Microsoft::WRL::ComPtr<IMMDevice> device);
    ~LoopbackCapture();

    // The PCM format of captured audio. RenderStream instances should be
    // created with this same format pointer (see AUTOCONVERTPCM in
    // RenderStream for why mismatched device formats are OK).
    const WAVEFORMATEX* GetFormat() const { return m_format; }

    void Start();
    void Stop();

    // Appends any audio captured since the last call to `out` (PCM bytes,
    // laid out per GetFormat()). Returns the number of bytes appended.
    // Call this repeatedly from a loop (e.g. every ~10ms). Returning 0 is
    // normal when the source device is silent/idle - just call again later.
    size_t ReadAvailable(std::vector<BYTE>& out);

private:
    Microsoft::WRL::ComPtr<IAudioClient> m_client;
    Microsoft::WRL::ComPtr<IAudioCaptureClient> m_capture;
    WAVEFORMATEX* m_format = nullptr;  // CoTaskMemAlloc'd by GetMixFormat, freed in dtor
};