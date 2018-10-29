using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BkdiffBackup {
    public class BackupException : IOException {
        public BackupException(string Message, FileSystemInfo fi) :
            base(Message + " on file '" + fi.FullName + "'") {

        }
    }
}
