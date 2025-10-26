using System;
using System.Windows;
using System.Windows.Forms;

using Autodesk.Revit.UI;

using Lightning.Extension;

using LightningRevit.Extension;
using LightningRevit.LightningExtension;

namespace LightningRevit.Views
{
    public class ViewBase : Window
    {
        /// <summary>
        /// 构造窗口时必须设置UIApplication，用于窗口初始读取数据
        /// </summary>
        protected UIApplication UIApplication;
        private readonly UIDocument UIDocument;
        public ViewBase(UIApplication uIApplication)
        {
            UIApplication = uIApplication;
            UIDocument = UIApplication.ActiveUIDocument;
            Icon = Information.GetFileInfo("Lightning.ico").ToBitmapImage();
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.Manual;
            SizeToContent = SizeToContent.WidthAndHeight;

            StartListen();

            SourceInitialized += ViewBase_SourceInitialized;
            MouseLeave += ViewBase_MouseLeave;
            Closed += ViewBase_Closed;
            ContentRendered += ViewBase_ContentRendered;

            UIApplication.ApplicationClosing += UIApplication_ApplicationClosing;
            UIApplication.ViewActivated += UIApplication_ViewActivated;
        }

        //保证打开窗口时，文档不被切换
        private void UIApplication_ViewActivated(object sender, Autodesk.Revit.UI.Events.ViewActivatedEventArgs e)
        {
            if (!e.Document.Equals(UIDocument.Document))
            {
                //暂时未实现
            }
        }

        private void UIApplication_ApplicationClosing(object sender, Autodesk.Revit.UI.Events.ApplicationClosingEventArgs e)
        {
            Close();
        }

        private void ViewBase_SourceInitialized(object sender, EventArgs e)
        {
            this.GetLocation(Information.God);
            IntPtr revit = UIApplication.MainWindowHandle;
            this.SetOwner(revit);
        }
        private void ViewBase_ContentRendered(object sender, EventArgs e)
        {
            UIApplication.Focus();
            ContentRendered -= ViewBase_ContentRendered;
        }

        private void ViewBase_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            UIApplication.Focus();
        }

        private void ViewBase_Closed(object sender, EventArgs e)
        {
            this.SetLocation(Information.God);
            StopListen();
            UIApplication.Focus();
        }

        public void StartListen()
        {
            myKeyEventHandeler = new KeyEventHandler(Hook_KeyDown);
            k_hook.KeyDownEvent += myKeyEventHandeler;//钩住键按下
            k_hook.Start();//安装键盘钩子
        }
        public void StopListen()
        {
            k_hook.KeyDownEvent -= myKeyEventHandeler;//取消按键事件
            k_hook.Stop();
        }

        private KeyEventHandler myKeyEventHandeler;//按键钩子事件处理器
        private readonly KeyboardHook k_hook = new KeyboardHook();

        private void Hook_KeyDown(object sender, KeyEventArgs e)
        {
            // Alt + ~ 关闭窗口 
            if (Control.ModifierKeys == Keys.Alt && e.KeyCode == Keys.Oemtilde)
            {
                Close();
            }
        }
    }
}