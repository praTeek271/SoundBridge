#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>
#include <conio.h>

#include <algorithm>
#include <cctype>
#include <chrono>
#include <cmath>
#include <cstdint>
#include <iostream>
#include <limits>
#include <sstream>
#include <string>
#include <thread>
#include <vector>

#include "audio/DeviceEnumerator.h"
#include "audio/LoopbackCapture.h"
#include "audio/MultiRenderer.h"
#include "util/Win32Util.h"

namespace {

// Rough peak level in [0,1], used only for the live VU meter below.
// WASAPI mix formats are virtually always 32-bit float on modern Windows;
// 16-bit PCM is handled as a fallback. Any other format simply reports 0
// (the meter stays empty, but audio still routes correctly).
float PeakLevel(const std::vector<BYTE>& buf, const WAVEFORMATEX* fmt) {
    if (buf.empty()) return 0.0f;
    float peak = 0.0f;

    if (fmt->wBitsPerSample == 32) {
        const float* samples = reinterpret_cast<const float*>(buf.data());
        size_t count = buf.size() / sizeof(float);
        for (size_t i = 0; i < count; ++i)
            peak = std::max(peak, std::fabs(samples[i]));
    } else if (fmt->wBitsPerSample == 16) {
        const int16_t* samples = reinterpret_cast<const int16_t*>(buf.data());
        size_t count = buf.size() / sizeof(int16_t);
        for (size_t i = 0; i < count; ++i)
            peak = std::max(peak, std::fabs(samples[i] / 32768.0f));
    }
    return std::min(peak, 1.0f);
}

void PrintMeter(float level, size_t numOutputs) {
    constexpr int width = 30;
    int filled = static_cast<int>(level * width);
    std::cout << "\r[";
    for (int i = 0; i < width; ++i) std::cout << (i < filled ? '#' : '-');
    std::cout << "] -> " << numOutputs << " device(s)   " << std::flush;
}

// Parses a comma-separated list of 1-based indices, e.g. "1, 3" -> {1, 3}.
std::vector<int> ParseIndexList(std::string line) {
    line.erase(std::remove_if(line.begin(), line.end(),
                               [](unsigned char c) { return std::isspace(c); }),
               line.end());

    std::vector<int> result;
    std::stringstream ss(line);
    std::string token;
    while (std::getline(ss, token, ',')) {
        if (token.empty()) continue;
        try {
            result.push_back(std::stoi(token));
        } catch (...) {
            // ignore malformed tokens
        }
    }
    return result;
}

} // namespace

int main() {
    SetConsoleOutputCP(CP_UTF8);

    if (FAILED(CoInitializeEx(nullptr, COINIT_MULTITHREADED))) {
        std::cerr << "CoInitializeEx failed\n";
        return 1;
    }

    std::cout << "MultiOutput Audio Router v0.2 (M2+M3: capture + fan-out)\n";
    std::cout << "============================================================\n\n";

    int exitCode = 0;

    try {
        DeviceEnumerator enumerator;
        auto devices = enumerator.GetRenderDevices();

        if (devices.empty()) {
            std::cout << "No output devices found.\n";
            CoUninitialize();
            return 1;
        }

        std::cout << "Available output devices:\n\n";
        for (size_t i = 0; i < devices.size(); ++i) {
            std::cout << "  [" << (i + 1) << "] " << ToUtf8(devices[i].friendlyName);
            if (devices[i].isDefault) std::cout << "  <- DEFAULT";
            std::cout << "\n";
        }

        // ---- Pick source device ----------------------------------------
        std::cout << "\nSelect the SOURCE device to capture from\n";
        std::cout << "(this should be the device set as Windows default\n";
        std::cout << "output - e.g. the Virtual Audio Driver once installed): ";

        int sourceIdx = 0;
        std::cin >> sourceIdx;
        std::cin.ignore(std::numeric_limits<std::streamsize>::max(), '\n');

        if (sourceIdx < 1 || sourceIdx > static_cast<int>(devices.size())) {
            std::cerr << "Invalid selection.\n";
            CoUninitialize();
            return 1;
        }

        // ---- Pick output devices ----------------------------------------
        std::cout << "\nSelect OUTPUT devices to send audio to.\n";
        std::cout << "Enter numbers separated by commas (e.g. 1,2): ";

        std::string line;
        std::getline(std::cin, line);
        std::vector<int> outputIdx = ParseIndexList(line);

        if (outputIdx.empty()) {
            std::cerr << "No valid output devices selected.\n";
            CoUninitialize();
            return 1;
        }

        // ---- Set up capture ----------------------------------------------
        auto sourceDevice = enumerator.GetDeviceById(devices[sourceIdx - 1].id);
        if (!sourceDevice) throw std::runtime_error("Could not open source device");

        LoopbackCapture capture(sourceDevice);
        const WAVEFORMATEX* fmt = capture.GetFormat();

        std::cout << "\nCapturing from: " << ToUtf8(devices[sourceIdx - 1].friendlyName) << "\n";
        std::cout << "Source format: " << fmt->nSamplesPerSec << " Hz, "
                  << fmt->wBitsPerSample << "-bit, " << fmt->nChannels << " ch\n\n";

        // ---- Set up renderers ----------------------------------------------
        MultiRenderer renderer;
        for (int idx : outputIdx) {
            if (idx < 1 || idx > static_cast<int>(devices.size())) {
                std::cerr << "[WARN] Skipping invalid index " << idx << "\n";
                continue;
            }
            const auto& d = devices[idx - 1];
            auto dev = enumerator.GetDeviceById(d.id);
            if (!dev) {
                std::cerr << "[WARN] Could not open device: " << ToUtf8(d.friendlyName) << "\n";
                continue;
            }
            if (renderer.AddDevice(dev, d.friendlyName, fmt)) {
                std::cout << "  + Routing to: " << ToUtf8(d.friendlyName) << "\n";
            }
        }

        if (renderer.DeviceCount() == 0) {
            std::cerr << "\nNo output devices could be opened. Exiting.\n";
            CoUninitialize();
            return 1;
        }

        // ---- Run -------------------------------------------------------
        capture.Start();
        std::cout << "\nRouting started. Play some audio on the source device.\n";
        std::cout << "Press Enter to stop.\n\n";

        std::vector<BYTE> buffer;
        while (true) {
            if (_kbhit()) {
                int c = _getch();
                if (c == '\r' || c == '\n') break;
            }

            buffer.clear();
            size_t bytes = capture.ReadAvailable(buffer);
            if (bytes > 0) {
                renderer.PushAudio(buffer.data(), bytes);
                PrintMeter(PeakLevel(buffer, fmt), renderer.DeviceCount());
            }

            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }

        std::cout << "\n\nStopping...\n";
        capture.Stop();

    } catch (const std::exception& e) {
        std::cerr << "\n[ERROR] " << e.what() << "\n";
        exitCode = 1;
    }

    CoUninitialize();
    if (exitCode == 0) std::cout << "Done.\n";
    return exitCode;
}