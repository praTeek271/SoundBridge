#pragma once
#include "RenderStream.h"

#include <memory>
#include <string>
#include <utility>
#include <vector>

// Owns one RenderStream per active output device and fans captured audio
// out to all of them simultaneously. Streams are keyed by device endpoint ID
// so volumes and delays can be addressed individually.
class MultiRenderer {
public:
    // Attempts to add one output device. On failure, logs a warning to stderr
    // and returns false — the failure is isolated; other devices continue.
    // gain    — initial volume multiplier [0.0, 1.0]
    // delayMs — latency compensation silence pre-fill (0–2000ms)
    bool AddDevice(
        Microsoft::WRL::ComPtr<IMMDevice> device,
        const std::wstring&               name,
        const WAVEFORMATEX*               format,
        float                             gain    = 1.0f,
        int                               delayMs = 0);

    // Fan audio out to every active stream.
    void PushAudio(const BYTE* data, size_t numBytes);

    // Live gain update on a named stream (no-op if device not found).
    void SetGain(const std::wstring& deviceId, float gain);

    size_t DeviceCount() const { return m_streams.size(); }

private:
    // vector of (endpointId, stream) pairs.
    // Linear search is fine for the 2–8 devices typical of this use case.
    std::vector<std::pair<std::wstring, std::unique_ptr<RenderStream>>> m_streams;
};
