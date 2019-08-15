using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaLauncher.Entities
{
    public class ReleaseMetadataFile
    {
        public int version_minor;
        public int version_major;
        public string release_channel;
        public DateTime time;
        public string app_exec;
    }
}
