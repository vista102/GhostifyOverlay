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
    public sealed class InstallResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Backup { get; set; }
    }
}
