using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Foto_CreaDB2;

/// <summary>
/// Locks the verified file against writes and replacement. Deletion uses that
/// same handle so a changed path cannot redirect it to another file.
/// </summary>
internal static class LockedFile
{
    private const uint GenericRead = 0x80000000;
    private const uint DeleteAccess = 0x00010000;
    private const uint OpenExisting = 3;
    private const uint OpenReparsePoint = 0x00200000;

    public static FileStream Open(string path, bool forDeletion = false)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("La cancellazione protetta richiede Windows.");
        if (!Path.IsPathFullyQualified(path))
            throw new IOException("È richiesto un percorso assoluto: " + path);

        string full = Path.GetFullPath(path);
        if (full.StartsWith(@"\\.\", StringComparison.Ordinal) ||
            full.AsSpan(Path.GetPathRoot(full)!.Length).Contains(':'))
            throw new IOException("Percorso dispositivo o stream alternativo non consentito: " + path);

        // Native CreateFile needs the extended prefix for long paths.
        string nativePath = full.StartsWith(@"\\?\", StringComparison.Ordinal) ? full
            : full.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + full[2..]
            : @"\\?\" + full;
        SafeFileHandle handle = CreateFileW(nativePath, GenericRead | (forDeletion ? DeleteAccess : 0),
            forDeletion ? 0u : (uint)FileShare.Read, IntPtr.Zero, OpenExisting, OpenReparsePoint, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            if (error == 2) throw new FileNotFoundException("File non trovato.", path);
            // An inaccessible directory is not proof that a file is absent.
            throw new IOException("Impossibile bloccare il file: " + path, new Win32Exception(error));
        }
        try
        {
            FileAttributes attributes = File.GetAttributes(handle);
            if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory | FileAttributes.Device)) != 0)
                throw new IOException("Collegamento o file speciale non consentito: " + path);
            return new FileStream(handle, FileAccess.Read);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static void Delete(FileStream file)
    {
        // FILE_DISPOSITION_INFO contains a one-byte BOOLEAN, not a Win32 BOOL.
        byte delete = 1;
        if (!SetFileInformationByHandle(file.SafeFileHandle, 4, ref delete, 1))
            throw new IOException("Impossibile eliminare il file verificato.", new Win32Exception(Marshal.GetLastWin32Error()));
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share,
        IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int informationClass,
        ref byte information, uint size);
}
