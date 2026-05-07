REM This batch file copies all files relevant for the mod 'ElectricityLamps' to the game's mod folder.

@ECHO off

REM Windows 10 x64 method to get admin rights
:-------------------------------------
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
IF '%errorlevel%' NEQ '0' (
    ECHO Requesting administrative privileges...
    GOTO UACPrompt
) ELSE ( GOTO gotAdmin )

:UACPrompt
    ECHO Set UAC = CreateObject^("Shell.Application"^) > "%temp%\getadmin.vbs"
    SET params = %*:"=""
    ECHO UAC.ShellExecute "cmd.exe", "/c %~s0 %params%", "", "runas", 1 >> "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    DEL "%temp%\getadmin.vbs"
    EXIT /B

:gotAdmin
    pushd "%CD%"
    CD /D "%~dp0"
:--------------------------------------

REM ------------------------------------------------------------------------
REM Configurable variables (%~dp0 is a system variable containing the directory path to THIS bat file)
REM ------------------------------------------------------------------------
set "logFile=00_Copy.txt"
set "modFolderName=ElectricityLamps"
set "modFolder=%~dp0tmp mods\%modFolderName%"
set "dllSource=%~dp0bin\Debug\ColoredLights_Port.dll"

REM ------------------------------------------------------------------------
REM Try to detect Steam path dynamically
REM ------------------------------------------------------------------------
for /f "tokens=2* skip=2" %%a in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath 2^>nul') do set "SteamPath=%%b"
if defined SteamPath (
    set "ModsPath=%SteamPath%\steamapps\common\7 Days To Die\Mods"
) else (
    set "ModsPath=C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\Mods"
)

REM ------------------------------------------------------------------------
REM Initialize log
REM ------------------------------------------------------------------------
type nul >"%logFile%"
echo ==================================================================================================== >>"%logFile%"
echo SteamPath: %SteamPath% >>"%logFile%"
echo modFolder: %modFolder% >>"%logFile%"
echo ModsPath: %ModsPath% >>"%logFile%"
echo dllSource: %dllSource% >>"%logFile%"
echo ==================================================================================================== >>"%logFile%"
echo. >>"%logFile%"

REM ------------------------------------------------------------------------
REM Copy freshly built DLL into mod source folder
REM ------------------------------------------------------------------------
echo Copying DLL from Visual Studio build output...
echo Copying DLL... >>"%logFile%"
if exist "%dllSource%" (
    copy /Y "%dllSource%" "%modFolder%\ColoredLights_Port.dll" >>"%logFile%"
    echo DLL copied successfully. >>"%logFile%"
	echo. >>"%logFile%"
) else (
    echo WARNING: DLL not found at: %dllSource% >>"%logFile%"
    echo WARNING: DLL not found at: %dllSource%
    echo Make sure you have built the project in Visual Studio first.
    pause
	exit
)

REM ------------------------------------------------------------------------
REM Copy mod folder to game's mod folder
REM ------------------------------------------------------------------------
robocopy "%modFolder%" "%ModsPath%\%modFolderName%" /mir /XF *.bak /log+:"%logFile%" /r:10 /w:5

REM ------------------------------------------------------------------------
REM Copy backup save folder to game's save folder
REM ------------------------------------------------------------------------
set "bkpFolder=%appdata%\7DaysToDie\Saves backup\Old Honihebu County"
set "saveFolder=%appdata%\7DaysToDie\Saves\Old Honihebu County"

if exist "%bkpFolder%" if exist "%saveFolder%" (
	echo ==================================================================================================== >>"%logFile%"
	echo bkpFolder: %bkpFolder% >>"%logFile%"
	echo saveFolder: %saveFolder% >>"%logFile%"
	echo ==================================================================================================== >>"%logFile%"
	robocopy "%bkpFolder%" "%saveFolder%" /mir /XF *.bak /log+:"%logFile%" /r:10 /w:5
) else (
	if not exist "%bkpFolder%" (
		echo WARNING: Backup folder does not exist at: %bkpFolder% >>"%logFile%"
		echo Check your bakup folder.  >>"%logFile%"
	)
	if not exist "%saveFolder%" (
		echo WARNING: Save folder does not exist at: %saveFolder% >>"%logFile%"
		echo Check your save folder.  >>"%logFile%"
	)
	pause
	exit
)

REM Launch 7 Days to Die via Steam (App ID 251570)
echo Launching 7 Days to Die...
start "" "steam://rungameid/251570"
