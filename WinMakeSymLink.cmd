@setlocal enableextensions
@cd /d "%~dp0"
rmdir BlocksWorld\Assets\Plugins\VoxSimPlatform & del /Q BlocksWorld\Assets\Plugins\VoxSimPlatform & mklink /D BlocksWorld\Assets\Plugins\VoxSimPlatform VoxSim\Assets\VoxSimPlatform