#include "MultiRenderer.h"
#include "../util/Win32Util.h"

#include <iostream>

bool MultiRenderer::AddDevice(
    Microsoft::WRL::ComPtr<IMMDevice> device,
    const std::wstring&               name,
    const WAVEFORMATEX*               format,
    float                             gain,
    int                               delayMs)
{
    try {
        auto stream = std::make_unique<RenderStream>(device, name, format, gain, delayMs);
        m_streams.emplace_back(name, std::move(stream));
        return true;
    } catch (const std::exception& e) {
        std::cerr << "[MultiRenderer] Could not open '" << ToUtf8(name) << "': "
                  << e.what() << "\n";
        return false;
    }
}

void MultiRenderer::PushAudio(const BYTE* data, size_t numBytes) {
    for (auto& [id, stream] : m_streams)
        stream->Push(data, numBytes);
}

void MultiRenderer::SetGain(const std::wstring& deviceId, float gain) {
    for (auto& [id, stream] : m_streams) {
        if (id == deviceId) {
            stream->SetGain(gain);
            return;
        }
    }
}
