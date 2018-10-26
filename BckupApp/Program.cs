using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BckupApp {
    static class Program {



        static void Main(string[] args) {
            string SourcePath = @"C:\Users\florian";

            string TargetPath = @"D:\bkup";
            string DateString = DateTime.Now.ToString("yyyy-MMM-dd--HH-mm-ss");

            string MirrorPath = Path.Combine(TargetPath, "mirror");
            string BkDiffPath = Path.Combine(TargetPath, "bkdiff-" + DateString);

            if (!Directory.Exists(MirrorPath)) {
                Directory.CreateDirectory(MirrorPath);
            }
            if (!Directory.Exists(BkDiffPath)) {
                Directory.CreateDirectory(BkDiffPath);
            }


            BckupKernel.Kernel.Execute(SourcePath, MirrorPath, BkDiffPath);


        }
    }
}
