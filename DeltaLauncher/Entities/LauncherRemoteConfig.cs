using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaLauncher.Entities
{
    public class LauncherRemoteConfig
    {
        public LauncherRemoteConfig_Release latest_release;
        public int launcher_config_sync_policy;
    }

    public class LauncherRemoteConfig_Release
    {
        public int version_major;
        public int version_minor;
        public Dictionary<string, LauncherRemoteConfig_Binary> binaries;
        public string change_notes;
    }

    public class LauncherRemoteConfig_Binary
    {
        public string download_url;
        public string exec_name;
    }
}
