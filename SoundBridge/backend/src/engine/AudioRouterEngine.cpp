#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include "AudioRouterEngine.h"

#include "../audio/LoopbackCapture.h"
#include "../audio/MultiRenderer.h"
#include "../util/Win32Util.h"

#include <Windows.h>

#include <algorithm>
#include <chrono>
#include <cmath>
#include <cstdint>
#include <stdexcept>

// ---------------------------------------------------------------------------
namespace {

constexpr float Pi = 3.14159265358979323846f;

class ComApartment {
public:
    ComApartment() {
        m_hr = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
        if (FAILED(m_hr) && m_hr != RPC_E_CHANGED_MODE)
            throw std::runtime_error("CoInitializeEx failed");
    }
    ~ComApartment() { if (SUCCEEDED(m_hr)) CoUninitialize(); }
private:
    HRESULT m_hr = E_FAIL;
};

float CalculatePeakLevel(const std::vector<BYTE>& buf, const WAVEFORMATEX* fmt) {
    if (buf.empty() || !fmt) return 0.0f;
    float peak = 0.0f;
    if (fmt->wBitsPerSample == 32) {
        const float* s = reinterpret_cast<const float*>(buf.data());
        size_t n = buf.size() / sizeof(float);
        for (size_t i = 0; i < n; ++i)
            peak = std::max(peak, std::fabs(s[i]));
    } else if (fmt->wBitsPerSample == 16) {
        const int16_t* s = reinterpret_cast<const int16_t*>(buf.data());
        size_t n = buf.size() / sizeof(int16_t);
        for (size_t i = 0; i < n; ++i)
            peak = std::max(peak, std::fabs(s[i] / 32768.0f));
    }
    return std::min(peak, 1.0f);
}

// Generate 3 × 800 Hz sine-wave beeps (200ms on, 100ms silence between).
// Format must be 32-bit float PCM; other formats produce silent buffers
// (tone generation for integer PCM can be added later if needed).
std::vector<uint8_t> GenerateCalibrationBeeps(
    uint32_t sampleRate,
    uint32_t bitsPerSample,
    uint32_t channels,
    uint32_t blockAlign)
{
    const float  freq       = 800.0f;
    const float  amplitude  = 0.45f;   // -7 dBFS — audible but not harsh
    const int    beepMs     = 200;
    const int    gapMs      = 100;
    const int    numBeeps   = 3;

    auto msToFrames = [&](int ms) {
        return static_cast<size_t>(sampleRate) * ms / 1000;
    };

    size_t beepFrames = msToFrames(beepMs);
    size_t gapFrames  = msToFrames(gapMs);
    size_t totalFrames = numBeeps * beepFrames + (numBeeps - 1) * gapFrames;
    size_t totalBytes  = totalFrames * blockAlign;

    std::vector<uint8_t> buffer(totalBytes, 0);

    if (bitsPerSample == 32 && channels > 0 && blockAlign > 0) {
        float* out = reinterpret_cast<float*>(buffer.data());
        size_t frame = 0;

        for (int b = 0; b < numBeeps; ++b) {
            for (size_t i = 0; i < beepFrames; ++i, ++frame) {
                float sample = amplitude *
                    std::sin(2.0f * Pi * freq * i / sampleRate);
                for (uint32_t ch = 0; ch < channels; ++ch)
                    out[frame * channels + ch] = sample;
            }
            if (b < numBeeps - 1)
                frame += gapFrames;   // already zero-filled
        }
    }
    return buffer;
}

} // namespace

// ---------------------------------------------------------------------------
AudioRouterEngine::~AudioRouterEngine() { Stop(); }

// ---------------------------------------------------------------------------
std::vector<AudioDevice> AudioRouterEngine::GetRenderDevices() const {
    ComApartment com;
    DeviceEnumerator enumerator;
    return enumerator.GetRenderDevices();
}

// ---------------------------------------------------------------------------
void AudioRouterEngine::StoreLastError(const std::string& msg) {
    std::lock_guard<std::mutex> lock(m_configMutex);
    m_lastError = msg;
}

std::string AudioRouterEngine::GetLastError() const {
    std::lock_guard<std::mutex> lock(m_configMutex);
    return m_lastError;
}

// ---------------------------------------------------------------------------
void AudioRouterEngine::SetSourceDevice(std::wstring deviceId) {
    if (IsRunning())
        throw std::runtime_error("Cannot change source device while routing is active. Call Stop() first.");
    std::lock_guard<std::mutex> lock(m_configMutex);
    m_sourceDeviceId = std::move(deviceId);
    m_lastError.clear();
}

void AudioRouterEngine::SetOutputDevices(std::vector<std::wstring> deviceIds) {
    if (IsRunning())
        throw std::runtime_error("Cannot change output devices while routing is active. Call Stop() first.");
    std::lock_guard<std::mutex> lock(m_configMutex);
    m_outputDeviceIds = std::move(deviceIds);
    m_lastError.clear();
}

void AudioRouterEngine::SetOutputVolume(const std::wstring& deviceId, float gain) {
    std::lock_guard<std::mutex> lock(m_configMutex);
    m_outputVolumes[deviceId] = std::max(0.0f, std::min(1.0f, gain));
}

void AudioRouterEngine::SetOutputDelayMs(const std::wstring& deviceId, int ms) {
    std::lock_guard<std::mutex> lock(m_configMutex);
    m_outputDelays[deviceId] = std::max(0, std::min(ms, 2000));
}

// ---------------------------------------------------------------------------
void AudioRouterEngine::Start() {
    if (IsRunning()) return;

    std::wstring sourceId;
    std::vector<std::wstring>    outputIds;
    std::map<std::wstring,float> volumes;
    std::map<std::wstring,int>   delays;
    {
        std::lock_guard<std::mutex> lock(m_configMutex);
        sourceId  = m_sourceDeviceId;
        outputIds = m_outputDeviceIds;
        volumes   = m_outputVolumes;
        delays    = m_outputDelays;
        m_lastError.clear();
    }

    if (sourceId.empty())
        throw std::runtime_error("Source device is not configured.");
    if (outputIds.empty())
        throw std::runtime_error("At least one output device must be configured.");

    m_running          = true;
    m_peakLevel        = 0.0f;
    m_activeOutputCount = 0;
    m_worker = std::thread(
        &AudioRouterEngine::RoutingLoop, this,
        std::move(sourceId),
        std::move(outputIds),
        std::move(volumes),
        std::move(delays));
}

void AudioRouterEngine::Stop() {
    m_running = false;
    if (m_worker.joinable()) m_worker.join();
    m_peakLevel         = 0.0f;
    m_activeOutputCount = 0;
    m_sampleRate        = 0;
    m_bitsPerSample     = 0;
    m_channels          = 0;
    m_blockAlign        = 0;
    {
        std::lock_guard<std::mutex> lock(m_calibMutex);
        m_calibBuffer.clear();
    }
}

void AudioRouterEngine::UpdateRouting(
    std::wstring               sourceDeviceId,
    std::vector<std::wstring>  outputDeviceIds)
{
    Stop();
    {
        std::lock_guard<std::mutex> lock(m_configMutex);
        m_sourceDeviceId  = std::move(sourceDeviceId);
        m_outputDeviceIds = std::move(outputDeviceIds);
        m_lastError.clear();
    }
    Start();
}

// ---------------------------------------------------------------------------
void AudioRouterEngine::PlayCalibrationTone() {
    if (!IsRunning())
        throw std::runtime_error("Routing is not active. Start routing before playing calibration beeps.");

    uint32_t sr  = m_sampleRate.load();
    uint32_t bps = m_bitsPerSample.load();
    uint32_t ch  = m_channels.load();
    uint32_t ba  = m_blockAlign.load();

    if (sr == 0)
        throw std::runtime_error("Audio format not yet initialised. Wait a moment and try again.");

    auto beeps = GenerateCalibrationBeeps(sr, bps, ch, ba);

    std::lock_guard<std::mutex> lock(m_calibMutex);
    m_calibBuffer = std::move(beeps);
}

// ---------------------------------------------------------------------------
void AudioRouterEngine::RoutingLoop(
    std::wstring               sourceDeviceId,
    std::vector<std::wstring>  outputDeviceIds,
    std::map<std::wstring,float> volumes,
    std::map<std::wstring,int>   delays)
{
    try {
        ComApartment com;
        DeviceEnumerator enumerator;

        // ---- Open source device ----------------------------------------
        auto sourceDevice = enumerator.GetDeviceById(sourceDeviceId);
        if (!sourceDevice)
            throw std::runtime_error("Could not open source device: " + ToUtf8(sourceDeviceId));

        LoopbackCapture capture(sourceDevice);
        const WAVEFORMATEX* format = capture.GetFormat();

        // Store format so PlayCalibrationTone can generate matching PCM
        m_sampleRate    = format->nSamplesPerSec;
        m_bitsPerSample = format->wBitsPerSample;
        m_channels      = format->nChannels;
        m_blockAlign    = format->nBlockAlign;

        // ---- Build renderer for each output device ----------------------
        MultiRenderer renderer;
        auto allDevices = enumerator.GetRenderDevices();

        for (const auto& outputId : outputDeviceIds) {
            auto outputDevice = enumerator.GetDeviceById(outputId);
            if (!outputDevice) continue;

            auto it = std::find_if(allDevices.begin(), allDevices.end(),
                [&](const AudioDevice& d) { return d.id == outputId; });

            std::wstring outputName = (it != allDevices.end()) ? it->friendlyName : outputId;

            float gain    = 1.0f;
            int   delayMs = 0;
            auto vit = volumes.find(outputId);  if (vit != volumes.end()) gain    = vit->second;
            auto dit = delays.find(outputId);   if (dit != delays.end())  delayMs = dit->second;

            renderer.AddDevice(outputDevice, outputName, format, gain, delayMs);
        }

        if (renderer.DeviceCount() == 0)
            throw std::runtime_error("No output devices could be opened. Check device availability.");

        m_activeOutputCount = renderer.DeviceCount();
        capture.Start();

        std::vector<BYTE> buffer;

        while (m_running.load()) {
            // ---- Inject calibration tone if one is pending -------------
            {
                std::lock_guard<std::mutex> cLock(m_calibMutex);
                if (!m_calibBuffer.empty()) {
                    renderer.PushAudio(m_calibBuffer.data(), m_calibBuffer.size());
                    m_calibBuffer.clear();
                }
            }

            // ---- Read captured audio and fan out -----------------------
            buffer.clear();
            size_t bytes = capture.ReadAvailable(buffer);
            if (bytes > 0) {
                renderer.PushAudio(buffer.data(), bytes);
                m_peakLevel = CalculatePeakLevel(buffer, format);
            }

            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }

        capture.Stop();

    } catch (const std::exception& e) {
        StoreLastError(e.what());
        m_running           = false;
        m_peakLevel         = 0.0f;
        m_activeOutputCount = 0;
        m_sampleRate        = 0;
    } catch (...) {
        StoreLastError("Unknown error in routing loop.");
        m_running           = false;
        m_peakLevel         = 0.0f;
        m_activeOutputCount = 0;
        m_sampleRate        = 0;
    }
}
