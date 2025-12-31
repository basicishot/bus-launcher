using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;

namespace buslauncher
{
    public partial class MainWindow : Window
    {
        private busubs launcher;

        public MainWindow()
        {
            InitializeComponent();

            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "buslauncher");

            launcher = new busubs(root);
        }

        private void winloads(object sender, RoutedEventArgs e)
        {
            statustex.Text = "loading versions";

            try
            {
                List<string> versions = getvers();
                versionsbx.ItemsSource = versions;

                if (versions.Count > 0)
                    versionsbx.SelectedIndex = 0;

                statustex.Text = "ready";
            }
            catch (Exception ex)
            {
                statustex.Text = "failed";
                logbox.AppendText(ex + "\n");
            }
        }

        private void laubuttclicl(object sender, RoutedEventArgs e)
        {
            if (versionsbx.SelectedItem == null)
                return;

            string username = usernamez.Text.Trim();
            if (username.Length == 0)
                username = "player";

            statustex.Text = "launching";

            try
            {
                launcher.Launch(
                    versionsbx.SelectedItem.ToString(),
                    username
                ); // i hate circuit

                statustex.Text = "started";
            }
            catch (Exception ex)
            {
                statustex.Text = "error";
                logbox.AppendText(ex + "\n");
            }
        }

        private List<string> getvers()
        {
            string json = new WebClient().DownloadString(
                "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");

            var list = new List<string>();
            var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

            foreach (var v in obj["versions"])
                list.Add(v["id"].ToString());

            return list;
        }
    }
}
