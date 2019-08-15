using DeltaLauncher.Entities;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace DeltaLauncher
{
    class Program
    {
        public const float LAUNCHER_VERSION = 1.0f;
        public const string LAUNCHER_CHANNEL = "prod";

        public static Process server;

        static void Main(string[] args)
        {
            //Write launcher info
            WriteLineColor("Delta Web Map (C) RomanPort 2019", ConsoleColor.Cyan);
            WriteLineColor("Launcher Version " + LAUNCHER_VERSION.ToString("N1"), ConsoleColor.DarkCyan);
            Console.Title = $"Delta Web Map Server - Boot";

            //Download launcher remote config file
            LauncherRemoteConfig config = DownloadConfigFile();

            //Get our pathnames and create them if they don't exist
            string root = GetRootPathname();
            string content = Path.Combine(GetRootPathname(), "content\\");
            string saved = Path.Combine(content, "saved\\");
            string app_saved = Path.Combine(saved, "app\\");
            string app_bin = Path.Combine(content, "bin\\");
            string release_metadata_path = Path.Combine(saved, "release_metadata.json");
            MakePathIfNotExist(content);
            MakePathIfNotExist(saved);
            MakePathIfNotExist(app_saved);
            MakePathIfNotExist(app_bin);

            //Try and open the release data file
            ReleaseMetadataFile current_release = null;
            if (File.Exists(release_metadata_path))
                current_release = JsonConvert.DeserializeObject<ReleaseMetadataFile>(File.ReadAllText(release_metadata_path));

            //Install the app if we need to
            if (current_release == null)
                current_release = ReleaseUpdater.UpdateRelease(config, app_bin, release_metadata_path);

            //Update the app if we need to
            if(current_release.version_major < config.latest_release.version_major || current_release.version_minor < config.latest_release.version_minor)
                current_release = ReleaseUpdater.UpdateRelease(config, app_bin, release_metadata_path);

            //Write version
            WriteLineColor($"Client Version {current_release.version_major}.{current_release.version_minor}", ConsoleColor.DarkCyan);

            //Start the server
            server = StartServer(current_release, app_bin, app_saved);
            
            //Go into an updater loop
            while(true)
            {
                //Wait for the amount of time our policy states
                Thread.Sleep(config.launcher_config_sync_policy);

                //Redownload the config file
                try { config = DownloadConfigFile(); } catch { }

                //Update the app if we need to
                if (current_release.version_major < config.latest_release.version_major || current_release.version_minor < config.latest_release.version_minor)
                {
                    //We must update. Shut down the service
                    WriteLineColor($"There is an update. Upgrading from version {current_release.version_major}.{current_release.version_minor} to {config.latest_release.version_major}.{config.latest_release.version_minor}... Service will temporarily be interrupted.", ConsoleColor.Yellow);
                    server.Kill();

                    //Now, update
                    current_release = ReleaseUpdater.UpdateRelease(config, app_bin, release_metadata_path);

                    //Restart the server
                    server = StartServer(current_release, app_bin, app_saved);
                }

                //Check if the service crashed
                if(server.HasExited)
                {
                    WriteLineColor($"The service crashed. Going to restart...", ConsoleColor.Yellow);
                    server = StartServer(current_release, app_bin, app_saved);
                }
            }
        }

        public static Process StartServer(ReleaseMetadataFile metadata, string app_bin, string app_saved)
        {
            //Create launch options
            LaunchOptions options = new LaunchOptions
            {
                launcher_version = (int)LAUNCHER_VERSION,
                launcher_channel = LAUNCHER_CHANNEL,
                path_config = Path.Combine(app_saved, "config.json"),
                path_db = Path.Combine(app_saved, "database.db"),
                path_root = app_saved
            };

            //End the existing process if it's running
            if(server != null)
            {
                if (!server.HasExited)
                    server.Kill();
            }

            //Now, start the process
            server = Process.Start(new ProcessStartInfo
            {
                WorkingDirectory = app_bin,
                FileName = app_bin + metadata.app_exec,
                Arguments = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(options))),
                RedirectStandardError = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false                
            });
            Console.Title = $"Delta Web Map Server - v{metadata.version_major}.{metadata.version_minor}";
            return server;
        }

        public static LauncherRemoteConfig DownloadConfigFile()
        {
            //Download the file
            LauncherRemoteConfig config;
            string platform = ReleaseUpdater.GetReleaseBinaryName();
            try
            {
                using (HttpClient hc = new HttpClient())
                {
                    string config_content = hc.GetStringAsync($"https://config.deltamap.net/{Program.LAUNCHER_CHANNEL}/games/0/launcher_config.json?p={platform}&v={Program.LAUNCHER_VERSION}").GetAwaiter().GetResult();
                    config = JsonConvert.DeserializeObject<LauncherRemoteConfig>(config_content);
                }
            }
            catch (Exception ex)
            {
                //Failed
                Program.WriteLineColor("Sorry, could not download the configuration file. Try again later. (Are you offline?)", ConsoleColor.Red);
                throw new Exception();
            }
            return config;
        }

        public static void WriteLineColor(string msg, ConsoleColor c)
        {
            Console.ForegroundColor = c;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static string GetRootPathname()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static void MakePathIfNotExist(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
