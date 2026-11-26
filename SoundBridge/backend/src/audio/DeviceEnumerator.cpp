#include "DeviceEnumerator.h"

#include <audioclient.h>
#include <endpointvolume.h>
#include <Functiondiscoverykeys_devpkey.h>  // PKEY_Device_FriendlyName
#include <mmdeviceapi.h>
#include <propvarutil.h>
#include <algorithm>
#include <stdexcept>

namespace {

constexpr PROPERTYKEY SoundBridgePkeyAudioEndpointFormFactor = {
    { 0x1da5d803, 0xd492, 0x4edd, { 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e } },
    0
};

std::wstring FormFactorName(int formFactor) {
    switch (formFactor) {
    case RemoteNetworkDevice:          return L"Remote network";
    case Speakers:                     return L"Speakers";
    case LineLevel:                    return L"Line level";
    case Headphones:                   return L"Headphones";
    case Microphone:                   return L"Microphone";
    case Headset:                      return L"Headset";
    case Handset:                      return L"Handset";
    case UnknownDigitalPassthrough:    return L"Digital passthrough";
    case SPDIF:                        return L"SPDIF";
    case DigitalAudioDisplayDevice:    return L"HDMI / display audio";
    case UnknownFormFactor:            return L"Unknown";
    default:                           return L"Unknown";
    }
}

void ReadMixFormat(IMMDevice* device, AudioDevice& dev) {
    Microsoft::WRL::ComPtr<IAudioClient> audioClient;
    if (FAILED(device->Activate(
            __uuidof(IAudioClient),
            CLSCTX_ALL,
            nullptr,
            reinterpret_cast<void**>(audioClient.GetAddressOf())))) {
        return;
    }

    WAVEFORMATEX* mixFormat = nullptr;
    if (FAILED(audioClient->GetMixFormat(&mixFormat)) || !mixFormat) {
        return;
    }

    dev.sampleRateHz = static_cast<int>(mixFormat->nSamplesPerSec);
    dev.channelCount = static_cast<int>(mixFormat->nChannels);
    dev.bitsPerSample = static_cast<int>(mixFormat->wBitsPerSample);
    CoTaskMemFree(mixFormat);
}

void ReadEndpointVolume(IMMDevice* device, AudioDevice& dev) {
    Microsoft::WRL::ComPtr<IAudioEndpointVolume> endpointVolume;
    if (FAILED(device->Activate(
            __uuidof(IAudioEndpointVolume),
            CLSCTX_ALL,
            nullptr,
            reinterpret_cast<void**>(endpointVolume.GetAddressOf())))) {
        return;
    }

    float scalar = 0.0f;
    if (SUCCEEDED(endpointVolume->GetMasterVolumeLevelScalar(&scalar))) {
        scalar = std::clamp(scalar, 0.0f, 1.0f);
        dev.volumePercent = static_cast<int>((scalar * 100.0f) + 0.5f);
    }
}

void ReadPropertyStore(IMMDevice* device, AudioDevice& dev) {
    Microsoft::WRL::ComPtr<IPropertyStore> pProps;
    if (FAILED(device->OpenPropertyStore(STGM_READ, &pProps))) {
        return;
    }

    PROPVARIANT var;
    PropVariantInit(&var);

    if (SUCCEEDED(pProps->GetValue(PKEY_Device_FriendlyName, &var))
        && var.vt == VT_LPWSTR) {
        dev.friendlyName = var.pwszVal;
    }

    PropVariantClear(&var);
    PropVariantInit(&var);

    if (SUCCEEDED(pProps->GetValue(SoundBridgePkeyAudioEndpointFormFactor, &var))) {
        if (var.vt == VT_UI4) {
            dev.formFactor = static_cast<int>(var.ulVal);
        } else if (var.vt == VT_I4) {
            dev.formFactor = static_cast<int>(var.lVal);
        }

        dev.category = FormFactorName(dev.formFactor);
    }

    PropVariantClear(&var);
    PropVariantInit(&var);

    if (SUCCEEDED(pProps->GetValue(PKEY_DeviceClass_IconPath, &var))
        && var.vt == VT_LPWSTR) {
        dev.iconPath = var.pwszVal;
    }

    PropVariantClear(&var);
}

} // namespace

// ---------------------------------------------------------------------------
DeviceEnumerator::DeviceEnumerator() {
    HRESULT hr = CoCreateInstance(
        __uuidof(MMDeviceEnumerator),
        nullptr,
        CLSCTX_ALL,
        IID_PPV_ARGS(&m_enumerator)
    );
    if (FAILED(hr))
        throw std::runtime_error(
            "CoCreateInstance(IMMDeviceEnumerator) failed. "
            "Ensure COM is initialized with CoInitializeEx before constructing DeviceEnumerator."
        );
}

// ---------------------------------------------------------------------------
std::vector<AudioDevice> DeviceEnumerator::GetRenderDevices() const {
    std::vector<AudioDevice> result;

    // ------------------------------------------------------------------
    // 1. Grab the default device ID so we can flag it in the list
    // ------------------------------------------------------------------
    std::wstring defaultId;
    {
        Microsoft::WRL::ComPtr<IMMDevice> pDefault;
        if (SUCCEEDED(m_enumerator->GetDefaultAudioEndpoint(eRender, eConsole, &pDefault))) {
            LPWSTR rawId = nullptr;
            if (SUCCEEDED(pDefault->GetId(&rawId))) {
                defaultId = rawId;
                CoTaskMemFree(rawId);
            }
        }
    }

    // ------------------------------------------------------------------
    // 2. Enumerate all active render endpoints
    // ------------------------------------------------------------------
    Microsoft::WRL::ComPtr<IMMDeviceCollection> pCollection;
    HRESULT hr = m_enumerator->EnumAudioEndpoints(
        eRender,
        DEVICE_STATE_ACTIVE,
        &pCollection
    );
    if (FAILED(hr)) return result;

    UINT count = 0;
    pCollection->GetCount(&count);
    result.reserve(count);

    for (UINT i = 0; i < count; ++i) {
        Microsoft::WRL::ComPtr<IMMDevice> pDevice;
        if (FAILED(pCollection->Item(i, &pDevice))) continue;

        AudioDevice dev{};
        dev.volumePercent = -1;
        dev.formFactor = UnknownFormFactor;
        dev.category = FormFactorName(dev.formFactor);

        // ---- Endpoint ID ----
        LPWSTR rawId = nullptr;
        if (SUCCEEDED(pDevice->GetId(&rawId))) {
            dev.id        = rawId;
            dev.isDefault = (rawId == defaultId);
            CoTaskMemFree(rawId);
        }

        ReadPropertyStore(pDevice.Get(), dev);
        ReadMixFormat(pDevice.Get(), dev);
        ReadEndpointVolume(pDevice.Get(), dev);

        result.push_back(std::move(dev));
    }

    return result;
}

// ---------------------------------------------------------------------------
Microsoft::WRL::ComPtr<IMMDevice> DeviceEnumerator::GetDeviceById(const std::wstring& id) const {
    Microsoft::WRL::ComPtr<IMMDevice> device;
    m_enumerator->GetDevice(id.c_str(), device.GetAddressOf());
    return device;
}
