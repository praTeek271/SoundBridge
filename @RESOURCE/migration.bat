@echo off
setlocal EnableExtensions

set "ROOT=%~dp0"
set "TARGET=SoundBridge"

call :EnsureDir "%TARGET%"
call :EnsureDir "%TARGET%\app"
call :EnsureDir "%TARGET%\app\assets"
call :EnsureDir "%TARGET%\app\Models"
call :EnsureDir "%TARGET%\app\Services"
call :EnsureDir "%TARGET%\app\ViewModels"
call :EnsureDir "%TARGET%\app\Views"
call :EnsureDir "%TARGET%\backend"
call :EnsureDir "%TARGET%\backend\src"
call :EnsureDir "%TARGET%\backend\src\audio"
call :EnsureDir "%TARGET%\backend\src\bridge"
call :EnsureDir "%TARGET%\backend\src\engine"
call :EnsureDir "%TARGET%\backend\src\util"
call :EnsureDir "%TARGET%\backend\scripts"
call :EnsureDir "%TARGET%\docs"
call :EnsureDir "%TARGET%\docs\prototypes"
call :EnsureDir "%TARGET%\drivers.test"
call :EnsureDir "%TARGET%\drivers.test\Virtual.Audio.Driver.Signed.-.25.7.14"
call :EnsureDir "%TARGET%\drivers.test\Virtual.Audio.Driver.Signed.-.25.7.14\Virtual Audio Driver"
call :EnsureDir "%TARGET%\installer"
call :EnsureDir "%TARGET%\dist"

rem WPF app source files.
call :MoveFile "app\App.xaml" "%TARGET%\app\App.xaml"
call :MoveFile "app\App.xaml.cs" "%TARGET%\app\App.xaml.cs"
call :MoveFile "app\MainWindow.xaml" "%TARGET%\app\MainWindow.xaml"
call :MoveFile "app\MainWindow.xaml.cs" "%TARGET%\app\MainWindow.xaml.cs"
call :MoveFile "app\SoundBridge.Gui.csproj" "%TARGET%\app\SoundBridge.Gui.csproj"

call :MoveFile "app\assets\beep.wav" "%TARGET%\app\assets\beep.wav"
call :MoveFile "app\assets\icon.ico" "%TARGET%\app\assets\icon.ico"
call :MoveFile "app\asset\beep.wav" "%TARGET%\app\asset\beep.wav"

call :MoveFile "app\Models\AudioDeviceDescriptor.cs" "%TARGET%\app\Models\AudioDeviceDescriptor.cs"
call :MoveFile "app\Models\NavigationItem.cs" "%TARGET%\app\Models\NavigationItem.cs"

call :MoveFile "app\Services\IAudioRouterService.cs" "%TARGET%\app\Services\IAudioRouterService.cs"
call :MoveFile "app\Services\ITemplateStore.cs" "%TARGET%\app\Services\ITemplateStore.cs"
call :MoveFile "app\Services\JsonTemplateStore.cs" "%TARGET%\app\Services\JsonTemplateStore.cs"
call :MoveFile "app\Services\MockAudioRouterService.cs" "%TARGET%\app\Services\MockAudioRouterService.cs"
call :MoveFile "app\Services\NativeAudioRouterInterop.cs" "%TARGET%\app\Services\NativeAudioRouterInterop.cs"
call :MoveFile "app\Services\NativeAudioRouterService.cs" "%TARGET%\app\Services\NativeAudioRouterService.cs"
call :MoveFile "app\Services\PersistedTemplates.cs" "%TARGET%\app\Services\PersistedTemplates.cs"
call :MoveFile "app\Services\SystemDefaultAudioEndpointService.cs" "%TARGET%\app\Services\SystemDefaultAudioEndpointService.cs"

call :MoveFile "app\ViewModels\AboutViewModel.cs" "%TARGET%\app\ViewModels\AboutViewModel.cs"
call :MoveFile "app\ViewModels\CalibrationViewModel.cs" "%TARGET%\app\ViewModels\CalibrationViewModel.cs"
call :MoveFile "app\ViewModels\DashboardViewModel.cs" "%TARGET%\app\ViewModels\DashboardViewModel.cs"
call :MoveFile "app\ViewModels\DevicesViewModel.cs" "%TARGET%\app\ViewModels\DevicesViewModel.cs"
call :MoveFile "app\ViewModels\DeviceViewModel.cs" "%TARGET%\app\ViewModels\DeviceViewModel.cs"
call :MoveFile "app\ViewModels\DeviceWorkspaceViewModel.cs" "%TARGET%\app\ViewModels\DeviceWorkspaceViewModel.cs"
call :MoveFile "app\ViewModels\MainViewModel.cs" "%TARGET%\app\ViewModels\MainViewModel.cs"
call :MoveFile "app\ViewModels\RelayCommand.cs" "%TARGET%\app\ViewModels\RelayCommand.cs"
call :MoveFile "app\ViewModels\SettingsViewModel.cs" "%TARGET%\app\ViewModels\SettingsViewModel.cs"
call :MoveFile "app\ViewModels\ViewModelBase.cs" "%TARGET%\app\ViewModels\ViewModelBase.cs"

call :MoveFile "app\Views\AboutView.xaml" "%TARGET%\app\Views\AboutView.xaml"
call :MoveFile "app\Views\AboutView.xaml.cs" "%TARGET%\app\Views\AboutView.xaml.cs"
call :MoveFile "app\Views\CalibrationView.xaml" "%TARGET%\app\Views\CalibrationView.xaml"
call :MoveFile "app\Views\CalibrationView.xaml.cs" "%TARGET%\app\Views\CalibrationView.xaml.cs"
call :MoveFile "app\Views\DashboardView.xaml" "%TARGET%\app\Views\DashboardView.xaml"
call :MoveFile "app\Views\DashboardView.xaml.cs" "%TARGET%\app\Views\DashboardView.xaml.cs"
call :MoveFile "app\Views\DevicesFlowchartControl.cs" "%TARGET%\app\Views\DevicesFlowchartControl.cs"
call :MoveFile "app\Views\DevicesView.xaml" "%TARGET%\app\Views\DevicesView.xaml"
call :MoveFile "app\Views\DevicesView.xaml.cs" "%TARGET%\app\Views\DevicesView.xaml.cs"
call :MoveFile "app\Views\DeviceWorkspaceView.xaml" "%TARGET%\app\Views\DeviceWorkspaceView.xaml"
call :MoveFile "app\Views\DeviceWorkspaceView.xaml.cs" "%TARGET%\app\Views\DeviceWorkspaceView.xaml.cs"
call :MoveFile "app\Views\SettingsView.xaml" "%TARGET%\app\Views\SettingsView.xaml"
call :MoveFile "app\Views\SettingsView.xaml.cs" "%TARGET%\app\Views\SettingsView.xaml.cs"

rem Backend source files.
call :MoveFile "native\SoundBridge.Native\CMakeLists.txt" "%TARGET%\backend\CMakeLists.txt"
call :MoveFile "native\SoundBridge.Native\README.md" "%TARGET%\backend\README.md"
call :MoveFile "native\SoundBridge.Native\src\main.cpp" "%TARGET%\backend\src\main.cpp"
call :MoveFile "native\SoundBridge.Native\src\audio\DeviceEnumerator.cpp" "%TARGET%\backend\src\audio\DeviceEnumerator.cpp"
call :MoveFile "native\SoundBridge.Native\src\audio\DeviceEnumerator.h" "%TARGET%\backend\src\audio\DeviceEnumerator.h"
call :MoveFile "native\SoundBridge.Native\src\audio\LoopbackCapture.cpp" "%TARGET%\backend\src\audio\LoopbackCapture.cpp"
call :MoveFile "native\SoundBridge.Native\src\audio\LoopbackCapture.h" "%TARGET%\backend\src\audio\LoopbackCapture.h"
call :MoveFile "native\SoundBridge.Native\src\audio\MultiRenderer.cpp" "%TARGET%\backend\src\audio\MultiRenderer.cpp"
call :MoveFile "native\SoundBridge.Native\src\audio\MultiRenderer.h" "%TARGET%\backend\src\audio\MultiRenderer.h"
call :MoveFile "native\SoundBridge.Native\src\audio\RenderStream.cpp" "%TARGET%\backend\src\audio\RenderStream.cpp"
call :MoveFile "native\SoundBridge.Native\src\audio\RenderStream.h" "%TARGET%\backend\src\audio\RenderStream.h"
call :MoveFile "native\SoundBridge.Native\src\bridge\SoundBridgeBridge.cpp" "%TARGET%\backend\src\bridge\SoundBridgeBridge.cpp"
call :MoveFile "native\SoundBridge.Native\src\engine\AudioRouterEngine.cpp" "%TARGET%\backend\src\engine\AudioRouterEngine.cpp"
call :MoveFile "native\SoundBridge.Native\src\engine\AudioRouterEngine.h" "%TARGET%\backend\src\engine\AudioRouterEngine.h"
call :MoveFile "native\SoundBridge.Native\src\util\Win32Util.h" "%TARGET%\backend\src\util\Win32Util.h"
call :MoveFile "native\SoundBridge.Native\scripts\pre_install_safety.ps1" "%TARGET%\backend\scripts\pre_install_safety.ps1"
call :MoveFile "native\SoundBridge.Native\scripts\rollback_virtual_audio_driver.ps1" "%TARGET%\backend\scripts\rollback_virtual_audio_driver.ps1"

rem Documentation and prototype references.
call :MoveFile "docs\architecture.md" "%TARGET%\docs\architecture.md"
call :MoveFile "docs\blueprint.md" "%TARGET%\docs\blueprint.md"
call :MoveFile "docs\flow.md" "%TARGET%\docs\flow.md"
call :MoveFile "docs\freesound_community-public-domain-beep-sound-100267.mp3" "%TARGET%\docs\freesound_community-public-domain-beep-sound-100267.mp3"
call :MoveFile "docs\gui-fix-notes.md" "%TARGET%\docs\gui-fix-notes.md"
call :MoveFile "docs\installation.md" "%TARGET%\docs\installation.md"
call :MoveFile "docs\integration Plan.md" "%TARGET%\docs\integration Plan.md"
call :MoveFile "docs\Project.md" "%TARGET%\docs\Project.md"
call :MoveFile "application_visual_layer_prototype_v3_2.html" "%TARGET%\docs\prototypes\application_visual_layer_prototype_v3_2.html"
call :MoveFile "file_structure.txt" "%TARGET%\docs\file_structure.txt"

rem Driver test package.
call :MoveFile "Virtual.Audio.Driver.Signed.-.25.7.14\Virtual Audio Driver\VirtualAudioDriver.inf" "%TARGET%\drivers.test\Virtual.Audio.Driver.Signed.-.25.7.14\Virtual Audio Driver\VirtualAudioDriver.inf"
call :MoveFile "Virtual.Audio.Driver.Signed.-.25.7.14\Virtual Audio Driver\VirtualAudioDriver.sys" "%TARGET%\drivers.test\Virtual.Audio.Driver.Signed.-.25.7.14\Virtual Audio Driver\VirtualAudioDriver.sys"
call :MoveFile "Virtual.Audio.Driver.Signed.-.25.7.14\Virtual Audio Driver\virtualaudiodriver.cat" "%TARGET%\drivers.test\Virtual.Audio.Driver.Signed.-.25.7.14\Virtual Audio Driver\virtualaudiodriver.cat"

rem Wrapper-root metadata.
call :MoveFile ".gitignore" "%TARGET%\.gitignore"

echo.
echo Migration completed.
echo Generated folders were intentionally not moved: app\bin, app\obj, dist, native build folders, .vs.
echo Next: update project paths under SoundBridge\ and rebuild from that folder.
goto :eof

:EnsureDir
if not exist "%~1" mkdir "%~1"
goto :eof

:MoveFile
set "SRC=%~1"
set "DST=%~2"
if not exist "%SRC%" (
    echo skip missing: %SRC%
    goto :eof
)
if exist "%DST%" (
    echo skip existing: %DST%
    goto :eof
)
for %%I in ("%DST%") do if not exist "%%~dpI" mkdir "%%~dpI"
echo move: %SRC% --^> %DST%
move "%SRC%" "%DST%" >nul
goto :eof
