#pragma once

#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>
#include <mmdeviceapi.h>
#include <wrl/client.h>   // ComPtr — automatic COM reference counting

#include <string>
#include <vector>

// Represents one active Windows audio output endpoint.
struct AudioDevice {
    std::wstring id;            // Unique endpoint GUID string (used to open streams)
    std::wstring friendlyName;  // Display name, e.g. "Sony WH-1000XM5 (Bluetooth)"
    bool         isDefault;     // True if currently the Windows default playback device
    int          sampleRateHz;  // Shared-mode mix sample rate, e.g. 48000
    int          channelCount;  // Shared-mode mix channel count
    int          bitsPerSample; // Shared-mode mix bit depth when reported
    int          volumePercent; // Current endpoint master volume, 0-100
    int          formFactor;    // Windows EndpointFormFactor enum value
    std::wstring category;      // Friendly form factor name, e.g. "Speakers"
    std::wstring iconPath;      // Windows icon resource path, if exposed
};

// Lists all active render (output) endpoints via WASAPI.
//
// Prerequisites:
//   - CoInitializeEx(nullptr, COINIT_MULTITHREADED) must be called before
//     constructing this object (done once in main()).
//   - The Virtual Audio Driver must be installed and active for it to appear
//     in the returned list.
//
class DeviceEnumerator {
public:
    // Throws std::runtime_error if COM object creation fails.
    DeviceEnumerator();

    // Returns all DEVICE_STATE_ACTIVE render endpoints.
    // Call this again after a device plug/unplug event to refresh.
    std::vector<AudioDevice> GetRenderDevices() const;

    // Looks up a device by its endpoint ID (AudioDevice::id from
    // GetRenderDevices()). Returns nullptr (empty ComPtr) if not found.
    Microsoft::WRL::ComPtr<IMMDevice> GetDeviceById(const std::wstring& id) const;

private:
    Microsoft::WRL::ComPtr<IMMDeviceEnumerator> m_enumerator;
};
