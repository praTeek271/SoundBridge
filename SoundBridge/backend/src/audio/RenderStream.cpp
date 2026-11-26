#include "RenderStream.h"
#include "../util/Win32Util.h"

#include <algorithm>
#include <avrt.h>
#include <chrono>
#include <cstring>
#include <stdexcept>

using Microsoft::WRL::ComPtr;

namespace {
    constexpr REFERENCE_TIME REFTIMES_20MS = 200000;
    constexpr int             MAX_DELAY_MS  = 2000;
}

RenderStream::RenderStream(
    ComPtr<IMMDevice>     device,
    std::wstring          name,
    const WAVEFORMATEX*   format,
    float                 gain,
    int                   delayMs)
    : m_name(std::move(name))
{
    // ---- Store format (copy including WAVEFORMATEXTENSIBLE tail) -----------
    size_t copyBytes = sizeof(WAVEFORMATEX) + format->cbSize;
    copyBytes = std::min(copyBytes, sizeof(WAVEFORMATEXTENSIBLE));
    std::memcpy(&m_format, format, copyBytes);

    m_gain.store(std::max(0.0f, std::min(1.0f, gain)));

    // ---- Open WASAPI render client -----------------------------------------
    HRESULT hr = device->Activate(
        __uuidof(IAudioClient), CLSCTX_ALL, nullptr,
        reinterpret_cast<void**>(m_client.GetAddressOf()));
    if (FAILED(hr))
        throw std::runtime_error("RenderStream Activate failed, hr=" + HrHex(hr));

    hr = m_client->Initialize(
        AUDCLNT_SHAREMODE_SHARED,
        AUDCLNT_STREAMFLAGS_AUTOCONVERTPCM | AUDCLNT_STREAMFLAGS_SRC_DEFAULT_QUALITY,
        REFTIMES_20MS, 0,
        &m_format.Format,
        nullptr);
    if (FAILED(hr))
        throw std::runtime_error("RenderStream Initialize failed, hr=" + HrHex(hr));

    hr = m_client->GetBufferSize(&m_bufferFrames);
    if (FAILED(hr))
        throw std::runtime_error("RenderStream GetBufferSize failed, hr=" + HrHex(hr));

    hr = m_client->GetService(
        __uuidof(IAudioRenderClient),
        reinterpret_cast<void**>(m_render.GetAddressOf()));
    if (FAILED(hr))
        throw std::runtime_error("RenderStream GetService failed, hr=" + HrHex(hr));

    // ---- Pre-fill device buffer with silence (prevents startup glitch) -----
    {
        BYTE* data = nullptr;
        if (SUCCEEDED(m_render->GetBuffer(m_bufferFrames, &data)))
            m_render->ReleaseBuffer(m_bufferFrames, AUDCLNT_BUFFERFLAGS_SILENT);
    }

    // ---- Pre-fill delay silence into queue ---------------------------------
    // This shifts all audio from this device into the future by delayMs,
    // letting slower devices (e.g. Bluetooth) stay in sync with faster ones.
    if (delayMs > 0 && m_format.Format.nAvgBytesPerSec > 0) {
        int clampedMs = std::min(delayMs, MAX_DELAY_MS);
        size_t delayBytes = static_cast<size_t>(m_format.Format.nAvgBytesPerSec)
                            * static_cast<size_t>(clampedMs) / 1000;
        // Round down to block-aligned boundary
        delayBytes = (delayBytes / m_format.Format.nBlockAlign) * m_format.Format.nBlockAlign;
        if (delayBytes > 0) {
            std::lock_guard<std::mutex> lock(m_mutex);
            m_queue.insert(m_queue.end(), delayBytes, BYTE(0));
        }
    }

    hr = m_client->Start();
    if (FAILED(hr))
        throw std::runtime_error("RenderStream Start failed, hr=" + HrHex(hr));

    m_running = true;
    m_thread  = std::thread(&RenderStream::RenderLoop, this);
}

RenderStream::~RenderStream() {
    m_running = false;
    if (m_thread.joinable()) m_thread.join();
    if (m_client) m_client->Stop();
}

void RenderStream::Push(const BYTE* data, size_t numBytes) {
    if (numBytes == 0) return;

    std::lock_guard<std::mutex> lock(m_mutex);
    m_queue.insert(m_queue.end(), data, data + numBytes);

    // Cap queue at ~1 second to prevent unbounded growth if a device stalls.
    const size_t maxBytes = static_cast<size_t>(m_format.Format.nAvgBytesPerSec);
    if (maxBytes > 0 && m_queue.size() > maxBytes) {
        size_t drop = m_queue.size() - maxBytes;
        // Drop whole frames only
        drop = (drop / m_format.Format.nBlockAlign) * m_format.Format.nBlockAlign;
        if (drop > 0)
            m_queue.erase(m_queue.begin(), m_queue.begin() + drop);
    }
}

double RenderStream::QueuedMs() const {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (m_format.Format.nAvgBytesPerSec == 0) return 0.0;
    return 1000.0 * static_cast<double>(m_queue.size())
           / m_format.Format.nAvgBytesPerSec;
}

void RenderStream::RenderLoop() {
    DWORD taskIndex = 0;
    HANDLE hTask = AvSetMmThreadCharacteristicsW(L"Audio", &taskIndex);

    while (m_running) {
        UINT32 padding = 0;
        if (FAILED(m_client->GetCurrentPadding(&padding))) break;

        UINT32 framesAvailable = m_bufferFrames - padding;
        if (framesAvailable == 0) {
            std::this_thread::sleep_for(std::chrono::milliseconds(5));
            continue;
        }

        BYTE*  renderData  = nullptr;
        if (FAILED(m_render->GetBuffer(framesAvailable, &renderData))) break;

        const UINT32 blockAlign   = m_format.Format.nBlockAlign;
        const size_t bytesNeeded  = static_cast<size_t>(framesAvailable) * blockAlign;
        size_t       bytesCopied  = 0;
        DWORD        flags        = 0;

        {
            std::lock_guard<std::mutex> lock(m_mutex);
            bytesCopied = std::min(bytesNeeded, m_queue.size());

            if (bytesCopied > 0) {
                // Copy from deque into render buffer
                std::copy(m_queue.begin(), m_queue.begin() + bytesCopied, renderData);
                m_queue.erase(m_queue.begin(), m_queue.begin() + bytesCopied);

                // Apply per-device volume gain in-place on the render buffer.
                // Only for 32-bit float PCM (the standard WASAPI shared-mode format).
                // Other bit depths are passed through unchanged.
                float gain = m_gain.load();
                if (gain != 1.0f && m_format.Format.wBitsPerSample == 32) {
                    float* samples   = reinterpret_cast<float*>(renderData);
                    size_t numFloats = bytesCopied / sizeof(float);
                    for (size_t i = 0; i < numFloats; ++i)
                        samples[i] *= gain;
                }
            }
        }

        // Pad remaining frames with silence if queue was drained
        if (bytesCopied < bytesNeeded) {
            std::memset(renderData + bytesCopied, 0, bytesNeeded - bytesCopied);
            if (bytesCopied == 0) flags = AUDCLNT_BUFFERFLAGS_SILENT;
        }

        m_render->ReleaseBuffer(framesAvailable, flags);
        std::this_thread::sleep_for(std::chrono::milliseconds(5));
    }

    if (hTask) AvRevertMmThreadCharacteristics(hTask);
}
