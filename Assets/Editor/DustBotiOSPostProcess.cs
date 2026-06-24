#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace DustBot.Editor
{
    public static class DustBotiOSPostProcess
    {
        [PostProcessBuild(100)]
        public static void ConfigureInfoPlist(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
            {
                return;
            }

            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            PlistElementDict root = plist.root;
            root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            root.SetBoolean("UIRequiresFullScreen", true);
            root.SetBoolean("UIStatusBarHidden", true);
            root.SetBoolean("UIViewControllerBasedStatusBarAppearance", false);
            root.SetBoolean("UIApplicationSupportsIndirectInputEvents", true);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
