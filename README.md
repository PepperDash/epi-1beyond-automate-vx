# 1Beyond Automate VX (c) 2025

## License

Provided under MIT license

<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 1.16.1
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "OneBeyondAutomateVx",
    "group": "Group",
    "properties": {
        "control": "SampleValue"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->

<!-- END Supported Types -->
<!-- START Join Maps -->

<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IRestfulComms
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsBridgeableDevice
- JoinMapBaseAdvanced
- RootResponse
- EventArgs
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void ClearToken()
- public void GetToken()
- public void Poll()
- public void GetAutoSwitchStatus()
- public void SetAutoSwitch(bool state)
- public void GetRecordStatus()
- public void SetRecord(ERecordOperation operation)
- public void GetIsoRecordStatus()
- public void SetIsoRecord(bool state)
- public void GetStreamStatus()
- public void SetStream(bool state)
- public void GetOutputStatus()
- public void SetOutput(bool state)
- public void GetLayouts()
- public void GetLayoutStatus()
- public void SetLayout(ushort layout)
- public void GetRoomConfigStatus()
- public void GetRoomConfigs()
- public void SetRoomConfig(uint configId)
- public void ForceSetRoomConfig(uint configId)
- public void GoHome()
- public void GetCameras()
- public void GetCameraStatus()
- public void SetCamera(uint cameraAddress)
- public void SetCameraPreset(uint camId, uint presetId)
- public void SaveCameraPreset(uint camId, uint presetId)
- public void ImportCameraPresets()
- public void ExportCameraPresets()
- public void CopyFiles(string dest, string logDest, bool delete)
- public void GetStorageSpaceAvailable(string driveLetters)
- public void GetRecordingSpaceAvailable()
- public void SetSleep()
- public void SetWake()
- public void Restart()
- public void SetCloseWirecast()
- public void GetScenarios()
- public void GetScenarioStatus()
- public void SetScenario(uint scenarioId)
- public void SendRequest(string requestType, string path, string content)
- public void SendRequest(RequestType requestType, string path, string content)
- public void SendRequest(string requestType, string path, string content)
- public void SendRequest(RequestType requestType, string path, string content)
<!-- END Public Methods -->

<!-- START Bool Feedbacks -->
### Bool Feedbacks

- LoginSuccessfulFeedback
- AutoSwitchIsOnFeedback
- RecordIsOnFeedback
- IsoRecordIsOnFeedback
- StreamIsOnFeedback
- OutputIsOnFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- ResponseCodeFeedback
- CameraAddressFeedback
- CamerasCountFeedback
- LayoutsCountFeedback
- CurrentLayoutIdFeedback
- RoomConfigsCountFeedback
- CurrentRoomConfigIdFeedback
- ScenariosCountFeedback
- CurrentScenarioIdFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- ResponseContentFeedback
- ResponseSuccessMessageFeedback
- ResponseErrorMessageFeedback
- CurrentLayoutNameFeedback
- CurrentRoomConfigNameFeedback
- CurrentScenarioNameFeedback
<!-- END String Feedbacks -->
