using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace BkdiffBackup {

    public  class Stats {
        public long CopiedBytes = 0;

        public int CopiedFiles = 0;

        public int CopiedDirectories = 0;

        public int Errors = 0;

        public TimeSpan Duration;

        public override string ToString() {
            return String.Format(
                "Duration {6}: {9} error{10}, copied {0} file{1}, checked {2} director{3}, copied {4} MB{5}; {7:N1} MB/sec, {8} files/sec",
                CopiedFiles, CopiedFiles == 1 ? "" : "s",
                CopiedDirectories, CopiedDirectories == 1 ? "y" : "ies",
                CopiedBytes/(1024.0*1024.0), "", //CopiedBytes == 1 ? "" : "s",
                Duration,
                ((double)CopiedBytes) / (1024.0*1024.0 * Duration.TotalSeconds),
                ((double)CopiedFiles) / (Duration.TotalSeconds),
                Errors, Errors == 1 ? "" : "s"
                );
        }
    }

    static public class Kernel {

        public static void RunNow(
            string SourcePath,
            string MirrorPath,
            string BackDiffPath) {
            if (!Directory.Exists(SourcePath))
                throw new ArgumentException();
            if (!Directory.Exists(MirrorPath))
                throw new ArgumentException();
            if (!Directory.Exists(BackDiffPath))
                throw new ArgumentException();

            SyncDirsRecursive(SourcePath, MirrorPath, BackDiffPath, new string[0], false, true);
        }


        static public Stats RunFromConfig(Configuration.BkupDir c) {
            DateTime start = DateTime.Now;
            ResetStat();

            string SourcePath = c.DirectoryToBackup;
            if (!Directory.Exists(SourcePath))
                throw new ArgumentException();


            string MirrorPath = c.MirrorLocation;
            
            if (!Directory.Exists(MirrorPath)) {
                Directory.CreateDirectory(MirrorPath);
            }

            string BkDiffPath = MirrorPath;
            if (BkDiffPath.EndsWith("/") || BkDiffPath.EndsWith("\\"))
                BkDiffPath = Path.GetDirectoryName(BkDiffPath);

            string DateString = DateTime.Now.ToString("yyyy-MMM-dd--HH-mm-ss");
            string LogBaseName = BkDiffPath;
            BkDiffPath = BkDiffPath + "-bkdiff-" + DateString;

            string __InfoLogPath = (LogBaseName + "-" + DateString + "-Info.txt");
            string __ErrLogPath = (LogBaseName + "-" + DateString + "-Err.txt");
            
            if (Directory.Exists(BkDiffPath)) {
                throw new IOException("bkdiff dir already exists: '" + BkDiffPath + "'");
            } else {
                Directory.CreateDirectory(BkDiffPath);
            }

            //using (TextWriter log = new StreamWriter(InfoLogPath), errLog = new StreamWriter(ErrLogPath)) {
            ErrLogPath = __ErrLogPath;
            InfoLogPath = __InfoLogPath;
            ResetWriter(ref InfoLogStream, InfoLogPath);
            ResetWriter(ref ErrLogStream, ErrLogPath);

            try {
                SyncDirsRecursive(SourcePath, MirrorPath, BkDiffPath, new string[0], c.LogFileList, c.CopyAccessControlLists);
                _stats.Duration = DateTime.Now - start;
                Logmsg(_stats.ToString());
            } finally {
                var el = ErrLogStream;
                ErrLogStream = null;
                ErrLogPath = null;

                var ll = InfoLogStream;
                InfoLogStream = null;
                InfoLogPath = null;

                if(el != null) {
                    el.Flush();
                    el.Close();
                    el.Dispose();
                }

                if(ll != null) {
                    ll.Flush();
                    ll.Close();
                    ll.Dispose();
                }
            }

            return _stats;
        }


        static Stats _stats = new Stats();

        static void ResetStat() {
            _stats = new Stats();
        }

        /// <summary>
        /// Error-Log
        /// </summary>
        static StreamWriter ErrLogStream;

        /// <summary>
        /// File path of <see cref="ErrLogStream"/>
        /// </summary>
        static string ErrLogPath;

        /// <summary>
        /// Standard-Log
        /// </summary>
        static StreamWriter InfoLogStream;

        /// <summary>
        /// File path of <see cref="InfoLogStream"/>
        /// </summary>
        static string InfoLogPath;


        /// <summary>
        /// Tries to (re-) open a text writer, with some exception handling
        /// </summary>
        static void ResetWriter(ref StreamWriter s, string FilePath) {
            if(s != null) {
                try {
                    s.Close();
                } catch (Exception) { }
                try {
                    s.Flush();
                } catch (Exception) { }
                try {
                    s.Dispose();
                } catch (Exception) { }

                s = null;
            }

            Exception ee = null;
            for(int iRetry = 0; iRetry < 2; iRetry++) {
                try {
                    s = new StreamWriter(FilePath, true);
                    return;
                } catch(Exception e) {
                    ee = e;
                    Thread.Sleep(60*1000);
                }
            }

            throw ee;
        }


        public static void Error(string s) {
            try {
                ErrLogStream.WriteLine(s);
                ErrLogStream.Flush();
            } catch (Exception) {
                ResetWriter(ref ErrLogStream, ErrLogPath);
                ErrLogStream.WriteLine("stream recovered...");
                ErrLogStream.WriteLine(s);
                ErrLogStream.Flush();
            }
            
        }

        public static void Error(string s, params object[] os) {
            Error(string.Format(s, os));
        }


        public static void Logmsg(string s) {
            try {
                InfoLogStream.WriteLine(s);
                InfoLogStream.Flush();
            } catch(Exception e) {
                // retry
                ResetWriter(ref InfoLogStream, InfoLogPath);
                InfoLogStream.WriteLine("stream recovered...");
                InfoLogStream.WriteLine(s);
                InfoLogStream.Flush();
            }
            
        }

        public static void Logmsg(string s, params object[] os) {
            Logmsg(string.Format(s, os));
        }



        static string[] Cat1(string[] a, string b) {
            string[] R = new string[a.Length + 1];
            Array.Copy(a, R, a.Length);
            R[a.Length] = b;
            return R;
        }
        
        static bool SyncDirsRecursive(string SourceDir, string MirrorDir, string BackDiffDir, string[] RelPath, bool LogFileList, bool CopyAccessControlLists) {
            if (!Directory.Exists(SourceDir)) {
                if (RelPath.Length == 0)
                    throw new ArgumentException("source directory does not exist");
                else 
                    return false;
            }
            _stats.CopiedDirectories++;

            Filter filter = new Filter(SourceDir);
           
            if (!Directory.Exists(MirrorDir)) {
                // +++++++++++++++++++++++
                // create mirror directory
                // +++++++++++++++++++++++

                try {
                    Directory.CreateDirectory(MirrorDir);
                    DirectorySecurity sec = Directory.GetAccessControl(SourceDir);
                    sec.SetAccessRuleProtection(true, true);
                    Directory.SetAccessControl(MirrorDir, sec);

                    if (!Directory.Exists(MirrorDir))
                        throw new BackupException("unable to create directory in mirror", new DirectoryInfo(MirrorDir));
                } catch(Exception e) {
                    Error(e.GetType().Name + ": " + e.Message);
                }
            }

            // sync files
            try {
                SyncFilesInDir(SourceDir, MirrorDir, BackDiffDir, RelPath, filter, LogFileList, CopyAccessControlLists);
            } catch(Exception e) {
                Error(e.GetType().Name + " (exception in recursion): " + e.Message);
            }



            string[] SourceDirs; 
            try { 
                SourceDirs = Directory.GetDirectories(SourceDir); // files in the source directory
            } catch(Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                SourceDirs = new string[0];
            }
                                 
            string[] MirrorDirs;
            try {
                MirrorDirs = Directory.GetDirectories(MirrorDir); // files already present in the mirror directory
            } catch (Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                MirrorDirs = new string[0];
            }

            // filter source dirs
            SourceDirs = SourceDirs.Where(dn => filter.FilterItem(dn) == false).ToArray();
            
            // recursion
            foreach(string s in SourceDirs) {
                string SubDirName = Path.GetFileName(s);

                // old-fashioned search loop -- so I know whats going on:
                int idxFound = -1;
                for (int iTarg = 0; iTarg < MirrorDirs.Length; iTarg++) {
                    if (MirrorDirs[iTarg] == null)
                        continue;

                    if (Path.GetFileName(MirrorDirs[iTarg]) == SubDirName) {
                        idxFound = iTarg;
                        break;
                    }
                }
                if (idxFound >= 0) {
                    MirrorDirs[idxFound] = null;
                }

                bool succ = SyncDirsRecursive(s, Path.Combine(MirrorDir, SubDirName), Path.Combine(BackDiffDir, SubDirName), Cat1( RelPath, SubDirName), LogFileList, CopyAccessControlLists);
                
            }

            // move un-matched directories in mirror to backdiff
            bool BackDiffCreated = Directory.Exists(BackDiffDir);
            foreach (string s in MirrorDirs) {
                if (s == null)
                    continue;

                string SubBackDiffDir = Path.Combine(BackDiffDir, Path.GetFileName(s));

                if (!BackDiffCreated) {
                    EnsureBackDiffDir(BackDiffDir, MirrorDir, RelPath, CopyAccessControlLists);
                    BackDiffCreated = true;
                }

                SaveDirectoryMove(s, SubBackDiffDir, LogFileList, CopyAccessControlLists);
            }

            return true;
        }


        static void SyncFilesInDir(string SourceDir, string MirrorDir, string BkdiffDir, string[] RelPath, Filter filter, bool LogFileList, bool CopyAccessControlLists) {
           
            if (!Directory.Exists(SourceDir))
                throw new ArgumentException("Source directory '{0}' does not exist.", SourceDir);
            if (!Directory.Exists(MirrorDir))
                throw new ArgumentException("Mirror directory '{0}' does not exist.", MirrorDir);

            // ==============
            // get file list
            // ==============
            string[] SourceFiles;
            try {
                SourceFiles = Directory.GetFiles(SourceDir); // files in the source directory
            } catch(Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                SourceFiles = new string[0];
            }


            string[] MirrorFiles;
            try {
                MirrorFiles = Directory.GetFiles(MirrorDir); // files already present in the mirror directory
            } catch (Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                MirrorFiles = new string[0];
            }

            // filter source dirs
            SourceFiles = SourceFiles.Where(dn => filter.FilterItem(dn) == false).ToArray();
            
            bool BackDiffCreated = Directory.Exists(BkdiffDir);

            // ===============================
            // sync source directory to mirror
            // ===============================
            foreach (string srcFile in SourceFiles) {

                // old-fashioned search loop -- so I know whats going on:
                int idxFound = -1;
                for (int iTarg = 0; iTarg < MirrorFiles.Length; iTarg++) {
                    if (MirrorFiles[iTarg] == null)
                        continue;

                    if (Path.GetFileName(MirrorFiles[iTarg]) == Path.GetFileName(srcFile)) {
                        idxFound = iTarg;
                        break;
                    }
                }

                if (idxFound < 0) {
                    // +++++++++++++++++++++++++++++++++++++++++++++++
                    // file is not present in mirror directory => copy
                    // +++++++++++++++++++++++++++++++++++++++++++++++
                    string MirrorFilePath = Path.Combine(MirrorDir, Path.GetFileName(srcFile));
                    SaveCopy(srcFile, MirrorFilePath, LogFileList, CopyAccessControlLists);

                } else {
                    string MirrorFile = MirrorFiles[idxFound];

                    bool WriteTimeOk = (File.GetLastWriteTimeUtc(MirrorFile) == File.GetLastWriteTimeUtc(srcFile));
                    //bool AttributesOk = (File.GetAttributes(MirrorFile) == File.GetAttributes(srcFile));
                    bool SizeOk = (new FileInfo(MirrorFile)).Length == (new FileInfo(srcFile)).Length;

                    if (WriteTimeOk && SizeOk) {
                        // ++++++++++++++++++++++++++++++++++++++++++++++++++
                        // file exists in mirror, equal size => Nothing to do
                        // ++++++++++++++++++++++++++++++++++++++++++++++++++

                        MirrorFiles[idxFound] = null;
                        MirrorFile = null;
                    } else {
                        // +++++++++++++++++++++++++++++++++
                        // something on file changed => copy
                        // +++++++++++++++++++++++++++++++++

                        if (File.Exists(MirrorFile)) {
                            // + + + + + + + + + + + + 
                            // move mirror to backdiff
                            // + + + + + + + + + + + + 

                            if (!BackDiffCreated) {
                                EnsureBackDiffDir(BkdiffDir, MirrorDir, RelPath, CopyAccessControlLists);
                                BackDiffCreated = true;
                            }

                            string BkdiffName = Path.Combine(BkdiffDir, Path.GetFileName(MirrorFile));
                            SaveMove(MirrorFile, BkdiffName, LogFileList, CopyAccessControlLists);
                        }

                        SaveCopy(srcFile, MirrorFile, LogFileList, CopyAccessControlLists);

                        MirrorFiles[idxFound] = null;
                        MirrorFile = null;
                    }
                }
            }

            // ==========================================
            // move untouched files in mirror to backdiff
            // ==========================================

            //
            // the remaining mirror files cannot be related to any file in the source directory
            // it seems they were deleted in the source directory by the user
            // -> move them to the backdiff
            //

            for (int iTarg = 0; iTarg < MirrorFiles.Length; iTarg++) {
                if (MirrorFiles[iTarg] == null)
                    continue;

                if (!BackDiffCreated) {
                    EnsureBackDiffDir(BkdiffDir, MirrorDir, RelPath, CopyAccessControlLists);
                    BackDiffCreated = true;
                }

                string BkdiffName = Path.Combine(BkdiffDir, Path.GetFileName(MirrorFiles[iTarg]));
                SaveMove(MirrorFiles[iTarg], BkdiffName, LogFileList, CopyAccessControlLists);

                MirrorFiles[iTarg] = null;
            }

        }

        private static void SaveMove(string MirrorFile, string BkdiffName, bool LogFileList, bool CopyAccessControlLists) {
            //if (Path.GetFileName(MirrorFile) == "Dopamine 1.5.12.0.msi")
            //    Console.Write(".");

            try {
                FileSecurity ac1 = null;
                if(CopyAccessControlLists)
                    ac1 = File.GetAccessControl(MirrorFile);
                if(LogFileList) {
                    Logmsg("Moving file: '{0}' -> '{1}'", MirrorFile, BkdiffName);
                }
                File.Move(MirrorFile, BkdiffName);
                //FileInfo BackdiffFile = new FileInfo(BkdiffName);

                // test copy
                if (!File.Exists(BkdiffName))
                    throw new BackupException("File move Failed", new FileInfo(BkdiffName));

                // try to set security
                if (CopyAccessControlLists) {
                    ac1.SetAccessRuleProtection(true, true);
                    File.SetAccessControl(BkdiffName, ac1);
                }

            } catch (Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                _stats.Errors++;
            }
        }

        private static void SaveDirectoryMove(string MirrorFile, string BkdiffName, bool LogFileList, bool CopyAccessControlLists) {
            try {

                DirectorySecurity ac1 = null;
                if(CopyAccessControlLists)
                    ac1 = Directory.GetAccessControl(MirrorFile);
                if(LogFileList) {
                    Logmsg("Moving directory: '{0}' -> '{1}'", MirrorFile, BkdiffName);
                }
                Directory.Move(MirrorFile, BkdiffName);
                //FileInfo BackdiffFile = new FileInfo(BkdiffName);

                // test copy
                if (!Directory.Exists(BkdiffName))
                    throw new BackupException("Directory move Failed", new DirectoryInfo(BkdiffName));

                // try to set security
                if (CopyAccessControlLists) {
                    ac1.SetAccessRuleProtection(true, true);
                    Directory.SetAccessControl(BkdiffName, ac1);
                }

            } catch (Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                _stats.Errors++;
            }
        }

        private static void EnsureBackDiffDir(string BkdiffDir, string MirrorDir, string[] RelPath, bool CopyAccessControlLists) {
            if (RelPath.Length <= 0)
                throw new ApplicationException();

            Debug.Assert(Path.GetFileName(BkdiffDir) == Path.GetFileName(MirrorDir));
            Debug.Assert(Path.GetFileName(BkdiffDir) == RelPath[RelPath.Length - 1]);

            string BaseBkdiffDir = Path.GetDirectoryName(BkdiffDir);
            if(!Directory.Exists(BaseBkdiffDir)) {
                EnsureBackDiffDir(BaseBkdiffDir, Path.GetDirectoryName(MirrorDir), RelPath.Take(RelPath.Length - 1).ToArray(), CopyAccessControlLists);
            }

            try {
                if (!Directory.Exists(BkdiffDir)) {
                    Directory.CreateDirectory(BkdiffDir); ;

                    // try to set security
                    if (CopyAccessControlLists) {
                        DirectorySecurity sec = Directory.GetAccessControl(MirrorDir);
                        sec.SetAccessRuleProtection(true, true);
                        Directory.SetAccessControl(BkdiffDir, sec);
                    }
                }
            } catch (Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                _stats.Errors++;
            }
        }

        private static void SaveCopy(string srcFile, string MirrorFilePath, bool LogFileList, bool CopyAccessControlLists) {
            try {
                FileSecurity ac1 = null;
                if(CopyAccessControlLists)
                    ac1 = (new FileInfo(srcFile)).GetAccessControl();

                FileInfo srcFileInfo = new FileInfo(srcFile);
                if(LogFileList) {
                    Logmsg("Copy: '{0}' -> '{1}'", srcFile, MirrorFilePath);
                }
                srcFileInfo.CopyTo(MirrorFilePath, false);

                FileInfo MirrorFile = new FileInfo(MirrorFilePath);

                // test copy
                if (!MirrorFile.Exists)
                    throw new BackupException("File Copy Failed", MirrorFile);

                // try to set security
                if (CopyAccessControlLists) {
                    ac1.SetAccessRuleProtection(true, true);
                    MirrorFile.SetAccessControl(ac1);
                }

                _stats.CopiedFiles++;
                _stats.CopiedBytes += srcFileInfo.Length;

            } catch (Exception e) {
                Error(e.GetType().Name + ": " + e.Message);
                _stats.Errors++;
            }
        }
    }
}
