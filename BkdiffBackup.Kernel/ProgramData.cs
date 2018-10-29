using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BkdiffBackup {
    /// <summary>
    /// read and write config from/to program data directory
    /// </summary>
    public static class ProgramData {

        /// <summary>
        /// currently loaded config
        /// </summary>
        public static Configuration CurrentConfiguration {
            get;
            private set;
        }

        static ProgramData() {
            if(File.Exists(FullconfigFilePath)) {
                ReloadConfiguration();
            } else {
                CurrentConfiguration = new Configuration();
                CurrentConfiguration.Directories = new Configuration.BkupDir[] {
                    new Configuration.BkupDir() {
                        DirectoryToBackup = "C:\\Users",
                        MirrorLocation = "Specify-mirror-here"
                    }
                };
                SaveConfiguration();
            }
        }


        public static string GetProgramDataDir() {
            string dir = System.Environment.GetEnvironmentVariable("ProgramData");
            string Sub = "BkdiffBkup";
            string r = Path.Combine(dir, Sub);
            if(!System.IO.Directory.Exists(r)) {
                System.IO.Directory.CreateDirectory(r);
            }
            return r;
        }

        /// <summary>
        /// file name of configuration file
        /// </summary>
        public const string ConfigFileName = "config.json";

        /// <summary>
        /// path to config file name.
        /// </summary>
        public static string FullconfigFilePath {
            get {
                return Path.Combine(GetProgramDataDir(), ConfigFileName);
            }
        }

        static string GetOldConfigFilename() {
            string b = Path.GetFileNameWithoutExtension(ConfigFileName);
            string e = Path.GetExtension(ConfigFileName);
            string d = GetProgramDataDir();

            for(int i = 0; i < 1000; i++) {
                string g = Path.Combine(b, b + ".old" + i + e);
                if (!File.Exists(g))
                    return g;
            }

            return Path.Combine(b, b + ".old1001" + e);
        }


        /// <summary>
        /// Re-loads <see cref="CurrentConfiguration"/> from disk
        /// </summary>
        static public void ReloadConfiguration() {
            string p = FullconfigFilePath;
            string s = File.ReadAllText(p);
            var config = Configuration.Deserialize(s);
            CurrentConfiguration = config;
        }


        /// <summary>
        /// save to text file 
        /// </summary>
        static public void SaveConfiguration() {
            string p = FullconfigFilePath;

            if(File.Exists(p)) {
                File.Copy(p, GetOldConfigFilename());
            }

            string s = CurrentConfiguration.Serialize();
            File.WriteAllText(p, s);
        }


    }
}
