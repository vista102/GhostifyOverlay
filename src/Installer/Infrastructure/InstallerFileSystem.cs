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
using static GhostifySetup.InstallerEngine;

namespace GhostifySetup
{
    internal static class InstallerFileSystem
    {
        internal static void ValidateRelative(string path)
        {
            if (string.IsNullOrEmpty(path) || Path.IsPathRooted(path) || path.Contains(":") || path.Split('\\', '/').Any(p => p == ".." || p == "." || p.Length == 0))
                throw new InvalidDataException("설치 파일 경로가 올바르지 않습니다.");
        }

        internal static string Child(string parent, string relative)
        {
            ValidateRelative(relative);
            string path = Path.GetFullPath(Path.Combine(parent, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(Path.GetFullPath(parent).TrimEnd('\\', '/') + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("설치 경로가 허용 범위를 벗어났습니다.");
            return path;
        }

        internal static void NoRedirect(string path)
        {
            if ((Directory.Exists(path) || File.Exists(path)) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("연결된 폴더나 파일에는 설치할 수 없습니다: " + path);
        }

        internal static void SafeTree(string path)
        {
            NoRedirect(path);
            if (!Directory.Exists(path))
                return;
            foreach (string entry in Directory.EnumerateFileSystemEntries(path))
            {
                NoRedirect(entry);
                if (Directory.Exists(entry))
                    SafeTree(entry);
            }
        }

        internal static Dictionary<string, object> JsonFile(string path)
        {
            return new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8));
        }

        internal static XmlDocument LoadParams(string path)
        {
            var doc = new XmlDocument
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };
            if (File.Exists(path))
            {
                using (var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    doc.Load(reader);
            }
            else
                doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\"?><Param><ModParams /></Param>");
            if (doc.SelectSingleNode("/Param/ModParams") == null)
                throw new InvalidDataException("Unity Mod Manager 설정 파일을 확인해 주세요.");
            return doc;
        }

        internal static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "");
        }

        internal static string HashFile(string file)
        {
            using (var hash = SHA256.Create())
            using (Stream stream = File.OpenRead(file))
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "");
        }

        internal static void AtomicBytes(string target, byte[] bytes)
        {
            string temporary = target + ".Ghostify-" + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, bytes);
                if (File.Exists(target))
                    File.Replace(temporary, target, null);
                else
                    File.Move(temporary, target);
            }
            finally
            {
                if (File.Exists(temporary))
                    File.Delete(temporary);
            }
        }

        internal static void CopyTree(string source, string destination)
        {
            SafeTree(source);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(source.Length).TrimStart('\\', '/'), output = Child(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.Copy(file, output, false);
                if (HashFile(file) != HashFile(output))
                    throw new IOException("백업 파일 확인에 실패했습니다.");
            }
        }

        internal static Dictionary<string, string> UserHashes(string target)
        {
            string data = Child(target, "UserData");
            SafeTree(data);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (Directory.Exists(data))
                foreach (string file in Directory.GetFiles(data, "*", SearchOption.AllDirectories))
                    result.Add(file.Substring(data.Length), HashFile(file));
            return result;
        }

        internal static void VerifyUsers(Dictionary<string, string> before, string target)
        {
            var after = UserHashes(target);
            if (before.Count != after.Count || before.Any(p => !after.ContainsKey(p.Key) || after[p.Key] != p.Value))
                throw new IOException("기존 설정 보존 확인에 실패했습니다.");
        }

        internal static void RemoveEmptyDirectories(string root)
        {
            NoRedirect(root);
            foreach (string child in Directory.GetDirectories(root))
                RemoveEmptyDirectories(child);
            if (!Directory.EnumerateFileSystemEntries(root).Any())
                Directory.Delete(root);
        }
    }
}
