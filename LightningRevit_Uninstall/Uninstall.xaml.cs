using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;

using Microsoft.Win32;

namespace LightningRevit_Uninstall
{
    /// <summary>
    /// Uninstall.xaml 的交互逻辑
    /// </summary>
    public partial class Uninstall : Window
    {
        private const string Product = "LightningRevit";
        readonly List<string> Versions = new List<string>
        {
            "2019",
            "2020",
            "2021",
            "2022",
            "2023",
            "2024",
            "2025",
            "2026",
        };

        public Uninstall()
        {
            InitializeComponent();
            Process[] processes = Process.GetProcessesByName("LightningMessage");
            foreach (var item in processes)
            {
                item.Kill();
            }
            Loaded += Uninstall_Loaded;
        }

        private void Uninstall_Loaded(object sender, RoutedEventArgs e)
        {
            Title = Product + "卸载程序";
            Hide();
            string folder = "";
            using (RegistryKey lightning = Registry.LocalMachine.OpenSubKey("Software\\Lightning", true))
            {
                if (lightning != null)
                {
                    RegistryKey key = lightning.OpenSubKey(Product);
                    if (key != null)
                    {
                        folder = key.GetValue("Folder").ToString();
                        lightning.Close();
                    }
                }
            }
            //判断是否已卸载
            if (folder == "")
            {
                MessageBox.Show("注册表信息已删除，请直接删除本文件夹！", Product);
                Close();
                return;
            }
            ;

            //确认是否关闭Revit
            if (Process.GetProcessesByName("revit").Length > 0)
            {
                MessageBox.Show("请先关闭Revit!", Product);
                Close();
                return;
            }

            //当前运行的卸载程序路径,在.net Framework中FriendlyName有后缀名（.exe）,在.net 8中不带后缀名
            string current = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;

            //安装目录的卸载程序
            string uninst = folder + "\\uninstall.exe";

            //需要他执行操作的卸载程序路径
            string temp = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\uninstall.exe";

            if (current == uninst)
            {
                File.Copy(uninst, temp, true);
                Close();
                Process.Start("explorer", temp);
            }
            else//真正的卸载操作
            {
                //自删除temp卸载程序进程信息
                ProcessStartInfo startInfo = new ProcessStartInfo("cmd.exe", "/C timeout /t 2 & Del " + temp)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    Verb = "runas",
                };


                //删除安装文件夹
                DirectoryInfo directory = new DirectoryInfo(folder);
                if (directory.Exists)
                {
                    directory.Delete(true);
                }

                //删除产品程序注册表信息
                using (RegistryKey lightning = Registry.LocalMachine.OpenSubKey("Software\\Lightning", true))
                {
                    if (lightning != null)
                    {
                        lightning.DeleteSubKeyTree(Product, false);
                        lightning.Close();
                    }
                }
                using (RegistryKey lightning = Registry.CurrentUser.OpenSubKey("Software\\Lightning", true))
                {
                    if (lightning != null)
                    {
                        lightning.DeleteSubKeyTree(Product, false);
                        lightning.Close();
                    }
                }

                //删除插件注册信息
                foreach (string version in Versions)
                {
                    FileInfo file = new FileInfo($"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{version}\\LightningRevit.addin");
                    if (file.Exists)
                    {
                        file.Delete();
                    }
                }

                //删除卸载程序注册表信息
                using (RegistryKey registry = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", true))
                {
                    registry.DeleteSubKeyTree(Product, false);
                    registry.Close();
                }

                Process.Start(startInfo);
                Close();
            }
        }
    }
}