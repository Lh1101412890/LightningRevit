using Autodesk.Revit.UI;

using Lightning.Extension;

namespace LightningRevit.Extension
{
    public static class UIApplicationExtension
    {
        public static void Focus(this UIApplication uIApplication)
        {
            uIApplication.MainWindowHandle.Focus();
        }
    }
}
