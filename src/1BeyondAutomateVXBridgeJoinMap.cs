using PepperDash.Essentials.Core;

namespace PDT.OneBeyondAutomateVx.EPI
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.  Reference Essentials JoinMaps, if one exists for the device plugin being developed
	/// </remarks>
	/// <see cref="PepperDash.Essentials.Core.Bridges"/>
	/// <example>
	/// "EssentialsPluginBridgeJoinMapTemplate" renamed to "SamsungMdcBridgeJoinMap"
	/// </example>
	public class EssentialsPluginTemplateBridgeJoinMap : JoinMapBaseAdvanced
	{
		#region Digital

		// TODO [ ] Add digital joins below plugin being developed

		[JoinName("AuthenticatedFB")]
        public JoinDataComplete AuthenticatedFB = new JoinDataComplete(
			new JoinData
			{
				JoinNumber = 1,
				JoinSpan = 1
			},
			new JoinMetadata
			{
				Description = "Successfully authenticated to Automate VX server",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
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
                JoinNumber =42,
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


		#endregion


		#region Analog

		// TODO [ ] Add analog joins below plugin being developed

        //[JoinName("Status")]
        //public JoinDataComplete Status = new JoinDataComplete(
        //    new JoinData
        //    {
        //        JoinNumber = 1,
        //        JoinSpan = 1
        //    },
        //    new JoinMetadata
        //    {
        //        Description = "Socket Status",
        //        JoinCapabilities = eJoinCapabilities.ToSIMPL,
        //        JoinType = eJoinType.Analog
        //    });

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
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
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
                JoinCapabilities = eJoinCapabilities.FromSIMPL,
                JoinType = eJoinType.Serial
            });
		#endregion

		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
        public EssentialsPluginTemplateBridgeJoinMap(uint joinStart)
            : base(joinStart, typeof(EssentialsPluginTemplateBridgeJoinMap))
		{
		}
	}
}