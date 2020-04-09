﻿using ModLib.Attributes;
using System.Xml.Serialization;

namespace ModLib
{
    public class Settings : SettingsBase
    {
        public override string ModName => "ModLib";
        public override string ModuleFolderName => ModLibSubModule.ModuleFolderName;
        private const string instanceID = "ModLibSettings";
        private static Settings _instance = null;
        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FileDatabase.Get<ModLib.Settings>(instanceID);
                    if (_instance == null)
                    {
                        _instance = new Settings();
                        FileDatabase.SaveToFile(ModLibSubModule.ModuleFolderName, _instance);
                    }
                }
                return _instance;
            }
        }

        [XmlElement]
        public override string ID { get; set; } = instanceID;
        [XmlElement]
        [SettingProperty("Enable Crash Error Reporting", "When enabled, shows a message box showing the cause of a crash.")]
        [SettingPropertyGroup("Debugging")]
        public bool DebugMode { get; set; } = true;

    }
}
