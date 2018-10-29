using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BkdiffBackup {
    static class Program {



        static void Main(string[] args) {

            if (ProgramData.ConfigfileExists) {
                ProgramData.ReloadConfiguration();

                foreach (var c in ProgramData.CurrentConfiguration.Directories) {
                    try {
                        Console.WriteLine(string.Format("Running backup {0} -> {1} ...", c.DirectoryToBackup, c.MirrorLocation));
                        Kernel.RunFromConfig(c);
                        Console.WriteLine("done.");
                    } catch (Exception e) {
                        Console.Error.WriteLine("SERIOUS EXCEPTION - BACKUP INCOMPLETE: " + e.GetType().Name + ": " + e.Message);

                    }
                }

            } else {
                ProgramData.CurrentConfiguration = new Configuration();
                ProgramData.CurrentConfiguration.Directories = new Configuration.BkupDir[] {
                    new Configuration.BkupDir() {
                        DirectoryToBackup = "C:\\Users",
                        MirrorLocation = "Specify-mirror-here"
                    }
                };
                ProgramData.SaveConfiguration();

                Console.WriteLine("Created dummy configuration file '{0}'.", ProgramData.FullconfigFilePath);
                Console.WriteLine("Enter valid configuration and run again.");
            }
            



        }
    }
}
