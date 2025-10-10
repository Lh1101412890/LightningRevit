﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Xml;

using Microsoft.Win32;

using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;

namespace LightningRevit_Install
{
    /// <summary>
    /// Install.xaml 的交互逻辑
    /// </summary>
    public partial class Install : Window
    {
        private static bool Installing = false;
        private const string Product = "LightningRevit";
        private string Version;
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

        public Install()
        {
            InitializeComponent();
            Process[] processes = Process.GetProcessesByName("LightningMessage");
            foreach (var item in processes)
            {
                item.Kill();
            }
            Loaded += Install_Loaded;
        }

        /// <summary>
        /// 安装过程中程序不能被关闭
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (Installing)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 获取Revit打开状态
        /// </summary>
        /// <returns>true代表打开，否则未打开</returns>
        private bool GetRevitState()
        {
            return Process.GetProcessesByName("revit").Any();
        }

        /// <summary>
        /// 图片从右至左滑动切换
        /// </summary>
        /// <param name="nextImage"></param>
        private void SlideToNextImage(BitmapImage nextImage)
        {
            // 新图片初始在右侧
            Canvas.SetLeft(imageNew, imageCanvas.Width);
            imageNew.Source = nextImage;

            // 旧图片从0滑到左侧，新图片从右侧滑到0
            DoubleAnimation oldAnim = new DoubleAnimation(0, -imageCanvas.Width, TimeSpan.FromMilliseconds(500));
            DoubleAnimation newAnim = new DoubleAnimation(imageCanvas.Width, 0, TimeSpan.FromMilliseconds(500));

            oldAnim.Completed += (s, e) =>
            {
                // 动画结束后，imageOld显示新图片，imageNew清空
                imageOld.Source = nextImage;
                Canvas.SetLeft(imageOld, 0);
                imageNew.Source = null;
            };

            imageOld.BeginAnimation(Canvas.LeftProperty, oldAnim);
            imageNew.BeginAnimation(Canvas.LeftProperty, newAnim);
        }

        /// <summary>
        /// 初始化，并确认Revit是否打开占用了文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Install_Loaded(object sender, RoutedEventArgs e)
        {
            DirectoryInfo data = new DirectoryInfo("Data");
            DirectoryInfo dll = new DirectoryInfo("dll");
            FileSystemInfo[] fileSystemInfos = dll.GetFileSystemInfos("LightningRevit.dll", SearchOption.AllDirectories);
            if (fileSystemInfos.Length == 0 || !File.Exists("Data\\Lightning.ico") || !File.Exists("Data\\b站名片.jpg"))
            {
                MessageBox.Show("数据丢失!", Product);
                Close();
                return;
            }
            Task.Run(() =>//循环展示更新动态
            {
                int i = 0;
                FileInfo[] fileInfos = new DirectoryInfo("Data\\UpdatePictures").GetFiles();
                Dispatcher.Invoke(() =>
                {
                    image.Source = new BitmapImage(new Uri(fileInfos[i].FullName));
                    imageOld.Source = new BitmapImage(new Uri(fileInfos[i].FullName));
                });
                Thread.Sleep(4500);
                i++;
                while (true)
                {
                    if (i > fileInfos.Length - 1) { i = 0; continue; }
                    Dispatcher.Invoke(() =>
                    {
                        image.Source = null;
                        SlideToNextImage(new BitmapImage(new Uri(fileInfos[i].FullName)));
                        image.Source = new BitmapImage(new Uri(fileInfos[i].FullName));
                    });
                    Thread.Sleep(4500);
                    i++;
                }
            });
            Version = FileVersionInfo.GetVersionInfo(fileSystemInfos.First().FullName).ProductVersion;
            Title = $"{Product}_{Version}";
            location.Text = "C:\\Program Files\\Lightning\\" + Product;
            if (GetRevitState())
            {
                MessageBox.Show("请先关闭Revit!", Product);
                Close();
                return;
            }
        }

        /// <summary>
        /// 点击图片也可以拖动窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        /// <summary>
        /// 指定安装文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetLocationButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog()
            {
                ShowNewFolderButton = true,
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                location.Text = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// 切换到自定义
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelfDefiningButton_Click(object sender, RoutedEventArgs e)
        {
            grid2.Visibility = Visibility.Hidden;
            grid3.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 返回到快速安装
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            grid2.Visibility = Visibility.Visible;
            grid3.Visibility = Visibility.Hidden;
        }

        /// <summary>
        /// 安装程序，返回后设置的文件夹仍然生效
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(location.Text) || location.Text.Length <= 2)
            {
                return;
            }
            DirectoryInfo target;
            //检查安装目录是否合法
            try
            {
                target = new DirectoryInfo(location.Text);
                if (!target.Exists)
                {
                    target.Create();
                }
            }
            catch (ArgumentException)
            {
                MessageBox.Show("路径不合法!", Product);
                return;
            }
            catch (PathTooLongException)
            {
                MessageBox.Show("该路径长度太长!", Product);
                return;
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("未找到该盘符!", Product);
                return;
            }
            if (target.FullName.Length == 3)
            {
                MessageBox.Show("不允许安装到根目录！", Product, MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            //安装过程中，程序不能被关闭
            Installing = true;
            grid2.Visibility = Visibility.Hidden;
            grid3.Visibility = Visibility.Hidden;
            progress.Visibility = Visibility.Visible;
            Timer timer = new Timer() { Interval = 100 };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
            progress.Maximum = 100;

            Task.Run(() =>
            {
                CancellationToken ct = new CancellationToken();
                Task task = Task.Run(() =>
                {
                    //删除原包文件
                    using (RegistryKey product = Registry.LocalMachine.OpenSubKey($"Software\\Lightning\\{Product}", false))
                    {
                        if (product != null)
                        {
                            DirectoryInfo oldFolder = new DirectoryInfo(product.GetValue("Folder").ToString());
                            if (oldFolder.Exists)
                            {
                                oldFolder.Delete(true);
                            }
                        }
                    }

                    //生成包文件
                    DirectoryInfo data = new DirectoryInfo("Data");
                    if (!data.Exists || !File.Exists("Data\\Lightning.ico"))
                    {
                        Installing = false;
                        MessageBox.Show("数据丢失!", Product);
                        Close();
                        return;
                    }
                    target.Create();
                    CopyDir(data, target);

                    // 创建产品注册表信息，LightningCAD、LightningRevit、LightningOffice
                    using (RegistryKey software = Registry.LocalMachine.OpenSubKey("Software", true))
                    {
                        using (RegistryKey product = software.OpenSubKey($"Lightning\\{Product}", true) ?? software.CreateSubKey($"Lightning\\{Product}", true))
                        {
                            product.SetValue("Folder", target.FullName);
                            product.Close();
                        }
                        software.Close();
                    }
                    using (RegistryKey software = Registry.CurrentUser.OpenSubKey("Software", true))
                    {
                        using (RegistryKey product = software.CreateSubKey($"Lightning\\{Product}", true))
                        {
                            product.Close();
                        }
                        software.Close();
                    }

                    //先删除原有插件注册信息再添加
                    foreach (string version in Versions)
                    {
                        FileInfo file = new FileInfo($"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{version}\\LightningRevit.addin");
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }

                    //添加插件注册信息
                    List<string> strings = new List<string>();
                    foreach (string version in Versions)
                    {
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\Autodesk\\Revit\\{version}"))
                        {
                            if (key == null) continue;
                            string[] strs = key.GetSubKeyNames().Where(s => s.Contains(":")).ToArray();
                            foreach (string str in strs)
                            {
                                using (RegistryKey registryKey = key.OpenSubKey(str))
                                {
                                    object v = registryKey.GetValue("InstallationLocation");
                                    if (v != null)
                                    {
                                        string path = v.ToString();
                                        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                                        {
                                            XmlDocument xml = new XmlDocument();
                                            xml.Load(data.FullName + "\\LightningRevit.addin");
                                            XmlElement root = xml.DocumentElement;
                                            root.FirstChild["Assembly"].InnerText = $"{target.FullName}\\{version}\\LightningRevit.dll";
                                            root.FirstChild["AddInId"].InnerText = Guid.NewGuid().ToString();
                                            xml.Save($"C:\\ProgramData\\Autodesk\\Revit\\Addins\\{version}\\LightningRevit.addin");
                                        }
                                    }
                                    strings.Add(version);
                                    registryKey.Close();
                                }
                            }
                            key.Close();
                        }
                    }

                    //复制dll文件夹下的内容到各个已安装的CAD版本文件夹
                    foreach (var item in strings.Distinct())
                    {
                        DirectoryInfo directoryInfo = new DirectoryInfo(target.FullName + "\\" + item);
                        directoryInfo.Create();
                        DirectoryInfo dll = new DirectoryInfo("dll\\" + item);
                        CopyDir(dll, directoryInfo);
                    }

                    //卸载程序
                    using (RegistryKey registry = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", true))
                    {
                        using (RegistryKey key = registry.CreateSubKey(Product, true))
                        {
                            key.SetValue("DisplayIcon", $"{target.FullName}\\Lightning.ico", RegistryValueKind.String);//图标
                            key.SetValue("DisplayName", Product, RegistryValueKind.String);//显示名称
                            key.SetValue("DisplayVersion", Version, RegistryValueKind.String);//版本
                            key.SetValue("Publisher", "b站up主：不要干施工", RegistryValueKind.String);//发布者
                            key.SetValue("UninstallString", $"{target.FullName}\\uninstall.exe", RegistryValueKind.String);//卸载程序路径
                            key.SetValue("URLInfoAbout", "https://space.bilibili.com/191930682", RegistryValueKind.String);//网页
                            key.SetValue("EstimatedSize", GetSize(target) / 1024, RegistryValueKind.DWord);//程序大小
                            key.SetValue("Comments", "Revit插件，支持64位Revit" + Versions.First() + "-" + Versions.Last(), RegistryValueKind.String);//备注
                            key.Close();
                        }
                        registry.Close();
                    }

                    //安装完成，程序可以被关闭
                    Dispatcher.Invoke(() =>
                    {
                        // 进度条完成并清理
                        timer.Stop();
                        timer.Elapsed -= Timer_Elapsed;
                        timer.Dispose();
                        progress.Value = 100;
                        Installing = false;
                        MessageBox.Show("安装完成!", Product);
                        Close();
                    });
                }, ct);
                try
                {
                    task.Wait(ct);
                }
                catch (Exception exp)
                {
                    MessageBox.Show(exp.InnerException.Message);
                    Installing = false;
                    Dispatcher.Invoke(() => Close());
                }
            });
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (progress.Value >= progress.Maximum)
                {
                    progress.Value = 0;
                }
                progress.Value += 10;
            });
        }

        private void UpdateTextButton_Click(object sender, RoutedEventArgs e)
        {
            FileInfo update = new FileInfo("Data\\更新说明.txt");
            Process.Start("explorer", update.FullName);
        }

        /// <summary>
        /// 将source文件夹的所有内容复制到target文件夹下
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        private static void CopyDir(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (FileInfo fileInfo in source.GetFiles())
            {
                fileInfo.CopyTo(target.FullName + "\\" + fileInfo.Name, true);
            }
            foreach (DirectoryInfo directoryInfo in source.GetDirectories())
            {
                DirectoryInfo directory = new DirectoryInfo(target.FullName + "\\" + directoryInfo.Name);
                if (!directory.Exists) directory.Create();
                CopyDir(directoryInfo, directory);
            }
        }

        /// <summary>
        /// 获取文件夹大小
        /// </summary>
        /// <param name="directoryInfo"></param>
        /// <returns></returns>
        private long GetSize(DirectoryInfo directoryInfo)
        {
            long size = 0;
            foreach (var item in directoryInfo.GetFiles())
            {
                size += item.Length;
            }
            foreach (var item in directoryInfo.GetDirectories())
            {
                size += GetSize(item);
            }
            return size;
        }
    }
}