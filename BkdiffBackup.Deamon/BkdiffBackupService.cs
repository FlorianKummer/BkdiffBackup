using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BkdiffBackup {
    public partial class BkdiffBackupService : ServiceBase {
        public BkdiffBackupService() {
            InitializeComponent();
        }

        protected override void OnStart(string[] args) {

        }

        protected override void OnStop() {
        }
    }
}
