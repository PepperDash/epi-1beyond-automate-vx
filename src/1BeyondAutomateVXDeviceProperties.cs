using System;
using System.Collections.Generic;
using OneBeyondAutomateVxEpi.ApiObjects;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace OneBeyondAutomateVxEpi
{
    public partial class OneBeyondAutomateVx
    {

        #region Available Storage
		//event EventHandler StorageSpaceAvailableChanged;

		//private List<Drive> _drives = new List<Drive>();

		//public List<Drive> Drives
		//{
		//    get
		//    {
		//        return _drives;
		//    }
		//    private set
		//    {
		//        _drives = value;

		//        OnDrivesChanged();
		//    }
		//}

		//private void OnDrivesChanged()
		//{
		//    var handler = StorageSpaceAvailableChanged;

		//    if (handler != null)
		//    {
		//        handler(this, new EventArgs());
		//    }
		//}

        #endregion


        #region Recording Space
		//event EventHandler RecordingSpaceAvailableChanged;

		//private RecordingSpace _recordingSpace = new RecordingSpace("0", "0");

		//public RecordingSpace RecordingSpace
		//{
		//    get 
		//    { 
		//        return _recordingSpace;
		//    }
		//    private set 
		//    {
		//        _recordingSpace = value;
		//        OnRecordingSpaceChanged();
		//    }
		//}

		//private void OnRecordingSpaceChanged()
		//{
		//    var handler = RecordingSpaceAvailableChanged;

		//    if (handler != null)
		//    {
		//        handler(this, new EventArgs());
		//    }
		//}

        #endregion


        

        

        event EventHandler FileCopySuccessful;


        private void OnFileCopySuccessful()
        {
            var handler = FileCopySuccessful;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

    }
}