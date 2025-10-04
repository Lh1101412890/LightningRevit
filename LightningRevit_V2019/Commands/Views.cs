using System;
using System.Collections.Generic;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using LightningRevit.LightningExtension;
using LightningRevit.Views;

namespace LightningRevit.Commands
{
    public class Views
    {
        private static readonly List<Type> types = new List<Type>();
        public static void ShowWindow(ViewBase view)
        {
            Type type = view.GetType();
            if (types.Contains(type))
            {
                LightningApp.ShowMessage("窗口已经打开了！", 2);
                return;
            }
            view.Show();
            view.Closed += (sender, e) => { types.Remove(type); };
            types.Add(type);
        }
    }

    /// <summary>
    /// 创建墙、柱
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CreatWallsColumns : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var view = new CreatWallsColumnsView(commandData.Application);
            Views.ShowWindow(view);
            return Result.Succeeded;
        }
    }

    /// <summary>
    /// 创建梁
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class CreatBeam : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var view = new CreatBeamView(commandData.Application);
            Views.ShowWindow(view);
            return Result.Succeeded;
        }
    }

    /// <summary>
    /// 连接设置
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Connection : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var view = new ConnectionView(commandData.Application);
            Views.ShowWindow(view);
            return Result.Succeeded;
        }
    }
}