﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BkdiffBackup.WinApp {

    [StructLayout(LayoutKind.Sequential)]
    public struct DevBroadcastVolume {
        public int Size;
        public int DeviceType;
        public int Reserved;
        public int Mask;
        public Int16 Flags;
    }

    public partial class Form : System.Windows.Forms.Form {
        public Form() {
            InitializeComponent();
        }

        private const int WM_DEVICECHANGE = 0x219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        private const int DBT_DEVTYP_VOLUME = 0x00000002;

        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);

            switch(m.Msg) {
                case WM_DEVICECHANGE:
                switch((int)m.WParam) {
                    case DBT_DEVICEARRIVAL:
                    listBox1.Items.Add("New Device Arrived");

                    int devType = Marshal.ReadInt32(m.LParam, 4);
                    if(devType == DBT_DEVTYP_VOLUME) {
                        DevBroadcastVolume vol;
                        vol = (DevBroadcastVolume)
                           Marshal.PtrToStructure(m.LParam,
                           typeof(DevBroadcastVolume));
                        listBox1.Items.Add("Mask is " + vol.Mask + ", size is = " + vol.Size);

                        DriveInfo[] drives = DriveInfo.GetDrives();
                        for(int i = 0; i < drives.Count(); i++) {
                            try {
                                listBox1.Items.Add("Drive " + i + ": " + drives[i].Name + " --- " + drives[i].VolumeLabel);
                            } catch (IOException ioe) {
                                listBox1.Items.Add("Drive " + i + ": " + ioe.Message);
                            }
                        }
                    }

                    break;

                    case DBT_DEVICEREMOVECOMPLETE:
                    listBox1.Items.Add("Device Removed");
                    break;

                }
                break;
            }

        }
    }
}
