using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BkdiffBackup {
    static class Program {



        static void Main(string[] args) {

            /*
            string fix = @"\\?\";

            string src = @"I:\misc\fdy-publication-data\Thesis\2017\Lindig\root3\src\public\L4-application\CutCellQuadrature\bin\Release\Comapare\lindigsinustestcase18avol_EqPol_tiny_0th_1th_2th_comp_NEW\lindigsinustestcase18avol_Square_Tiny_EquivalentPolynomials_0th.txt";
            string dst = @"\\?\UNC\terminal03\BackupTemp\misc\fdy-publication-data\Thesis\2017\Lindig\root3\src\public\L4-application\CutCellQuadrature\bin\Release\Comapare\lindigsinustestcase18avol_EqPol_tiny_0th_1th_2th_comp_NEW\lindigsinustestcase18avol_Square_Tiny_EquivalentPolynomials_0th.txt";


            string oda = @"a:\dsjflsfjldsjaljflsfjslfjldsjfsjkflsfjsalfjlöjkjl\hfkjdhfkahfkjdsafkjhvhfuggvztzsgcghysvcsv\fchrcerbjsd\ahgxvhkbcas\xcatevdhssyvbcgh\cheatgvhdvm\terminal03\BackupTemp\misc\fdy-publication-data\Thesis\2017\Lindig\root3\src\public\L4-application\CutCellQuadrature\bin\Release\Comapare\lindigsinustestcase18avol_EqPol_tiny_0th_1th_2th_comp_NEW\lindigsinustestcase18avol_Square_Tiny_EquivalentPolynomials_0th.txt";

            string a = Path.GetDirectoryName(oda);
            string b = Path.GetFileName(oda);


            Console.WriteLine("src exists: " + File.Exists(src));

            Console.WriteLine("dst dir: " + Directory.Exists(@"\\terminal03\BackupTemp"));
            Console.WriteLine("dst dir: " + Directory.Exists(@"\\?\UNC\terminal03\BackupTemp"));
            


            File.Copy(fix + src, dst, true);
            //*/





            if (ProgramData.ConfigfileExists) {
                ProgramData.ReloadConfiguration();

                foreach (var c in ProgramData.CurrentConfiguration.Directories) {
#if !DEBUG
                    try {
#endif
                        Console.WriteLine(string.Format("Running backup {0} -> {1} ...", c.DirectoryToBackup, c.MirrorLocation));
                        var stat = Kernel.RunFromConfig(c);
                        Console.WriteLine("done; statistics: " + stat.ToString());
#if !DEBUG
                    } catch (Exception e) {
                        Console.Error.WriteLine("SERIOUS EXCEPTION - BACKUP INCOMPLETE: " + e.GetType().Name + ": " + e.Message);

                    }
#endif
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
