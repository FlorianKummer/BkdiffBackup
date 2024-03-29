﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BkdiffBackup {
    
    
    /// <summary>
    /// Filtering of files/subdirectories to include or exclude
    /// - if an include list is present in some directory (a file named <see cref="INCLUDE_LIST_NAME"/>) **only** files/directories in this list are considered
    /// - if a blacklist is present (a file named <see cref="BLACK_LIST"/>) every file/directory which matches some entry of the blacklist is omitted.
    ///   The **blacklist dominates the include list**, i.e. if the same item is specified in the include and the black-list, the item is omitted.
    /// </summary>
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
            "thumbs.db",
            ".ds_store",
            "*\\bin\\Debug",
            "*\\bin\\Release",
            "*\\obj\\Debug",
            "*\\obj\\Release"
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


            try {
                if ((File.GetAttributes(Kernel.FixLongPath(Name)) & FileAttributes.ReparsePoint) != 0)
                    return true;
            } catch (Exception e) {
                Kernel.Error(e);
                return true; // something weired with file 
            }



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


        public const string INCLUDE_LIST_NAME = "bkdiff-include-list.txt";

        public const string BLACK_LIST = "bkdiff-black-list.txt";

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
                    Kernel.Error(e, " during reading include list '" + inc + "'");
                    IncludeList = null;
                } 
            } else {
                IncludeList = null;
            }

            string blk = Path.Combine(Directory, BLACK_LIST);
            string[] _BlackList;
            if(File.Exists(blk)) {
                try {
                    _BlackList = File.ReadAllLines(blk);
                } catch(Exception e) {
                    Kernel.Error(e, " during reading black-list '" + inc + "'");
                    _BlackList = new string[0];
                } 
            } else {
                _BlackList = new string[0];
            }

            SanitizeList(ref IncludeList);
            SanitizeList(ref _BlackList);


            BlackList = new string[DefaultBlackList.Length + _BlackList.Length];
            Array.Copy(DefaultBlackList, BlackList, DefaultBlackList.Length);
            Array.Copy(_BlackList, 0, BlackList, DefaultBlackList.Length, _BlackList.Length);
        }


        static void SanitizeList(ref string[] myList) {
            if(myList == null)
                return;
            List<string> temp = new List<string>(myList);
            for(int i = 0; i < temp.Count; i++) {
                temp[i] = temp[i].Trim();
                if(temp[i].Length <= 0) {
                    temp.RemoveAt(i);
                    i--;
                }
            }

            myList = temp.ToArray();
        }


    }
}
