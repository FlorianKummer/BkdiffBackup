using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BkdiffBackup {
    /// <summary>
    /// All user-configurable items 
    /// </summary>
    [Serializable]
    public class Configuration {

        /// <summary>
        /// Tuple, containing source and destination
        /// </summary>
        [Serializable]
        public class BkupDir {
            /// <summary>
            /// 
            /// </summary>
            public string DirectoryToBackup;

            /// <summary>
            /// Location of the mirror; backdiff folders will be created side-by-side.
            /// </summary>
            public string MirrorLocation;

            /// <summary>
            /// if true, each and every file copy will be logged
            /// </summary>
            public bool LogFileList = false;

            /// <summary>
            /// Copy the ACL's
            /// </summary>
            public bool CopyAccessControlLists = true;
        }

        /// <summary>
        /// All directories which should be back-uped
        /// </summary>
        public BkupDir[] Directories = new BkupDir[0];


        /// <summary>
        /// Used for control objects in work-flow management, 
        /// Converts object to a serializable text.
        /// </summary>
        public string Serialize() {
            JsonSerializer formatter = new JsonSerializer() {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                Formatting = Formatting.Indented
//                ObjectCreationHandling = ObjectCreationHandling.
            };
                        
            using(var tw = new StringWriter()) {
                //tw.WriteLine(this.GetType().AssemblyQualifiedName);
                using(JsonWriter writer = new JsonTextWriter(tw)) {  // Alternative: binary writer: BsonWriter
                    formatter.Serialize(writer, this);
                }

                string Ret = tw.ToString();
                return Ret;
            }
            
        }

        /// <summary>
        /// Used for control objects in work-flow management, 
        /// re-loads  an object from memory.
        /// </summary>
        public static Configuration Deserialize(string Str) {
            JsonSerializer formatter = new JsonSerializer() {
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ReferenceLoopHandling = ReferenceLoopHandling.Error
            };

            
            using(var tr = new StringReader(Str)) {
                //string typeName = tr.ReadLine();
                Type ControlObjectType = typeof(Configuration); //Type.GetType(typeName);
                using(JsonReader reader = new JsonTextReader(tr)) {
                    var obj = formatter.Deserialize(reader, ControlObjectType);

                    Configuration ctrl = (Configuration)obj;
                    return ctrl;
                }
              
            }
        }


   

    }
}
