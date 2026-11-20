using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using HemoIQ.Data;

namespace HemoIQ.Services
{
    /// <summary>
    /// Records a data export that is not a specimen or panel report (Analytics CSV, Audit Log CSV) in the audit log,
    /// with the file name, software version and a SHA-256 fingerprint of exactly what was written. These files
    /// carry accession numbers, so who exported what, and when, is kept as report output already is.
    /// </summary>
    public static class ExportAudit
    {
        public const string Action = "export_data";

        public static void Record(DatabaseService db, string kind, string filePath)
        {
            db.AppendAudit(Action, "export", kind, null, afterJson: JsonSerializer.Serialize(new
            {
                output = "csv",
                file = Path.GetFileName(filePath),
                sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(filePath))),
                softwareVersion = SoftwareIdentity.Version,
            }));
        }
    }
}
