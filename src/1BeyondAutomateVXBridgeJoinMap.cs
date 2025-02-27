using PepperDash.Essentials.Core;

namespace OneBeyondAutomateVxEpi
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	public class OneBeyondAutomateVxBridgeJoinMap : JoinMapBaseAdvanced
	{
		#region Digital

		[JoinName("Authenticate")]
        public JoinDataComplete Authenticate = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Authenticates with Automate VX server and reports when authenticated",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

        [JoinName("AutoSwitchOn")]
		public JoinDataComplete AutoSwitchOn = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 2,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Pulse input to enable auto switch. FB high when enabled",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Digital
			});

        [JoinName("AutoSwitchOff")]
        public JoinDataComplete AutoSwitchOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to disable auto switch. FB high when disabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("RecordStart")]
        public JoinDataComplete RecordStart = new JoinDataComplete(
            new JoinData
            {
                JoinNumber =4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to start recording. FB high when recording",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("RecordPause")]
        public JoinDataComplete RecordPause = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to pause recording",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("RecordStop")]
        public JoinDataComplete RecordStop = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 6,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to stop recording. FB high when not recording",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("IsoRecordOn")]
        public JoinDataComplete IsoRecordOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 7,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to start ISO Recording. FB high when enabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("IsoRecordOff")]
        public JoinDataComplete IsoRecordOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 8,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to stop ISO Recording. FB high when disabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("StreamOn")]
        public JoinDataComplete StreamOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 9,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to start stream. FB high when enabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("StreamOff")]
        public JoinDataComplete StreamOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to stop stream. FB high when disabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("OutputOn")]
        public JoinDataComplete OutputOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to start stream. FB high when enabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("OutputOff")]
        public JoinDataComplete OutputOff = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 12,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input to stop stream. FB high when disabled",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Sleep")]
        public JoinDataComplete Sleep = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 13,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input call the home shot in the system",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("Wake")]
        public JoinDataComplete Wake = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 14,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input call the home shot in the system",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GoHome")]
        public JoinDataComplete GoHome = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 15,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulse input call the home shot in the system",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetAutoSwitchStatus")]
        public JoinDataComplete GetAutoSwitchStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 16,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets current status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetRecordStatus")]
        public JoinDataComplete GetRecordStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 17,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets current status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetIsoRecordStatus")]
        public JoinDataComplete GetIsoRecordStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 18,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets current status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetStreamStatus")]
        public JoinDataComplete GetStreamStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 19,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets current status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetOutputStatus")]
        public JoinDataComplete GetOutputStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 20,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets current status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetCurrentLayout")]
        public JoinDataComplete GetCurrentLayout = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 21,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets current status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetLayouts")]
        public JoinDataComplete GetLayouts = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 22,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets available layouts",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetCameraStatus")]
        public JoinDataComplete GetCameraStatus = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 23,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets current status",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("GetCameras")]
        public JoinDataComplete GetCameras = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 24,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Gets available cameras",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("RecallCameraPreset")]
        public JoinDataComplete RecallCameraPreset = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 25,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Recalls the preset specified by the CameraPresetToRecall and CameraToRecallPresetOn analog joins.",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CopyFiles")]
        public JoinDataComplete CopyFiles = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 26,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Trigger copying of files.",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("CopyFilesSuccesfulFB")]
        public JoinDataComplete CopyFilesSuccesfulFb = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 27,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Pulses when copy is successful.",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Digital
            });

        [JoinName("DeleteFiles")]
        public JoinDataComplete DeleteFiles = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 27,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Set High to have files deleted after copy files operation",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Digital
            });

		[JoinName("GetRoomConfigs")]
		public JoinDataComplete GetRoomConfigs = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 28,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Query for available room configs",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

        #endregion


		#region Analog

		[JoinName("ChangeLayout")]
        public JoinDataComplete ChangeLayout = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 1,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Specifies the desired layout and provides feedback for current layout. Valid values 1-26 correspond to A-Z",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("NumberOfLayouts")]
        public JoinDataComplete NumberOfLayouts = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Reports the number of layouts stored on the Automate server",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("ChangeRoomConfig")]
        public JoinDataComplete ChangeRoomConfig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Specifies the desired room config and provides feedback for current config. Valid values 1-99.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("ForceChangeRoomConfig")]
        public JoinDataComplete ForceChangeRoomConfig = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Forces the desired room config. Valid values 1-99.",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });
        
        [JoinName("NumberOfRoomConfigs")]
        public JoinDataComplete NumberOfRoomConfigs = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 5,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Reports the number of configs stored on the Automate server",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });


        [JoinName("ChangeCamera")]
        public JoinDataComplete ChangeCamera = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 6,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Specifies the desired camera and provides feedback for current camera.",
                JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("NumberOfCameras")]
        public JoinDataComplete NumberOfCameras = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 7,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Reports the number of cameras available on the Automate server",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("LiveCameraPreset")]
        public JoinDataComplete LiveCameraPreset = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 8,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Specifies preset for the current camera.",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("CameraPresetToRecall")]
        public JoinDataComplete CameraPresetToRecall = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 9,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Specifies preset to recall on CameraToRecallPresetOn. Pulse RecallCameraPreset join to execute.",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("CameraToRecallPresetOn")]
        public JoinDataComplete CameraToRecallPresetOn = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 10,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Specifies camera to recall preset on specified by CameraPresetToRecall.. Pulse RecallCameraPreset join to execute.",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("StorageSpaceAvailableGB")]
        public JoinDataComplete StorageSpaceAvailableGb = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 11,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Reports the remaining storage space in GB",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

        [JoinName("StorageSpaceTotalGB")]
        public JoinDataComplete StorageSpaceTotalGb = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 12,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Reports the total storage space in GB",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Analog
            });

		[JoinName("ChangeScenario")]
		public JoinDataComplete ChangeScenario = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 13,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Specifies the desired scenario and provides feedback for current scenario.",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Analog
			});

		[JoinName("NumberOfScenarios")]
		public JoinDataComplete NumberOfScenarios = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 14,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Reports the number of scenarios stored on the Automate server",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Analog
			});

		#endregion


		#region Serial


        [JoinName("ErrorMessage")]
        public JoinDataComplete ErrorMessage = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Error message from device",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

        [JoinName("SuccessMessage")]
        public JoinDataComplete SuccessMessage = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 2,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "Success message from device",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("CopyFilesDestination")]
        public JoinDataComplete CopyFilesDestination = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 3,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "The location to copy files to",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

        [JoinName("CopyLogDestination")]
        public JoinDataComplete CopyLogDestination = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 4,
                JoinSpan = 1
            },
            new JoinMetadata
            {
                Description = "The location for the log file for the copy files operation",
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });

		[JoinName("CurrentLayoutName")]
		public JoinDataComplete CurrentLayoutName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 11,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Current layout name.",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("CurrentRoomConfigName")]
		public JoinDataComplete CurrentRoomConfigName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 12,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Current room config name.",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("CurrentScenarioName")]
		public JoinDataComplete CurrentScenarioName = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 13,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Current scenario name.",
				JoinCapabilities = eJoinCapabilities.ToFromSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("CameraNames")]
		public JoinDataComplete CameraNames = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 31,
				JoinSpan = 12
			}, 
			new JoinMetadata
			{
				Description = "Name of each camera 1-12",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});

		[JoinName("LayoutNames")]
		public JoinDataComplete LayoutNames = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 51,
				JoinSpan = 26
			},
			new JoinMetadata
			{
				Description = "Name of each layout [A-Z]",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Serial
			});


        [JoinName("RoomConfigNames")]
        public JoinDataComplete RoomConfigNames = new JoinDataComplete(
            new JoinData
            {
                JoinNumber = 101,
                JoinSpan = 99,
            },
            new JoinMetadata
            {
                Description = "Name of each room config [1-99]",
                JoinCapabilities = eJoinCapabilities.ToSIMPL,
                JoinType = eJoinType.Serial
            });


		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
        public OneBeyondAutomateVxBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(OneBeyondAutomateVxBridgeJoinMap))
		{
		}
	}
}