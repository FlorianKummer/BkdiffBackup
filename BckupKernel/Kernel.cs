using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BckupKernel
{
    static public class Kernel {

        public static void Execute(
            string SourcePath,
            string MirrorPath,
            string BackDiffPath) {
            if (!Directory.Exists(SourcePath))
                throw new ArgumentException();
            if (!Directory.Exists(MirrorPath))
                throw new ArgumentException();
            if (!Directory.Exists(BackDiffPath))
                throw new ArgumentException();

            SyncDirsRecursive(SourcePath, MirrorPath, BackDiffPath, new string[0]);
        }

        static string[] Cat1(this string[] a, string b) {
            string[] R = new string[a.Length + 1];
            Array.Copy(a, R, a.Length);
            R[a.Length] = b;
            return R;
        }
        
        static void SyncDirsRecursive(string SourceDir, string MirrorDir, string BackDiffDir, string[] RelPath) {
            if (!Directory.Exists(SourceDir))
                throw new ArgumentException("source directory does not exist");

            Filter filter = new Filter(SourceDir);
            
            //string DirName = (new DirectoryInfo(SourceDir)).Name;

            //if (Path.GetFileName(MirrorDir) != DirName)
            //    throw new ArgumentException();
            //if (Path.GetFileName(BackDiffDir) != DirName)
            //    throw new ArgumentException();

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
                    Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
                }
            }

            // sync files
            SyncFilesInDir(SourceDir, MirrorDir, BackDiffDir, RelPath, filter);

            string[] SourceDirs; 
            try { 
                SourceDirs = Directory.GetDirectories(SourceDir); // files in the source directory
            } catch(Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
                SourceDirs = new string[0];
            }
                                 
            string[] MirrorDirs;
            try {
                MirrorDirs = Directory.GetDirectories(MirrorDir); // files already present in the mirror directory
            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
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
                if(idxFound >= 0) {
                    MirrorDirs[idxFound] = null;
                }

                SyncDirsRecursive(s, Path.Combine(MirrorDir, SubDirName), Path.Combine(BackDiffDir, SubDirName), RelPath.Cat1(SubDirName));
            }

            // move un-matched directories in mirror to backdiff
            bool BackDiffCreated = Directory.Exists(BackDiffDir);
            foreach (string s in MirrorDirs) {
                if (s == null)
                    continue;

                string SubBackDiffDir = Path.Combine(BackDiffDir, Path.GetFileName(s));

                if (!BackDiffCreated) {
                    EnsureBackDiffDir(BackDiffDir, MirrorDir, RelPath);
                    BackDiffCreated = true;
                }

                SaveDirectoryMove(s, SubBackDiffDir);
            }
        }


        static void SyncFilesInDir(string SourceDir, string MirrorDir, string BkdiffDir, string[] RelPath, Filter filter) {
           
            if (!Directory.Exists(SourceDir))
                throw new ArgumentException();
            if (!Directory.Exists(MirrorDir))
                throw new ArgumentException("Mirror directory does not exist.");

            // ==============
            // get file list
            // ==============
            string[] SourceFiles;
            try {
                SourceFiles = Directory.GetFiles(SourceDir); // files in the source directory
            } catch(Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
                SourceFiles = new string[0];
            }


            string[] MirrorFiles;
            try {
                MirrorFiles = Directory.GetFiles(MirrorDir); // files already present in the mirror directory
            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
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
                    SaveCopy(srcFile, MirrorFilePath);

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
                                EnsureBackDiffDir(BkdiffDir, MirrorDir, RelPath);
                                BackDiffCreated = true;
                            }

                            string BkdiffName = Path.Combine(BkdiffDir, Path.GetFileName(MirrorFile));
                            SaveMove(MirrorFile, BkdiffName);
                        }

                        SaveCopy(srcFile, MirrorFile);

                        MirrorFiles[idxFound] = null;
                        MirrorFile = null;
                    }
                }
            }

            // ==========================================
            // move untouched files in mirror to backdiff
            // ==========================================

            for (int iTarg = 0; iTarg < MirrorFiles.Length; iTarg++) {
                if (MirrorFiles[iTarg] == null)
                    continue;

                if (!BackDiffCreated) {
                    EnsureBackDiffDir(BkdiffDir, MirrorDir, RelPath);
                    BackDiffCreated = true;
                }

                string BkdiffName = Path.Combine(BkdiffDir, Path.GetFileName(MirrorFiles[iTarg]));
                SaveMove(MirrorFiles[iTarg], BkdiffName);

                MirrorFiles[iTarg] = null;
            }

        }

        private static void SaveMove(string MirrorFile, string BkdiffName) {
            //if (Path.GetFileName(MirrorFile) == "Dopamine 1.5.12.0.msi")
            //    Console.Write(".");

            try {
                                
                FileSecurity ac1 = File.GetAccessControl(MirrorFile);
                File.Move(MirrorFile, BkdiffName);
                //FileInfo BackdiffFile = new FileInfo(BkdiffName);

                // test copy
                if (!File.Exists(BkdiffName))
                    throw new BackupException("File move Failed", new FileInfo(BkdiffName));

                // try to set security
                ac1.SetAccessRuleProtection(true, true);
                File.SetAccessControl(BkdiffName, ac1);

            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
            }
        }

        private static void SaveDirectoryMove(string MirrorFile, string BkdiffName) {
            try {
                                
                DirectorySecurity ac1 = Directory.GetAccessControl(MirrorFile);
                Directory.Move(MirrorFile, BkdiffName);
                //FileInfo BackdiffFile = new FileInfo(BkdiffName);

                // test copy
                if (!Directory.Exists(BkdiffName))
                    throw new BackupException("Directory move Failed", new DirectoryInfo(BkdiffName));

                // try to set security
                ac1.SetAccessRuleProtection(true, true);
                Directory.SetAccessControl(BkdiffName, ac1);

            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
            }
        }

        private static void EnsureBackDiffDir(string BkdiffDir, string MirrorDir, string[] RelPath) {
            if (RelPath.Length <= 0)
                throw new ApplicationException();

            Debug.Assert(Path.GetFileName(BkdiffDir) == Path.GetFileName(MirrorDir));
            Debug.Assert(Path.GetFileName(BkdiffDir) == RelPath[RelPath.Length - 1]);

            string BaseBkdiffDir = Path.GetDirectoryName(BkdiffDir);
            if(!Directory.Exists(BaseBkdiffDir)) {
                EnsureBackDiffDir(BaseBkdiffDir, Path.GetDirectoryName(MirrorDir), RelPath.Take(RelPath.Length - 1).ToArray());
            }

            try {
                if (!Directory.Exists(BkdiffDir)) {
                    Directory.CreateDirectory(BkdiffDir); ;

                    // try to set security
                    DirectorySecurity sec = Directory.GetAccessControl(MirrorDir);
                    sec.SetAccessRuleProtection(true, true);
                    Directory.SetAccessControl(BkdiffDir, sec);
                }
            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
            }
        }

        private static void SaveCopy(string srcFile, string MirrorFilePath) {
            try {
                FileInfo srcFileInfo = new FileInfo(srcFile);
                srcFileInfo.CopyTo(MirrorFilePath, false);

                FileInfo MirrorFile = new FileInfo(MirrorFilePath);

                // test copy
                if (!MirrorFile.Exists)
                    throw new BackupException("File Copy Failed", MirrorFile);

                // try to set security
                FileSecurity ac1 = (new FileInfo(srcFile)).GetAccessControl();
                ac1.SetAccessRuleProtection(true, true);
                MirrorFile.SetAccessControl(ac1);
            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
            }
        }

        /*
        static void TraverseRecursive(out int dirCount, out int fileCount) {
            string[] Files;
            try {
                Files = Directory.GetFiles(path);
            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
                Files = new string[0];
            }
            fileCount = Files.Length;

            string[] Dirs;
            try {
                Dirs = Directory.GetDirectories(path);
            } catch (Exception e) {
                Console.Error.WriteLine(e.GetType().Name + ": " + e.Message);
                Dirs = new string[0];
            }
            dirCount = Dirs.Length;

            foreach (string subpath in Dirs) {
                TraverseRecursive(out int _dirCount, out int _fileCount, subpath);
                dirCount += _dirCount;
                fileCount += _fileCount;
            }
        }
        */
    }
}
