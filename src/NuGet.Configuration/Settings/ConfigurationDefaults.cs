﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Configuration;

namespace NuGet.Configuration
{
    public class ConfigurationDefaults
    {
        private ISettings _settingsManager = NullSettings.Instance;
        private const string ConfigurationDefaultsFile = "NuGetDefaults.config";
        private static readonly ConfigurationDefaults _instance = InitializeInstance();

        private bool _defaultPackageSourceInitialized;
        private List<PackageSource> _defaultPackageSources;
        private string _defaultPushSource;

        private static ConfigurationDefaults InitializeInstance()
        {
#if !DNXCORE50
            var baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NuGet");
#else
            var baseDirectory = Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "NuGet");
#endif
            return new ConfigurationDefaults(baseDirectory, ConfigurationDefaultsFile);
        }

        // TODO: Make this internal again
        /// <summary>
        /// An internal constructor MAINLY INTENDED FOR TESTING THE CLASS. But, the product code is only expected to use the static Instance property
        /// Only catches FileNotFoundException. Will throw all exceptions including other IOExceptions and XmlExceptions for invalid xml and so on
        /// </summary>
        /// <param name="directory">The directory that has the NuGetDefaults.Config</param>
        /// <param name="configFile">Name of the NuGetDefaults.Config</param>
        public ConfigurationDefaults(string directory, string configFile)
        {
            try
            {
                if (System.IO.File.Exists(Path.Combine(directory, configFile)))
                {
                    _settingsManager = new Settings(directory, configFile);
                }
            }
            catch (FileNotFoundException)
            {
            }

            // Intentionally, we don't catch all IOExceptions, XmlException or other file related exceptions like UnAuthorizedAccessException
            // This way, administrator will become aware of the failures when the ConfigurationDefaults file is not valid or permissions are not set properly
        }

        public static ConfigurationDefaults Instance
        {
            get
            {
                return _instance;
            }
        }

        public IEnumerable<PackageSource> DefaultPackageSources
        {
            get
            {
                if (_defaultPackageSources == null)
                {
                    _defaultPackageSources = new List<PackageSource>();
                    IList<SettingValue> disabledPackageSources = _settingsManager.GetSettingValues("disabledPackageSources");
                    IList<SettingValue> packageSources = _settingsManager.GetSettingValues("packageSources");

                    foreach (var settingValue in packageSources)
                    {
                        // In a SettingValue representing a package source, the Key represents the name of the package source and the Value its source
                        _defaultPackageSources.Add(new PackageSource(settingValue.Value,
                            settingValue.Key,
                            isEnabled: !disabledPackageSources.Any<SettingValue>(p => p.Key.Equals(settingValue.Key, StringComparison.CurrentCultureIgnoreCase)),
                            isOfficial: true));
                    }
                }
                return _defaultPackageSources;
            }
        }

        public string DefaultPushSource
        {
            get
            {
                if (_defaultPushSource == null && !_defaultPackageSourceInitialized)
                {
                    _defaultPackageSourceInitialized = true;
                    _defaultPushSource = _settingsManager.GetValue("config", "DefaultPushSource");
                }
                return _defaultPushSource;
            }
        }

        public string DefaultPackageRestoreConsent
        {
            get
            {
                return _settingsManager.GetValue("packageRestore", "enabled");
            }
        }
    }
}
