using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using PepperDash.Essentials.Core;

namespace PDT.OneBeyondAutomateVx.EPI
{
    public partial class OneBeyondAutomateVX
    {
        // Constructors, methods and some fields located in ******************************
        // separate file: 1BeyondAutomateVXDevice.cs        ******************************


        private bool _autoSwitchIsOn;

        private bool AutoSwitchIsOn
        {
            get { return _autoSwitchIsOn; }
            set
            {
                if (value != _autoSwitchIsOn)
                {
                    value = _autoSwitchIsOn;
                    AutoSwitchIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback AutoSwitchIsOnFB;


        private bool _recordIsOn;

        private bool RecordIsOn
        {
            get { return _recordIsOn; }
            set
            {
                if (value != _recordIsOn)
                {
                    value = _recordIsOn;
                    RecordIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback RecordIsOnFB;


        private bool _isoRecordIsOn;

        private bool IsoRecordIsOn
        {
            get { return _isoRecordIsOn; }
            set
            {
                if (value != _isoRecordIsOn)
                {
                    value = _isoRecordIsOn;
                    IsoRecordIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback IsoRecordIsOnFB;


        private bool _streamIsOn;

        private bool StreamIsOn
        {
            get { return _streamIsOn; }
            set
            {
                if (value != _streamIsOn)
                {
                    value = _streamIsOn;
                    StreamIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback StreamIsOnFB;


        private bool _outputIsOn;

        private bool OutputIsOn
        {
            get { return _outputIsOn; }
            set
            {
                if (value != _outputIsOn)
                {
                    value = _outputIsOn;
                    OutputIsOnFB.FireUpdate();
                }
            }
        }

        public BoolFeedback OutputIsOnFB;

        event EventHandler LayoutsChanged;

        private List<Layout> _layouts;

        public List<Layout> Layouts
        {
            get
            {
                return _layouts;
            }
            set
            {
                _layouts = value;

                OnLayoutsChanged();
            }
        }

        private void OnLayoutsChanged()
        {
            var handler = LayoutsChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        event EventHandler LayoutChanged;

        private Layout _layout;

        public Layout Layout
        {
            get
            {
                return _layout;
            }
            set
            {
                _layout = value;

                OnLayoutChanged();
            }
        }

        private void OnLayoutChanged()
        {
            var handler = LayoutChanged;

            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }
    }
}