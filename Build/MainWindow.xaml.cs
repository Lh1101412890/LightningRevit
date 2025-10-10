using System.Diagnostics;
using System.IO;
using System.Windows;

using Lightning.Extension;

namespace Build
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += MainWindow_SourceInitialized;
        }

        private void MainWindow_SourceInitialized(object? sender, EventArgs e)
        {
            bool isDebug = false;
#if DEBUG
            isDebug = true;
#endif
            if (isDebug)
            {
                MessageBox.Show("请运行Release版本");
                Close();
                return;
            }
            // 项目位置
            string local = "D:\\Visual Studio 2022 Projects";
            string product = "LightningRevit";

            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo($"{local}\\{product}\\{product}_V2019\\bin\\x64\\Release\\{product}.dll");
            //创建文件夹
            DirectoryInfo release = new($"{local}\\Products\\{product} {fileVersionInfo.ProductVersion}");
            release.Create();


            //复制Data文件夹
            DirectoryInfo ndata = new($"{release.FullName}\\Data");
            ndata.Create();
            DirectoryInfo data = new($"{local}\\{product}\\Data");
            LTools.XCopy(data, ndata);

            //复制族文件夹
            DirectoryInfo nku = new($"{release.FullName}\\库");
            nku.Create();
            DirectoryInfo ku = new($"{local}\\{product}\\库");
            LTools.XCopy(ku, nku);

            //复制安装程序
            FileInfo setup = new($"{local}\\{product}\\{product}_Install\\bin\\x64\\Release\\Setup.exe");
            setup.CopyTo(new FileInfo($"{release.FullName}\\{setup.Name}"));

            //复制卸载程序
            FileInfo uninstall = new($"{local}\\{product}\\{product}_Uninstall\\bin\\x64\\Release\\uninstall.exe");
            uninstall.CopyTo(new FileInfo($"{ndata.FullName}\\{uninstall.Name}"));

            //复制2019-2024版本插件
            string[] strings1 = ["2019", "2020", "2021", "2022", "2023", "2024"];
            foreach (string s in strings1)
            {
                DirectoryInfo directory = new($"{local}\\{product}\\{product}_V{s}\\bin\\x64\\Release");
                DirectoryInfo nversion = new($"{release.FullName}\\dll\\{s}");
                if (!nversion.Exists)
                    nversion.Create();
                LTools.XCopy(directory, nversion, ".dll", false);
            }

            //复制2025-2026版本插件
            string[] strings2 = ["2025", "2026"];
            foreach (string s in strings2)
            {
                DirectoryInfo directory = new($"{local}\\{product}\\{product}_V{s}\\bin\\x64\\Release\\net8.0-windows8.0");
                DirectoryInfo nversion = new($"{release.FullName}\\dll\\{s}");
                if (!nversion.Exists)
                    nversion.Create();
                LTools.XCopy(directory, nversion, ".dll", false);
            }

            //打开文件夹
            Process.Start("explorer", release.FullName);
            Close();
        }
    }
}