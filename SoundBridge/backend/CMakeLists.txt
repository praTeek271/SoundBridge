cmake_minimum_required(VERSION 3.20)
project(SoundBridgeNative VERSION 0.1.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Windows only — WASAPI is Win32
if(NOT WIN32)
    message(FATAL_ERROR "SoundBridge.Native only builds on Windows")
endif()
if(NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(FATAL_ERROR "SoundBridge.Native requires a 64-bit (x64) build")
endif()

# ---------------------------------------------------------------------------
# FTXUI — terminal UI
# NOTE: Disabled for now (Milestones 1-3 don't need it). It pulls from GitHub
# during cmake configure, which is a common source of "configure failed ->
# Target missing executable" errors in Visual Studio if network/git has any
# hiccup. We'll re-enable this in Milestone 4 when we build the TUI.
# ---------------------------------------------------------------------------
# include(FetchContent)
# FetchContent_Declare(ftxui
#     GIT_REPOSITORY https://github.com/ArthurSonzogni/FTXUI
#     GIT_TAG        v5.0.0
#     GIT_SHALLOW    TRUE
# )
# FetchContent_MakeAvailable(ftxui)

# ---------------------------------------------------------------------------
# Native audio core
# ---------------------------------------------------------------------------
add_library(SoundBridgeNative STATIC
    src/audio/DeviceEnumerator.cpp
    src/audio/LoopbackCapture.cpp
    src/audio/RenderStream.cpp
    src/audio/MultiRenderer.cpp
    src/engine/AudioRouterEngine.cpp
)

target_include_directories(SoundBridgeNative PUBLIC src)

target_link_libraries(SoundBridgeNative PUBLIC
    # ftxui::screen
    # ftxui::dom
    # ftxui::component
    ole32       # COM — CoInitializeEx, CoCreateInstance
    uuid        # CLSID / IID symbol definitions
    avrt        # AvSetMmThreadCharacteristics (real-time audio thread priority)
)

# UTF-8 source files, clean warnings
target_compile_options(SoundBridgeNative PRIVATE /utf-8 /W4 /WX-)

# ---------------------------------------------------------------------------
# C ABI bridge for managed GUI integration
# ---------------------------------------------------------------------------
add_library(SoundBridgeBridge SHARED
    src/bridge/SoundBridgeBridge.cpp
)

target_link_libraries(SoundBridgeBridge PRIVATE SoundBridgeNative)
target_compile_options(SoundBridgeBridge PRIVATE /utf-8 /W4 /WX-)

# ---------------------------------------------------------------------------
# Legacy console harness
# ---------------------------------------------------------------------------
add_executable(MultiOutputAudio
    src/main.cpp
)

target_link_libraries(MultiOutputAudio PRIVATE SoundBridgeNative)
target_compile_options(MultiOutputAudio PRIVATE /utf-8 /W4 /WX-)
