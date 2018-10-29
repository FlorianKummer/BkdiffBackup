using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BkdiffBackup {
    public partial class BkdiffBackupService : ServiceBase {
        public BkdiffBackupService() {
            InitializeComponent();
        }

        TextWriter log;

        void Logmsg(string s) {
            log.Write(DateTime.Now);
            log.Write(": ");
            log.Write(s);
            log.WriteLine();
            log.Flush();
        }

        Thread bkground;

        protected override void OnStart(string[] args) {
            string DeamonFile = Path.Combine(ProgramData.GetProgramDataDir(), "deamon-log.txt");
            log = new StreamWriter(DeamonFile, true);
            Logmsg("BkdiffBackup deamon started...");

            bkground = new Thread(InfinityLoop);
            bkground.Start();
        }

        protected override void OnStop() {
            if(log != null) {
                Logmsg("BkdiffBackup deamon stopped");
                log.Flush();
                log.Close();
            }
        }
        
        void InfinityLoop() {

            while (true) {
                DateTime next = ProgramData.CurrentConfiguration.GetNextOccurence();
                Logmsg("Next backup scheduled for: " + next);

                int Sleeptime = (int)Math.Round((next - DateTime.Now).TotalMilliseconds);
                Sleeptime = Math.Min(Sleeptime, 1000);
                Logmsg("Going to sleep for " + (Sleeptime / 1000) + " seconds...");

                Thread.Sleep(Sleeptime);
                Logmsg("wakeup");

                foreach (var c in ProgramData.CurrentConfiguration.Directories) {
                    try {
                        Logmsg(string.Format("Running backup {0} -> {1} ...", c.DirectoryToBackup, c.MirrorLocation));
                        Kernel.RunFromConfig(c);
                        Logmsg("done.");
                    } catch (Exception e) {
                        Logmsg("SERIOUS EXCEPTION - BACKUP INCOMPLETE: " + e.GetType().Name + ": " + e.Message);

                    }
                }
            }
        }
    }
}
