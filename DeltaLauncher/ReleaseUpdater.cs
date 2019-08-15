using DeltaLauncher.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

namespace DeltaLauncher
{
    public static class ReleaseUpdater
    {
        public static ReleaseMetadataFile UpdateRelease(LauncherRemoteConfig config, string bin_pathname, string metadata_pathname)
        {
            //We're going to download the update now
            Console.Title = $"Delta Web Map Server - Updating...";
            string platform = GetReleaseBinaryName();
            using (MemoryStream ms = new MemoryStream())
            {
                //Download our binary
                Program.WriteLineColor($"Downloading update {config.latest_release.version_major}.{config.latest_release.version_minor}...", ConsoleColor.DarkCyan);
                try
                {
                    using (HttpClient hc = new HttpClient())
                    using (Stream ds = hc.GetStreamAsync(config.latest_release.binaries[platform].download_url).GetAwaiter().GetResult())
                        ds.CopyTo(ms);
                } catch (Exception ex)
                {
                    //Failed
                    Program.WriteLineColor("Sorry, could not download the update. Try again later.", ConsoleColor.Red);
                    throw new Exception();
                }

                //Open the ZIP file
                Program.WriteLineColor($"Extracting update {config.latest_release.version_major}.{config.latest_release.version_minor}...", ConsoleColor.DarkCyan);
                try
                {
                    using (ZipArchive za = new ZipArchive(ms, ZipArchiveMode.Read, true))
                    {
                        //Delete the existing binaries
                        Directory.Delete(bin_pathname, true);

                        //Create a new directory and extract
                        Directory.CreateDirectory(bin_pathname);
                        za.ExtractToDirectory(bin_pathname);
                    }
                } catch (Exception ex)
                {
                    //Failed
                    Program.WriteLineColor("Sorry, could not extract update. Try again later.", ConsoleColor.Red);
                    throw new Exception();
                }
            }

            //Now, we'll create the metadata and save it
            ReleaseMetadataFile metadata = new ReleaseMetadataFile
            {
                release_channel = Program.LAUNCHER_CHANNEL,
                time = DateTime.UtcNow,
                version_major = config.latest_release.version_major,
                version_minor = config.latest_release.version_minor,
                app_exec = config.latest_release.binaries[platform].exec_name
            };
            File.WriteAllText(metadata_pathname, JsonConvert.SerializeObject(metadata));
            Program.WriteLineColor($"Updated Delta Web Map to version {config.latest_release.version_major}.{config.latest_release.version_minor}!", ConsoleColor.DarkCyan);
            return metadata;
        }

        /// <summary>
        /// Gets the platform name
        /// </summary>
        /// <returns></returns>
        public static string GetReleaseBinaryName()
        {
            //Get the platform name
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "mac";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win64";

            //Not sure.
            Program.WriteLineColor("Sorry, this platform is not supported. Exiting...", ConsoleColor.Red);
            throw new Exception();
        }
    }
}
