using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Color = Autodesk.Revit.DB.Color;

namespace LightningRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    //黑白背景切换
    public class BackgroundSwitch : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Color backColor = commandData.Application.Application.BackgroundColor;
            Color white = new Color(255, 255, 255);
            Color black = new Color(0, 0, 0);
            if (!(backColor.Red == 0 && backColor.Blue == 0 && backColor.Green == 0))
            {
                //必须要先变主题颜色，再变背景颜色，不然有bug
                UIThemeManager.CurrentTheme = UITheme.Dark;
                commandData.Application.Application.BackgroundColor = black;
            }
            else
            {
                UIThemeManager.CurrentTheme = UITheme.Light;
                commandData.Application.Application.BackgroundColor = white;
            }
            return Result.Succeeded;
        }
    }
}