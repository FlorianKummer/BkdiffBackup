using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BckupKernel {
    class Filter {

        /// <summary>
        /// The default blacklist: files and directories which are never ever added to the backup
        /// </summary>
        public static string[] DefaultBlackList = {
            "*\\Users\\*\\AppData\\Local",
            "*\\Users\\*\\AppData\\LocalLow",
            "$Recycle.Bin",
            "ntuser.dat*",
            "desktop.ini",
            "thumbs.db"
        };

        private static Regex WildcardToRegex(string pattern) {
            return new Regex("^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Tests if a specific file or directory (<paramref name="Name"/>) should be in the backup or not
        /// </summary>
        /// <param name="Name">
        /// Absolute path of file-system item (file or directory) for which the filters should be tested
        /// </param>
        /// <returns>
        /// true if item <paramref name="Name"/> should *NOT* be in the backup
        /// </returns>
        public bool FilterItem(string Name) {
            string TopDirName = Path.GetFileName(Name);

            if (IncludeList != null) {
                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                // An include list is present in the directory:
                // if an include-list is present, the file must match some item on the list to be in the backup
                // it can be, however excluded by the black-list later
                // ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
                bool Include = false;

                foreach (var s in IncludeList) {

                    if (TopDirName.Equals(s, StringComparison.InvariantCultureIgnoreCase)) {
                        Include = true;
                        break;
                    }
                    if (Name.Equals(s, StringComparison.InvariantCultureIgnoreCase)) { 
                        Include = true;
                        break;
                    }

                    Regex sr = WildcardToRegex(s);


                    if (sr.IsMatch(TopDirName)) { 
                        Include = true;
                        break;
                    }
                    if (sr.IsMatch(Name)) { 
                        Include = true;
                        break;
                    }
                    
                }

                if (!Include) {
                    // No match with include list => file should *not* be in the backup
                    return true;
                }
            }



            if ((File.GetAttributes(Name) & FileAttributes.ReparsePoint) != 0)
                return true;

            foreach(var s in BlackList) {

                if (TopDirName.Equals(s, StringComparison.InvariantCultureIgnoreCase))
                    return true;
                if (Name.Equals(s, StringComparison.InvariantCultureIgnoreCase))
                    return true;

                Regex sr = WildcardToRegex(s);
                

                if (sr.IsMatch(TopDirName))
                    return true;
                if (sr.IsMatch(Name))
                    return true;
            }

            // if we reach this point, the file/directory should be in the backup
            return false;
        }


        string[] IncludeList;


        string[] BlackList;


        const string INCLUDE_LIST_NAME = "bkdiff-include-list.txt";

        const string INCLUDE_BLACK_LIST = "bkdiff-black-list.txt";

        /// <summary>
        /// Constructor: reads local include, resp. black-lists.
        /// </summary>
        /// <param name="Directory"></param>
        public Filter(string Directory) {
            string inc = Path.Combine(Directory, INCLUDE_LIST_NAME);

            if(File.Exists(inc)) {
                try {
                    IncludeList = File.ReadAllLines(inc);
                } catch(Exception e) {
                    Console.WriteLine(e.GetType().Name + " during reading include list '" + inc + "'");
                    IncludeList = null;
                } 
            } else {
                IncludeList = null;
            }

            string blk = Path.Combine(Directory, INCLUDE_BLACK_LIST);
            string[] _BlackList;
            if(File.Exists(blk)) {
                try {
                    _BlackList = File.ReadAllLines(blk);
                } catch(Exception e) {
                    Console.WriteLine(e.GetType().Name + " during reading black-list '" + inc + "'");
                    _BlackList = new string[0];
                } 
            } else {
                _BlackList = new string[0];
            }

            BlackList = new string[DefaultBlackList.Length + _BlackList.Length];
            Array.Copy(DefaultBlackList, BlackList, DefaultBlackList.Length);
            Array.Copy(_BlackList, 0, BlackList, DefaultBlackList.Length, _BlackList.Length);
        }


    }
}
