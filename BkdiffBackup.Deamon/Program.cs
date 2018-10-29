using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BkdiffBackup {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// - Installation of the service:
        ///   sc.exe create BkdiffBackup binpath=c:\tmp\BkdiffBackup\BkdiffBackup.Deamon.exe
        /// - Uninstall:
        ///   sc.exe delete BkdiffBackup
        /// </summary>
        static void Main() {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new BkdiffBackupService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
