using System.Diagnostics;
using System.IO;

using Lightning.Manager;

using Microsoft.Win32;

namespace LightningRevit.LightningExtension
{
    public class Information
    {
        internal static God God = new God(GodEnum.Revit);

        /// <summary>
        /// "Lightning"
        /// </summary>
        public static string Brand => God.Brand;

        public static string ProductName => God.ProductName;

        /// <summary>
        /// 错误日志路径
        /// </summary>
        public static string ErrorLog => God.ErrorLog;

        /// <summary>
        /// 获取随包安装文件路径
        /// </summary>
        /// <param name="lastPath"></param>
        /// <returns></returns>
        public static FileInfo GetFileInfo(string lastPath)
        {
            return God.GetFileInfo(lastPath);
        }

        /// <summary>
        /// 软件版本，20**
        /// </summary>
        public static string Version =>
#if R19
                 "2019";
#elif R20
                 "2020";
#elif R21
                 "2021";
#elif R22
                 "2022";
#elif R23
                 "2023";
#elif R24
                 "2024";
#elif R25
                 "2025";
#elif R26
                 "2026";
#endif

        /// <summary>
        /// 主模块文件
        /// </summary>
        /// <param name="revitVersion"></param>
        /// <returns></returns>
        public static FileInfo ProductModule
        {
            get
            {
                FileInfo fileInfo = null;
                using (RegistryKey registry = Registry.LocalMachine.OpenSubKey($"Software\\Lightning\\{ProductName}"))
                {
                    if (registry != null)
                    {
                        string dirBase = registry.GetValue("Folder").ToString();
                        string file = $"{dirBase}\\{Version}\\{ProductName}.dll";
                        fileInfo = new FileInfo(file);
                        registry.Close();
                    }
                    else
                    {
#if DEBUG
                        string dr = "Debug";
#else
                        string dr = "Release";
#endif
#if R25 || R26
                        fileInfo = new FileInfo($"D:\\Visual Studio 2022 Projects\\{ProductName}\\{ProductName}_V{Version}\\bin\\x64\\{dr}\\net8.0-windows8.0\\{ProductName}.dll");
#else
                        fileInfo = new FileInfo($"D:\\Visual Studio 2022 Projects\\{ProductName}\\{ProductName}_V{Version}\\bin\\x64\\{dr}\\{ProductName}.dll");
#endif
                    }
                    return fileInfo;
                }
            }
        }

        /// <summary>
        /// 插件版本
        /// </summary>
        public static string ProductVersion => FileVersionInfo.GetVersionInfo(ProductModule.FullName).ProductVersion;

    }
}