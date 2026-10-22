using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml;
using Microsoft.Win32;

namespace GhostifySetup
{
    public sealed class GameCompatibility
    {
        public string GameVersion { get; set; }
        public string Branch { get; set; }
        public string SteamBuild { get; set; }
        public string AssemblySha256 { get; set; }
    }
}
