using OneBeyondAutomateVxEpi.ApiObjects;
using PepperDash.Core;
using PepperDash.Essentials.Core.DeviceTypeInterfaces;
using System;
using System.Collections.Generic;

namespace OneBeyondAutomateVxEpi
    {
    public class ScenariosSelectableItems : ISelectableItems<string>, IKeyName
    {
        //private Dictionary<string, ISelectableItem> _items = new Dictionary<string, ISelectableItem>();

        public event EventHandler ItemsUpdated;
        public event EventHandler CurrentItemChanged;

        private Dictionary<string, ISelectableItem> _items = new Dictionary<string, ISelectableItem>();

        public Dictionary<string, ISelectableItem> Items {
            get { return _items; }
            set
            {
                _items = value;
                ItemsUpdated?.Invoke(this, EventArgs.Empty);
            }
        }
        public string CurrentItem { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }

        public ScenariosSelectableItems(string key, string name, Dictionary<string, ISelectableItem> items)
        {
            Key = key;
            Name = name;
            Items = items;
        }

    }

        public class ScenariosSelectableItem : ISelectableItem
            {
            public string Key { get; set; }
            public string Name { get; set; }
            private OneBeyondAutomateVx _parent;

            public event EventHandler ItemUpdated;

            public int Id { get; set; }
            public bool IsSelected { get; set; }

            public ScenariosSelectableItem(string key, string name, int id, OneBeyondAutomateVx parent)
                {
                Key = key;
                Name = name;
                Id = id;
                _parent = parent;
                }

            public void Select()
                {
                _parent.SetScenario((uint)Id);
                }

            public void UpdateSelectedFromFeedback(int selectedId)
                {
                IsSelected = Id == selectedId;
                ItemUpdated?.Invoke(this, EventArgs.Empty);
                }

            public override string ToString()
                {
                return $"{Name} (ID: {Id})";
                }
            }
        }
    