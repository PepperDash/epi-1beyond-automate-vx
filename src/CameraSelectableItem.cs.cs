using System;
using PepperDash.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;

namespace OneBeyondAutomateVxEpi
    {
    public class CameraSelectableItem : ISelectableItem, IKeyName, IKeyed
        {
        private readonly OneBeyondAutomateVx _parent;
        public ushort Key { get; }
        string IKeyed.Key => Key.ToString();
        public string Name { get; }

        private bool _isSelected;
        public bool IsSelected
            {
            get => _isSelected;
            set
                {
                if (_isSelected == value) return;
                _isSelected = value;
                ItemUpdated?.Invoke(this, EventArgs.Empty);
                }
            }

        public event EventHandler ItemUpdated;

        public CameraSelectableItem(OneBeyondAutomateVx parent, ushort key, string name)
            {
            _parent = parent;
            Key = key;
            Name = name;
            }

        public void Select()
            {
            _parent.SetCamera(Key);
            }
        }
    }
