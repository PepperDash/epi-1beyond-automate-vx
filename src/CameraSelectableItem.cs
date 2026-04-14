using System;
using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;

namespace OneBeyondAutomateVxEpi
    {
    public class CameraSelectableItems : ISelectableItems<string>, IKeyName
        {
        private Dictionary<string, ISelectableItem> _items = new Dictionary<string, ISelectableItem>();
        public Dictionary<string, ISelectableItem> Items
            {
            get => _items;
            set
                {
                _items = value;
                ItemsUpdated?.Invoke(this, EventArgs.Empty);
                }
            }

        private string _currentItem;
        public string CurrentItem
            {
            get => _currentItem;
            set
                {
                _currentItem = value;
                CurrentItemChanged?.Invoke(this, EventArgs.Empty);
                }
            }

        public string Name { get; private set; }

        public string Key { get; private set; }

        public event EventHandler ItemsUpdated;
        public event EventHandler CurrentItemChanged;

        public CameraSelectableItems(string key, string name, Dictionary<string, ISelectableItem> items)
            {
            Key = key;
            Name = name;
            Items = items;
            }

        public class CameraSelectableItem : ISelectableItem
            {
            public string Key { get; private set; }
            public string Name { get; private set; }
            private OneBeyondAutomateVx _parent;
            private bool _isSelected;

            public int Id { get; set; }
            public bool IsSelected
                {
                get { return _isSelected; }
                set
                    {
                    if (_isSelected == value) return;
                    _isSelected = value;
                    ItemUpdated?.Invoke(this, EventArgs.Empty);
                    }
                }

            public event EventHandler ItemUpdated;

            public CameraSelectableItem(string key, string name, int id, OneBeyondAutomateVx parent)
                {
                if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
                if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
                if (parent == null) throw new ArgumentNullException(nameof(parent));
                if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id), "ID must be greater than zero.");

                Key = key;
                Name = name;
                Id = id;
                _parent = parent;
                }

            public void Select()
                {
                _parent.SetCamera((ushort)Id);
                }

            // Override ToString() for better logging
            public override string ToString()
                {
                return Name;
                }
            }
        }
    }