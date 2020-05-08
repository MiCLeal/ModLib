﻿using ModLib.Debugging;
using ModLib.Definitions;
using ModLib.GUI.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModLib
{
    internal static class SettingsDatabase
    {
        private static List<ModSettingsVM> _modSettingsVMs = null;
        private static Dictionary<string, SettingsBase> AllSettingsDict { get; } = new Dictionary<string, SettingsBase>();

        public static List<SettingsBase> AllSettings => AllSettingsDict.Values.ToList();
        public static int SettingsCount => AllSettingsDict.Values.Count;
        public static List<ModSettingsVM> ModSettingsVMs
        {
            get
            {
                if (_modSettingsVMs == null)
                {
                    BuildModSettingsVMs();
                }
                return _modSettingsVMs;
            }
        }

        /// <summary>
        /// Registers the settings class with the SettingsDatabase for use in the settings menu.
        /// </summary>
        /// <param name="settings">Intance of the settings object to be registered with the SettingsDatabase.</param>
        /// <returns>Returns true if successful. Returns false if the object's ID key is already present in the SettingsDatabase.</returns>
        private static bool RegisterSettings(SettingsBase settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (!AllSettingsDict.ContainsKey(settings.ID))
            {
                AllSettingsDict.Add(settings.ID, settings);
                _modSettingsVMs = null;
                return true;
            }
            else
            {
                //TODO:: When debugging log is finished, show a message saying that the key already exists
                return false;
            }
        }

        /// <summary>
        /// Retrieves the Settings instance from the SettingsDatabase with the given ID.
        /// </summary>
        /// <param name="uniqueID">The ID for the settings instance.</param>
        /// <returns>Returns the settings instance with the given ID. Returns null if nothing can be found.</returns>
        internal static SettingsBase GetSettings(string uniqueID)
        {
            if (AllSettingsDict.ContainsKey(uniqueID))
            {
                return AllSettingsDict[uniqueID];
            }
            else
                return null;
        }

        /// <summary>
        /// Saves the settings instance to file.
        /// </summary>
        /// <param name="settingsInstance">Instance of the settings object to save to file.</param>
        /// <returns>Return true if the settings object was saved successfully. Returns false if it failed to save.</returns>
        internal static bool SaveSettings(SettingsBase settingsInstance)
        {
            if (settingsInstance == null) throw new ArgumentNullException(nameof(settingsInstance));
            return FileDatabase.SaveToFile(settingsInstance.ModuleFolderName, settingsInstance, FileDatabase.Location.Configs);
        }

        /// <summary>
        /// Resets the settings instance to the default values for that instance.
        /// </summary>
        /// <param name="settingsInstance">The instance of the object to be reset</param>
        /// <returns>Returns the instance of the new object with default values.</returns>
        internal static SettingsBase ResetSettingsInstance(SettingsBase settingsInstance)
        {
            if (settingsInstance == null) throw new ArgumentNullException(nameof(settingsInstance));
            string id = settingsInstance.ID;
            SettingsBase newObj = (SettingsBase)Activator.CreateInstance(settingsInstance.GetType());
            newObj.ID = id;
            AllSettingsDict[id] = newObj;
            return newObj;
        }

        internal static bool OverrideSettingsWithID(SettingsBase settings, string ID)
        {
            if (AllSettingsDict.ContainsKey(ID))
            {
                AllSettingsDict[ID] = settings;
                return true;
            }
            return false;
        }

        internal static void LoadAllSettings()
        {
            List<Type> types = new List<Type>();

            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                var list = (from t in assem.GetTypes()
                            where t != typeof(SettingsBase) && t.IsSubclassOf(typeof(SettingsBase)) && !t.IsAbstract && !t.IsInterface
                            select t).ToList();

                if (list.Any())
                    types.AddRange(list);
            }

            if (types.Any())
            {
                foreach (var t in types)
                {
                    LoadSettingsFromType(t);
                }
            }
        }

        internal static void LoadSettingsFromType(Type t)
        {
            SettingsBase defaultSB = (SettingsBase)Activator.CreateInstance(t);
            SettingsBase sb = FileDatabase.Get<SettingsBase>(defaultSB.ID);
            if (sb == null)
            {
                string path = Path.Combine(FileDatabase.GetPathForModule(defaultSB.ModuleFolderName, FileDatabase.Location.Configs), FileDatabase.GetFileNameFor(defaultSB));
                if (File.Exists(path))
                {
                    FileDatabase.LoadFromFile(path);
                    sb = FileDatabase.Get<SettingsBase>(defaultSB.ID);
                }
                if (sb == null)
                    sb = defaultSB;
            }
            RegisterSettings(sb);
        }

        internal static void BuildModSettingsVMs()
        {
            try
            {
                _modSettingsVMs = new List<ModSettingsVM>();
                foreach (var settings in AllSettings)
                {
                    ModSettingsVM msvm = new ModSettingsVM(settings);
                    _modSettingsVMs.Add(msvm);
                }
                _modSettingsVMs.Sort((x, y) => y.ModName.CompareTo(x.ModName));
            }
            catch (Exception ex)
            {
                ModDebug.ShowError("An error occurred while creating the ViewModels for all mod settings", "Error Occurred", ex);
            }
        }
    }
}
