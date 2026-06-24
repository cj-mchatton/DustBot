using System;
using System.IO;
using UnityEngine;

namespace DustBot
{
    public sealed class SaveSystem
    {
        private const string FileName = "dustbot_save.json";
        private readonly string path;
        private readonly string backupPath;
        private readonly string temporaryPath;

        public SaveSystem()
        {
            path = Path.Combine(Application.persistentDataPath, FileName);
            backupPath = path + ".bak";
            temporaryPath = path + ".tmp";
        }

        public PlayerProgressData Load()
        {
            try
            {
                PlayerProgressData primary = TryLoadFile(path);
                if (primary != null)
                {
                    return primary;
                }

                PlayerProgressData backup = TryLoadFile(backupPath);
                return backup ?? new PlayerProgressData();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("DustBot save could not be loaded. Starting fresh. " + exception.Message);
                return new PlayerProgressData();
            }
        }

        public void Save(PlayerProgressData data)
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(data, false);
                File.WriteAllText(temporaryPath, json);

                if (File.Exists(path))
                {
                    try
                    {
                        File.Replace(temporaryPath, path, backupPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        ReplaceWithCopyFallback();
                    }
                    catch (IOException)
                    {
                        ReplaceWithCopyFallback();
                    }
                }
                else
                {
                    File.Move(temporaryPath, path);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("DustBot save could not be written. " + exception.Message);
            }
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("DustBot save could not be deleted. " + exception.Message);
            }
        }

        private static PlayerProgressData TryLoadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<PlayerProgressData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void ReplaceWithCopyFallback()
        {
            File.Copy(path, backupPath, true);
            File.Copy(temporaryPath, path, true);
            File.Delete(temporaryPath);
        }
    }
}
