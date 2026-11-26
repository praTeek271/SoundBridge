#include "LoopbackCapture.h"
#include "../util/Win32Util.h"

#include <cstring>
#include <stdexcept>

using Microsoft::WRL::ComPtr;

namespace {
    // 20ms buffer - small enough for low latency, large enough that a 10ms
    // polling loop never starves it. Matches Microsoft's loopback sample.
    constexpr REFERENCE_TIME REFTIMES_20MS = 200000;
}

LoopbackCapture::LoopbackCapture(ComPtr<IMMDevice> device) {
    HRESULT hr = device->Activate(__uuidof(IAudioClient), CLSCTX_ALL, nullptr,
                                   reinterpret_cast<void**>(m_client.GetAddressOf()));
    if (FAILED(hr))
        throw std::runtime_error("LoopbackCapture: Activate(IAudioClient) failed, hr=" + HrHex(hr));

    hr = m_client->GetMixFormat(&m_format);
    if (FAILED(hr))
        throw std::runtime_error("LoopbackCapture: GetMixFormat failed, hr=" + HrHex(hr));

    hr = m_client->Initialize(
        AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_STREAMFLAGS_LOOPBACK,
        REFTIMES_20MS,
        0,
        m_format,
        nullptr);
    if (FAILED(hr))
        throw std::runtime_error("LoopbackCapture: Initialize failed, hr=" + HrHex(hr));

    hr = m_client->GetService(__uuidof(IAudioCaptureClient),
                               reinterpret_cast<void**>(m_capture.GetAddressOf()));
    if (FAILED(hr))
        throw std::runtime_error("LoopbackCapture: GetService(IAudioCaptureClient) failed, hr=" + HrHex(hr));
}

LoopbackCapture::~LoopbackCapture() {
    if (m_client) m_client->Stop();
    if (m_format) CoTaskMemFree(m_format);
}

void LoopbackCapture::Start() {
    HRESULT hr = m_client->Start();
    if (FAILED(hr))
        throw std::runtime_error("LoopbackCapture: Start failed, hr=" + HrHex(hr));
}

void LoopbackCapture::Stop() {
    m_client->Stop();
}

size_t LoopbackCapture::ReadAvailable(std::vector<BYTE>& out) {
    size_t totalBytes = 0;
    UINT32 packetLength = 0;

    if (FAILED(m_capture->GetNextPacketSize(&packetLength)))
        return 0;

    while (packetLength != 0) {
        BYTE* data = nullptr;
        UINT32 numFrames = 0;
        DWORD flags = 0;

        if (FAILED(m_capture->GetBuffer(&data, &numFrames, &flags, nullptr, nullptr)))
            break;

        size_t bytes = static_cast<size_t>(numFrames) * m_format->nBlockAlign;
        size_t offset = out.size();
        out.resize(offset + bytes);

        if (flags & AUDCLNT_BUFFERFLAGS_SILENT) {
            std::memset(out.data() + offset, 0, bytes);
        } else {
            std::memcpy(out.data() + offset, data, bytes);
        }
        totalBytes += bytes;

        m_capture->ReleaseBuffer(numFrames);

        if (FAILED(m_capture->GetNextPacketSize(&packetLength)))
            break;
    }

    return totalBytes;
}
