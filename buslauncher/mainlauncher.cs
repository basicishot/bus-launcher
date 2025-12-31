using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using Newtonsoft.Json.Linq;

// discord.gg/busclient

namespace buslauncher
{
    public class busubs
    {
        private readonly string root;
        private readonly string jexe =
            @"C:\Program Files\Java\jdk-25\bin\javaw.exe";

        public busubs(string rootDir)
        {
            root = rootDir;
            Directory.CreateDirectory(root);
        }

        public void Launch(string version, string username)
        {
            string verdir = Path.Combine(root, "versions", version);
            string nativdir = Path.Combine(verdir, "natives");
            string libsdir = Path.Combine(root, "libraries");
            string asstsdir = Path.Combine(root, "assets");
            string gamdir = Path.Combine(root, "game");

            Directory.CreateDirectory(verdir);
            Directory.CreateDirectory(nativdir);
            Directory.CreateDirectory(libsdir);
            Directory.CreateDirectory(asstsdir);
            Directory.CreateDirectory(gamdir);

            string cerjson = dversionjs(version, verdir);
            JObject vjson = JObject.Parse(cerjson);

            dassets(vjson);

            string clientJar = dclient(vjson, verdir, version);
            List<string> classpath = downloadlibandnatives(vjson, nativdir);

            classpath.Insert(0, clientJar);
            string cp = string.Join(Path.PathSeparator.ToString(), classpath);

            string mainClass = vjson["mainClass"] != null
                ? vjson["mainClass"].ToString()
                : "net.minecraft.client.main.Main";

            string asssinid = vjson["assetIndex"]["id"].ToString();

            string args =
                "-Xmx2G " +
                "-Djava.library.path=\"" + nativdir + "\" " +
                "-cp \"" + cp + "\" " +
                mainClass + " " +
                "--username " + username + " " +
                "--version " + version + " " +
                "--gameDir \"" + gamdir + "\" " +
                "--assetsDir \"" + asstsdir + "\" " +
                "--assetIndex " + asssinid + " " +
                "--uuid 00000000000000000000000000000000 " +
                "--accessToken 0 " +
                "--userType legacy" +
                "--userProperties {} " +
                "--profileProperties {} ";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = jexe,
                    Arguments = args,
                    WorkingDirectory = root,
                    UseShellExecute = false
                });
            }
            catch
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = @"C:\Program Files\Java\jre1.8.0_471\bin\javaw.exe",
                    Arguments = args,
                    WorkingDirectory = root,
                    UseShellExecute = false
                });
            }

        }

        private string dversionjs(string version, string dir)
        {
            string manifest = new WebClient().DownloadString(
                "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");

            JObject man = JObject.Parse(manifest);

            foreach (var v in man["versions"])
            {
                if (v["id"].ToString() == version)
                {
                    string json = new WebClient().DownloadString(v["url"].ToString());
                    File.WriteAllText(Path.Combine(dir, version + ".json"), json);
                    return json;
                }
            }

            throw new Exception("version not found");
        }

        private string dclient(JObject vjson, string dir, string version)
        {
            string jar = Path.Combine(dir, version + ".jar");

            if (!File.Exists(jar))
            {
                string url = vjson["downloads"]["client"]["url"].ToString();
                new WebClient().DownloadFile(url, jar);
            }

            return jar;
        }

        private void dassets(JObject vjson)
        {
            string assroot = Path.Combine(root, "assets");
            Directory.CreateDirectory(assroot);

            JObject assetIndex = (JObject)vjson["assetIndex"];
            string indexId = assetIndex["id"].ToString();
            string indexUrl = assetIndex["url"].ToString();

            string indexesDir = Path.Combine(assroot, "indexes");
            Directory.CreateDirectory(indexesDir);

            string indexPath = Path.Combine(indexesDir, indexId + ".json");
            if (!File.Exists(indexPath))
                new WebClient().DownloadFile(indexUrl, indexPath);

            JObject indexJson = JObject.Parse(File.ReadAllText(indexPath));
            JObject objects = (JObject)indexJson["objects"];

            string objectsDir = Path.Combine(assroot, "objects");
            Directory.CreateDirectory(objectsDir);

            foreach (var prop in objects.Properties())
            {
                string hash = prop.Value["hash"].ToString();
                string sub = hash.Substring(0, 2);

                string subDir = Path.Combine(objectsDir, sub);
                Directory.CreateDirectory(subDir);

                string outPath = Path.Combine(subDir, hash);
                if (File.Exists(outPath)) continue;

                string url = "https://resources.download.minecraft.net/" + sub + "/" + hash;
                new WebClient().DownloadFile(url, outPath);
            }
        }

        private List<string> downloadlibandnatives(JObject vjson, string nativdir)
        {
            var list = new List<string>();

            foreach (var lib in vjson["libraries"])
            {
                var downloads = lib["downloads"];
                if (downloads == null) continue;

                var art = downloads["artifact"];
                if (art != null)
                {
                    string path = Path.Combine(root, "libraries", art["path"].ToString());
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    if (!File.Exists(path))
                        new WebClient().DownloadFile(art["url"].ToString(), path);

                    list.Add(path);
                }

                var classifiers = downloads["classifiers"];
                if (classifiers != null && classifiers["natives-windows"] != null)
                {
                    var nat = classifiers["natives-windows"];
                    string npath = Path.Combine(root, "libraries", nat["path"].ToString());
                    Directory.CreateDirectory(Path.GetDirectoryName(npath));

                    if (!File.Exists(npath))
                        new WebClient().DownloadFile(nat["url"].ToString(), npath);

                    extnatives(npath, nativdir);
                }
            }

            return list;
        }

        private void extnatives(string jar, string outDir)
        {
            using (ZipArchive zip = ZipFile.OpenRead(jar))
            {
                foreach (var e in zip.Entries)
                {
                    if (string.IsNullOrEmpty(e.Name)) continue;
                    if (e.FullName.StartsWith("META-INF")) continue;

                    string outPath = Path.Combine(outDir, e.Name);
                    e.ExtractToFile(outPath, true);
                }
            }
        }
    }
}
