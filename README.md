# Essentials Plugin Template (c) 2020

## License

Provided under MIT license

## Overview

Fork this repo when creating a new plugin for Essentials. For more information about plugins, refer to the Essentials Wiki [Plugins](https://github.com/PepperDash/Essentials/wiki/Plugins) article.

This repo contains example classes for the three main categories of devices:
* `EssentialsPluginTemplateDevice`: Used for most third party devices which require communication over a streaming mechanism such as a Com port, TCP/SSh/UDP socket, CEC, etc
* `EssentialsPluginTemplateLogicDevice`:  Used for devices that contain logic, but don't require any communication with third parties outside the program
* `EssentialsPluginTemplateCrestronDevice`:  Used for devices that represent a piece of Crestron hardware

There are matching factory classes for each of the three categories of devices.  The `EssentialsPluginTemplateConfigObject` should be used as a template and modified for any of the categories of device.  Same goes for the `EssentialsPluginTemplateBridgeJoinMap`.

This also illustrates how a plugin can contain multiple devices.

## Cloning Instructions

After forking this repository into your own GitHub space, you can create a new repository using this one as the template.  Then you must install the necessary dependencies as indicated below.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
Alternatively, you can simply run the `GetPackages.bat` file.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.

### Instructions for Renaming Solution and Files

See the Task List in Visual Studio for a guide on how to start using the template.  There is extensive inline documentation and examples as well.

For renaming instructions in particular, see the XML `remarks` tags on class definitions

## Build Instructions (PepperDash Internal) 

## Generating Nuget Package 

In the solution folder is a file named "PDT.EssentialsPluginTemplate.nuspec" 

1. Rename the file to match your plugin solution name 
2. Edit the file to include your project specifics including
    1. <id>PepperDash.Essentials.Plugin.MakeModel</id> Convention is to use the prefix "PepperDash.Essentials.Plugin" and include the MakeModel of the device. 
    2. <projectUrl>https://github.com/PepperDash/EssentialsPluginTemplate</projectUrl> Change to your url to the project repo

There is no longer a requirement to adjust workflow files for nuget generation for private and public repositories.  This is now handled automatically in the workflow.

__If you do not make these changes to the nuspec file, the project will not generate a nuget package__
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.12.8
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "oneBeyondAutomateVx",
    "group": "Group",
    "properties": {
        "control": "SampleValue"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- oneBeyondAutomateVx
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Description |
|------|-------------|
| 1 | Successfully authenticated to Automate VX server |
| 2 | Pulse input to enable auto switch. FB high when enabled |
| 3 | Pulse input to disable auto switch. FB high when disabled |
| 4 | Pulse input to start recording. FB high when recording |
| 5 | Pulse input to pause recording |
| 6 | Pulse input to stop recording. FB high when not recording |
| 7 | Pulse input to start ISO Recording. FB high when enabled |
| 8 | Pulse input to stop ISO Recording. FB high when disabled |
| 9 | Pulse input to start stream. FB high when enabled |
| 10 | Pulse input to stop stream. FB high when disabled |
| 11 | Pulse input to start stream. FB high when enabled |
| 12 | Pulse input to stop stream. FB high when disabled |
| 13 | Pulse input call the home shot in the system |
| 14 | Pulse input call the home shot in the system |
| 15 | Pulse input call the home shot in the system |
| 16 | Gets current status |
| 17 | Gets current status |
| 18 | Gets current status |
| 19 | Gets current status |
| 20 | Gets current status |
| 21 | Gets current status |
| 22 | Gets available layouts |
| 23 | Gets current status |
| 24 | Gets available cameras |
| 25 | Recalls the preset specified by the CameraPresetToRecall and CameraToRecallPresetOn analog joins. |
| 26 | Trigger copying of files. |
| 27 | Pulses when copy is successful. |
| 27 | Set High to have files deleted after copy files operation |

#### Analogs

| Join | Description |
|------|-------------|
| 1 | Specifies the desired layout and provides feedback for current layout. Valid values 1-26 correspond to A-Z |
| 2 | Reports the number of layouts stored on the Automate server |
| 3 | Specifies the desired room config and provides feedback for current config. Valid values 1-99. |
| 4 | Forces the desired room config. Valid values 1-99. |
| 5 | Reports the number of configs stored on the Automate server |
| 6 | Specifies the desired camera and provides feedback for current camera. |
| 7 | Reports the number of cameras available on the Automate server |
| 8 | Specifies preset for the current camera. |
| 9 | Specifies preset to recall on CameraToRecallPresetOn. Pulse RecallCameraPreset join to execute. |
| 10 | Specifies camera to recall preset on specified by CameraPresetToRecall.. Pulse RecallCameraPreset join to execute. |
| 11 | Reports the remaining storage space in GB |
| 12 | Reports the total storage space in GB |

#### Serials

| Join | Description |
|------|-------------|
| 1 | Error message from device |
| 2 | Success message from device |
| 3 | The location to copy files to |
| 3 | The location for the log file for the copy files operation |
| 5 | Name of each layout [A-Z] |
| 101 | Name of each room config [1-99] |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- ID
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- JoinMapBaseAdvanced
- EssentialsBridgeableDevice
- EventArgs
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void GetToken()
- public void ClearToken()
- public void GetAutoSwitchStatus()
- public void SetAutoSwitch(bool state)
- public void GetRecordStatus()
- public void SetRecord(eRecordOperation operation)
- public void GetIsoRecordStatus()
- public void SetIsoRecord(bool state)
- public void GetStreamStatus()
- public void SetStream(bool state)
- public void GetOutputStatus()
- public void SetOutput(bool state)
- public void GetLayouts()
- public void GetLayoutStatus()
- public void SetLayout(string layout)
- public void GetRoomConfigStatus()
- public void GetRoomConfigs()
- public void SetRoomConfig(uint id)
- public void ForceSetRoomConfig(uint id)
- public void GoHome()
- public void GetCameras()
- public void GetCameraStatus()
- public void SetCamera(uint address)
- public void SetCameraPreset(uint camId, uint presetId)
- public void SaveCameraPreset(uint camId, uint presetId)
- public void ImportCameraPresets()
- public void ExportCameraPresets()
- public void CopyFiles(string dest, string logDest, bool delete)
- public void GetStorageSpaceAvailable()
- public void GetRecordingSpaceAvailable()
- public void SetSleep()
- public void SetWake()
- public void Restart()
- public void SetCloseWirecast()
- public void GetScenarios()
- public void GetScenarioStatus()
- public void SetScenario(uint id)
<!-- END Public Methods -->
