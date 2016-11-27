﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Input;

namespace VolvoWrench.Hotkey
{
    public partial class Hotkey : Form
    {
        public int DemoPopupKey;
        public int OverLayExitKey;
        public int OverLayRescanKey;

        public Hotkey()
        {
            KeyPreview = true;
            InitializeComponent();
            label1.Text = @"Demo info popup: " + KeyInterop.KeyFromVirtualKey(Main.DemoPopupKey);
            label2.Text = @"Overlay exit: " + KeyInterop.KeyFromVirtualKey(Main.OverLayExitKey);
            label3.Text = @"Overlay rescan: " + KeyInterop.KeyFromVirtualKey(Main.OverLayRescanKey);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Main.SettingsManager(false);
            label1.Text = @"Demo info popup: " + KeyInterop.KeyFromVirtualKey(Main.DemoPopupKey);
            label2.Text = @"Overlay exit: " + KeyInterop.KeyFromVirtualKey(Main.OverLayExitKey);
            label3.Text = @"Overlay rescan: " + KeyInterop.KeyFromVirtualKey(Main.OverLayRescanKey);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Main.SettingsManager(true);
            label1.Text = @"Demo info popup: " + KeyInterop.KeyFromVirtualKey(Main.DemoPopupKey);
            label2.Text = @"Overlay exit: " + KeyInterop.KeyFromVirtualKey(Main.OverLayExitKey);
            label3.Text = @"Overlay rescan: " + KeyInterop.KeyFromVirtualKey(Main.OverLayRescanKey);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Process.Start(Main.SettingsPath);
        }
    }
}