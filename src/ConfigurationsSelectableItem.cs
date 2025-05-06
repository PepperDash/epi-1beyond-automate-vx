using OneBeyondAutomateVxEpi.ApiObjects;
using PepperDash.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using System;
using System.Collections.Generic;

namespace OneBeyondAutomateVxEpi
    {
    public class ConfigurationsSelectableItems : ISelectableItems<string>, IKeyName
        {
        private Dictionary<string, ISelectableItem> _items = new Dictionary<string, ISelectableItem>();

        public event EventHandler ItemsUpdated;
        public event EventHandler CurrentItemChanged;

        public Dictionary<string, ISelectableItem> Items { get; set; }
        public string CurrentItem { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }

        public ConfigurationsSelectableItems(string key, string name, Dictionary<string, ISelectableItem> items)
            {
            Key = key;
            Name = name;
            Items = items;
            }

        public class ConfigurationsSelectableItem : ISelectableItem
            {
            public string Key { get; set; }
            public string Name { get; set; }
            private OneBeyondAutomateVx _parent;

            public event EventHandler ItemUpdated;

            public int Id { get; set; }
            public bool IsSelected { get; set; }

            public ConfigurationsSelectableItem(string key, string name, int id, OneBeyondAutomateVx parent)
                {
                Key = key;
                Name = name;
                Id = id;
                _parent = parent;
                }

            public void Select()
                {
                // Logic for selecting the configuration
                _parent.CurrentRoomConfig = new NameWithIdInt { Id = Id, Name = Name };
                IsSelected = true;
                }

            public override string ToString()
                {
                return $"{Name} (ID: {Id})";
                }
            }
        }
    }