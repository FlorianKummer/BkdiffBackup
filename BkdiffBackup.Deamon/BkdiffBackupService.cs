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
            try {
                Logmsg("loading configuration...");
                ProgramData.ReloadConfiguration();
                Logmsg("success.");
                foreach (var c in ProgramData.CurrentConfiguration.Directories) {
                    Logmsg(string.Format("backup job '{0}' -> '{1}' ...", c.DirectoryToBackup, c.MirrorLocation));
                }

            } catch(Exception e) {
                Logmsg("SERIOUS EXCEPTION - LOADING CONFIGURATION: " + e.GetType().Name + ": '" + e.Message + "' Stacktrace: " + e.StackTrace);
                Logmsg("TERMINATING SERVICE.");
                base.Stop();
            }

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
                Sleeptime = Math.Max(Sleeptime, 1000);
                Logmsg("Going to sleep for " + (Sleeptime / 1000) + " seconds...");

                Thread.Sleep(Sleeptime);
                Logmsg("wakeup");

                //bool anyFailed
                foreach (var c in ProgramData.CurrentConfiguration.Directories) {
                    /*if(IsDriveReady(c)) {

                    } else {

                    }*/
                    try {
                        Logmsg(string.Format("Running backup '{0}' -> '{1}' ...", c.DirectoryToBackup, c.MirrorLocation));
                        var s = Kernel.RunFromConfig(c);
                        Logmsg("success. statistics: " + s.ToString());
                    } catch (Exception e) {
                        Logmsg("SERIOUS EXCEPTION - BACKUP INCOMPLETE: " + e.GetType().Name + ": '" + e.Message + "' Stacktrace: " + e.StackTrace);
                    }
                }
            }
        }
    }
}
