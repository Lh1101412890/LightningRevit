using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

using Lightning.Information;

using LightningRevit.Extension;
using LightningRevit.LightningExtension;
using LightningRevit.Models;

using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace LightningRevit.Views
{
    /// <summary>
    /// CreatBeamView.xaml 的交互逻辑
    /// </summary>
    public partial class CreatBeamView : ViewBase
    {
        private readonly ExternalEventHandler handlerEvent;
        public CreatBeamView(UIApplication uIApplication) : base(uIApplication)
        {
            InitializeComponent();
            SourceInitialized += CreatColumnView_SourceInitialized;
            file.PreviewDrop += File_PreviewDrop;
            file.PreviewDragOver += File_PreviewDragOver;
            handlerEvent = new ExternalEventHandler(Creat, "创建结构梁");
        }
        private void File_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        /// <summary>
        /// 允许拖动数据文件至窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void File_PreviewDrop(object sender, DragEventArgs e)
        {
            foreach (string f in (string[])e.Data.GetData(DataFormats.FileDrop))
            {
                file.Text = f;
            }
        }
        private void File_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Multiselect = false,
                Filter = "CAD到Revit建模数据文件|*.ctr",
                InitialDirectory = PCInfo.MyDocumentsDirectoryInfo + "\\Lightning\\LightningRevit",
                AddExtension = true,
                Title = "选择柱数据",
            };
            bool? result = fileDialog.ShowDialog();
            if (result.Value != true)
            {
                return;
            }
            file.Text = fileDialog.FileName;
        }

        /// <summary>
        /// 获取所有标高
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CreatColumnView_SourceInitialized(object sender, EventArgs e)
        {
            // 获取当前文档
            Document doc = UIApplication.ActiveUIDocument.Document;

            // 创建过滤器集合
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            // 过滤标高元素
            var elements = collector.OfClass(typeof(Level)).ToElements();
            foreach (var item in elements)
            {
                levels.Items.Add(item.Name);
            }
            levels.SelectedIndex = 0;
        }

        private void Creat_Click(object sender, RoutedEventArgs e)
        {
            handlerEvent.Raise();
        }

        private void Creat(UIApplication uIApplication)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(file.Text);
                if (!fileInfo.Exists)
                {
                    LightningApp.ShowMessage("文件不存在", 3);
                    return;
                }
            }
            catch (ArgumentException)
            {
                LightningApp.ShowMessage("路径不合法", 3);
                return;
            }
            if (levels.SelectedIndex == -1)
            {
                LightningApp.ShowMessage("未选择创建标高", 3);
                return;
            }
            Close();
            Autodesk.Revit.ApplicationServices.Application app = UIApplication.Application;
            UIDocument uIDocument = UIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;
            XYZ align;
            try
            {
                LightningApp.ShowMessage("指定基点（同CAD）", 0, true);
                align = uIDocument.Selection.PickPoint("指定基点");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                LightningApp.ShowMessage("", 0);
                return;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                LightningApp.ShowMessage("视图未激活，请移动当前视图后重试", 2);
                return;
            }

            LightningApp.ShowMessage("创建结构梁", 2);


            FilteredElementCollector collector = new FilteredElementCollector(document);
            Family family = collector.OfClass(typeof(Family)).Cast<Family>().ToList().Find(f => f.Name == "混凝土 - 矩形梁");

            if (family == null)
            {
                using (Transaction transaction = new Transaction(document, "创建结构梁族"))
                {
                    transaction.Start();
                    // 导入族类型
                    string familyPath = Information.GetFileInfo($"族文件\\{app.VersionNumber}\\混凝土 - 矩形梁.rfa").FullName;
                    if (!document.LoadFamily(familyPath, out family))
                    {
                        LightningApp.ShowMessage("无法加载族文件: " + familyPath, 3);
                        return;
                    }
                    transaction.Commit();
                }
            }

            var sizes = ReadSizes();
            foreach (var item in sizes)
            {
                FamilySymbol familySymbol = null;
                foreach (ElementId id in family.GetFamilySymbolIds())
                {
                    FamilySymbol symbol = document.GetElement(id) as FamilySymbol;
                    if (symbol.Name == item)
                    {
                        familySymbol = symbol;
                        break;
                    }
                }

                if (familySymbol == null)
                {
                    //新建类型
                    Document familyDoc = document.EditFamily(family);
                    FamilyManager familyManager = familyDoc.FamilyManager;
                    using (Transaction transaction = new Transaction(familyDoc, "创建梁类型"))
                    {
                        transaction.Start();
                        FamilyType familyType = familyManager.NewType(item);
                        familyManager.Set(familyManager.get_Parameter("b"), double.Parse(item.Split('x')[0]) / 304.8);
                        familyManager.Set(familyManager.get_Parameter("h"), double.Parse(item.Split('x')[1]) / 304.8);
                        transaction.Commit();
                    }

                    family = familyDoc.LoadFamily(document, new FamilyLoadOptions(false, FamilySource.Family));
                    familyDoc.Close(false);
                }
            }

            var beamModels = ReadBeams();
            using (Transaction trans = new Transaction(document, "创建梁"))
            {
                trans.Start();
                document.Regenerate();
                foreach (var item in beamModels)
                {
                    string symbolName = item.Width.ToString() + "x" + item.Height.ToString();
                    FamilySymbol familySymbol = null;
                    foreach (ElementId id in family.GetFamilySymbolIds())
                    {
                        FamilySymbol symbol = document.GetElement(id) as FamilySymbol;
                        if (symbol.Name == symbolName)
                        {
                            familySymbol = symbol;
                            break;
                        }
                    }

                    // 激活族类型
                    if (!familySymbol.IsActive)
                    {
                        familySymbol.Activate();
                        document.Regenerate();
                    }

                    // 获取标高
                    Level level = new FilteredElementCollector(document)
                        .OfClass(typeof(Level))
                        .Cast<Level>()
                        .FirstOrDefault(l => l.Name == levels.SelectedItem.ToString());

                    if (level == null)
                    {
                        LightningApp.ShowMessage("未找到指定的标高", 3);
                        return;
                    }

                    // 创建梁
                    Line line = Line.CreateBound(item.Start + align, item.End + align);
                    FamilyInstance beam = document.Create.NewFamilyInstance(line, familySymbol, level, StructuralType.Beam);
                }
                LightningApp.ShowMessage("创建完成", 2);
                trans.Commit();
            }
        }

        private List<BeamModel> ReadBeams()
        {
            // 获取标高
            Level level = new FilteredElementCollector(UIApplication.ActiveUIDocument.Document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .FirstOrDefault(l => l.Name == levels.SelectedItem.ToString());
            double z = level == null ? 0 : level.Elevation;

            XmlDocument xml = new XmlDocument();
            xml.Load(file.Text);
            XmlElement root = xml.DocumentElement;
            List<BeamModel> beamModels = new List<BeamModel>();
            foreach (XmlNode item in root["BeamModels"].ChildNodes)
            {
                string start = item["Start"].InnerText;
                double x1 = double.Parse(start.Split(',').First()) / 304.8;
                double y1 = double.Parse(start.Split(',').Last()) / 304.8;
                string end = item["End"].InnerText;
                double x2 = double.Parse(end.Split(',').First()) / 304.8;
                double y2 = double.Parse(end.Split(',').Last()) / 304.8;
                BeamModel beamModel = new BeamModel()
                {
                    Start = new XYZ(x1, y1, z),
                    End = new XYZ(x2, y2, z),
                    Width = int.Parse(item["Width"].InnerText),
                    Height = int.Parse(item["Height"].InnerText),
                };
                beamModels.Add(beamModel);
            }
            return beamModels;
        }

        private List<string> ReadSizes()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(file.Text);
            XmlElement root = xml.DocumentElement;
            List<string> sizes = new List<string>();
            foreach (XmlNode item in root["Sizes"].ChildNodes)
            {
                sizes.Add(item.InnerText);
            }
            return sizes;
        }
    }
}