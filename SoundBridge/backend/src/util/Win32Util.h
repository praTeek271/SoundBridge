#pragma once
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <Windows.h>

#include <sstream>
#include <string>

// Converts a UTF-16 (Windows wide) string to UTF-8, for console output.
inline std::string ToUtf8(const std::wstring& ws) {
    if (ws.empty()) return {};
    int n = WideCharToMultiByte(CP_UTF8, 0, ws.c_str(), -1, nullptr, 0, nullptr, nullptr);
    if (n <= 0) return {};
    std::string s(static_cast<size_t>(n - 1), '\0');
    WideCharToMultiByte(CP_UTF8, 0, ws.c_str(), -1, s.data(), n, nullptr, nullptr);
    return s;
}

// Formats an HRESULT as "0xAABBCCDD" for error messages.
inline std::string HrHex(HRESULT hr) {
    std::ostringstream oss;
    oss << "0x" << std::hex << std::uppercase << static_cast<unsigned long>(hr);
    return oss.str();
}