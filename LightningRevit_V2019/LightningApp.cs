using System.Diagnostics;

using Autodesk.Revit.UI;

using Lightning.Extension;
using Lightning.Information;

using LightningRevit.Commands;
using LightningRevit.LightningExtension;

namespace LightningRevit
{
    public class LightningApp : IExternalApplication
    {
        private string assembly;
        private UIControlledApplication application;
        private static bool IsGod => PCInfo.IsGod;

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="msg">提示信息</param>
        /// <param name="time">显示时长</param>
        /// <param name="always">是否一直显示</param>
        public static void ShowMsg(string msg, int time = 0, bool always = false) => Information.God.ShowMessage(msg, time, always);

        public Result OnStartup(UIControlledApplication application)
        {
            if (!IsGod) ShowMsg("Lightning插件作者：【不要干施工】，点击去b站充电，插件群：785371506！", 25);

            this.application = application;
            assembly = Information.ProductModule.FullName;

            application.CreateRibbonTab(Information.Brand);
            CreatGeneral();
            CreatCTR();
            CreatOthers();

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            //关闭消息窗口
            Process[] processes = Process.GetProcessesByName("LightningMessage");
            foreach (var item in processes)
            {
                item.Kill();
            }
            return Result.Succeeded;
        }

        private void CreatGeneral()
        {
            RibbonPanel panel = application.CreateRibbonPanel(Information.Brand, "通用");

            //背景切换
            panel.AddItem(new PushButtonData("背景切换", "背景\n切换", assembly, typeof(BackgroundSwitch).FullName)
            {
                LargeImage = Information.GetFileInfo("Commands\\背景切换.png").ToBitmapImage().Resize(32),
                Image = Information.GetFileInfo("Commands\\背景切换.png").ToBitmapImage().Resize(16),
            });

            //连接设置
            panel.AddItem(new PushButtonData("连接设置", "连接\n设置", assembly, typeof(Connection).FullName)
            {
                LargeImage = Information.GetFileInfo("Commands\\连接设置.png").ToBitmapImage().Resize(32),
                Image = Information.GetFileInfo("Commands\\连接设置.png").ToBitmapImage().Resize(16),
            });

        }

        private void CreatCTR()
        {
            RibbonPanel panel = application.CreateRibbonPanel(Information.Brand, "CTR");

            //创建结构墙柱
            panel.AddItem(new PushButtonData("创建结构墙柱柱", "创建\n墙柱", assembly, typeof(CreatWallsColumns).FullName)
            {
                LargeImage = Information.GetFileInfo("Commands\\结构柱.png").ToBitmapImage().Resize(32),
                Image = Information.GetFileInfo("Commands\\结构柱.png").ToBitmapImage().Resize(16),
            });

            //创建结构梁
            panel.AddItem(new PushButtonData("创建结构梁", "创建\n梁", assembly, typeof(CreatBeam).FullName)
            {
                LargeImage = Information.GetFileInfo("Commands\\结构梁.png").ToBitmapImage().Resize(32),
                Image = Information.GetFileInfo("Commands\\结构梁.png").ToBitmapImage().Resize(16),
            });
        }

        private void CreatOthers()
        {
            //RibbonPanel panel = application.CreateRibbonPanel(Information.Brand, "其他");

        }
    }
}