using System;
using System.IO;
using UnityEngine;

public static class PermissionManager
{
    public static bool IsAtApplicationRootPermissionDirectory(string path) => path == Application.persistentDataPath;
    
    public static bool HasPermissionsAtDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.LogError("[PermissionManager] Could not check directory permission since target directory does not exist.");
            return false;
        }

        string tempPath = path + Path.GetRandomFileName();
        try
        {
            using (var stream = File.Create(tempPath, 1, FileOptions.DeleteOnClose))
            {
                stream.WriteByte(0);
            }
            
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }
}
