using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

using Lightning.Extension;
using Lightning.Information;

using LightningRevit.Extension;
using LightningRevit.LightningExtension;
using LightningRevit.Models;

using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using View = Autodesk.Revit.DB.View;
using Wall = Autodesk.Revit.DB.Wall;

namespace LightningRevit.Views
{
    /// <summary>
    /// CreatWallsColumnsView.xaml 的交互逻辑
    /// </summary>
    public partial class CreatWallsColumnsView : ViewBase
    {
        private readonly ExternalEventHandler handlerEvent;
        public CreatWallsColumnsView(UIApplication uIApplication) : base(uIApplication)
        {
            InitializeComponent();
            SourceInitialized += CreatColumnView_SourceInitialized;
            file.PreviewDrop += File_PreviewDrop;
            file.PreviewDragOver += File_PreviewDragOver;
            handlerEvent = new ExternalEventHandler(Creat, "创建结构墙柱");
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
                    LightningApp.ShowMsg("文件不存在", 3);
                    return;
                }
            }
            catch (ArgumentException)
            {
                LightningApp.ShowMsg("路径不合法", 3);
                return;
            }
            if (levels.SelectedIndex == -1)
            {
                LightningApp.ShowMsg("未选择创建标高", 3);
                return;
            }
            Close();
            Autodesk.Revit.ApplicationServices.Application app = UIApplication.Application;
            UIDocument uIDocument = UIApplication.ActiveUIDocument;
            Document document = uIDocument.Document;
            XYZ align;
            try
            {
                LightningApp.ShowMsg("指定基点（同CAD）", 0, true);
                align = uIDocument.Selection.PickPoint("指定基点");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                LightningApp.ShowMsg("", 0);
                return;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                LightningApp.ShowMsg("视图未激活，请移动当前视图后重试", 2);
                return;
            }

            LightningApp.ShowMsg("正在创建结构柱族。。。", 0, true);
            try
            {
                XmlDocument xml = new XmlDocument();
                xml.Load(file.Text);
                XmlElement root = xml.DocumentElement;
                string prefix = root["Prefix"].InnerText;
                string postfix = root["Postfix"].InnerText;

                //读取大样
                List<ColumnDetail> details = new List<ColumnDetail>();
                foreach (XmlNode item in root["Details"].ChildNodes)
                {
                    ColumnDetail detail = new ColumnDetail
                    {
                        Name = prefix + item["Name"].InnerText + postfix,
                        Points = new List<XYZ>()
                    };
                    foreach (XmlNode child in item["Points"].ChildNodes)
                    {
                        string[] strings = child.InnerText.Split(',');
                        double x = double.Parse(strings.First()) / 304.8;
                        double y = double.Parse(strings.Last()) / 304.8;
                        XYZ point = new XYZ(x, y, 0);
                        detail.Points.Add(point);
                    }
                    details.Add(detail);
                }

                // 通过过滤器获取所有的族
                FilteredElementCollector collector1 = new FilteredElementCollector(document);
                List<Family> families = collector1.OfClass(typeof(Family)).Cast<Family>().ToList();
                //创建结构柱族及载入族
                foreach (ColumnDetail detail in details)
                {
                    if (families.Exists(f => f.FamilyCategory.Name == "结构柱" && f.Name == detail.Name))
                    {
                        continue;
                    }

                    //新建公制结构柱族文档
                    string file = Information.GetFileInfo($"族文件\\{app.VersionNumber}\\公制结构柱.rft").FullName;
                    Document familyDocument = app.NewFamilyDocument(file);
                    using (Transaction transaction = new Transaction(familyDocument, "创建结构柱族" + detail.Name))
                    {
                        transaction.Start();

                        FamilyManager manager = familyDocument.FamilyManager;
                        manager.NewType("Lightning");

                        //设置用于模型行为的材质
                        //1 钢
                        //2 混凝土
                        //3 木材
                        //4 其他
                        //5 预制混凝土
                        foreach (Parameter item in familyDocument.OwnerFamily.Parameters)
                        {
                            if (item.Definition.Name == "用于模型行为的材质")
                            {
                                item.Set(2);
                            }
                        }

                        CurveArrArray curveArrArray = detail.GetCurve();
                        SketchPlane skplane = GetSketchPlane(familyDocument);

                        //创建拉伸
                        Extrusion extruction = familyDocument.FamilyCreate.NewExtrusion(true, curveArrArray, skplane, 4000 / 304.8);
                        //更新文档
                        familyDocument.Regenerate();

                        //锁定拉伸上下面到底标高及顶标高
                        Reference topFaceRef = null;
                        Reference bottomFaceRef = null;
                        Options opt = new Options
                        {
                            ComputeReferences = true,
                            DetailLevel = ViewDetailLevel.Fine
                        };
                        GeometryElement geometryElement = extruction.get_Geometry(opt);
                        foreach (GeometryObject geometry in geometryElement)
                        {
                            if (geometry is Solid)
                            {
                                Solid solid = geometry as Solid;
                                foreach (Face face in solid.Faces)
                                {
                                    if (face.ComputeNormal(new UV()).IsAlmostEqualTo(new XYZ(0, 0, 1)))
                                    {
                                        topFaceRef = face.Reference;
                                    }
                                    if (face.ComputeNormal(new UV()).IsAlmostEqualTo(new XYZ(0, 0, -1)))
                                    {
                                        bottomFaceRef = face.Reference;
                                    }
                                }
                            }
                        }
                        View view = GetView(familyDocument);
                        Reference reference1 = GetTopLevel(familyDocument);
                        Dimension dimension1 = familyDocument.FamilyCreate.NewAlignment(view, reference1, topFaceRef);
                        dimension1.IsLocked = true;
                        Reference reference2 = GetBottomLevel(familyDocument);
                        Dimension dimension2 = familyDocument.FamilyCreate.NewAlignment(view, reference2, bottomFaceRef);
                        dimension2.IsLocked = true;

                        familyDocument.Regenerate();

                        //创建材质
                        Parameter parameter = extruction.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
#if R19 || R20 || R21
                        FamilyParameter familyParameter = manager.AddParameter("材质", BuiltInParameterGroup.PG_MATERIALS, ParameterType.Material, false);
#else
                        FamilyParameter familyParameter = manager.AddParameter("材质", GroupTypeId.Materials, SpecTypeId.Reference.Material, false);
#endif
                        //关联材质参数
                        manager.AssociateElementParameterToFamilyParameter(parameter, familyParameter);

                        transaction.Commit();
                    }

                    string familyFile = PCInfo.MyDocumentsDirectoryInfo.FullName + "\\Lightning\\LightningRevit\\" + detail.Name + ".rfa";
                    familyDocument.SaveAs(familyFile, new SaveAsOptions() { OverwriteExistingFile = true });
                    Family family = familyDocument.LoadFamily(document);
                    familyDocument.Close(false);
                    File.Delete(familyFile);
                }

                LightningApp.ShowMsg("正在创建结构柱。。。", 0, true);
                //读取平面柱
                List<Column> columns = new List<Column>();
                foreach (XmlNode item in root["Columns"].ChildNodes)
                {
                    string[] strings = item["Point"].InnerText.Split(',');
                    double x = double.Parse(strings.First()) / 304.8;
                    double y = double.Parse(strings.Last()) / 304.8;
                    XYZ point = new XYZ(x, y, 0);
                    Column column = new Column
                    {
                        Name = prefix + item["Name"].InnerText + postfix,
                        Point = point,
                        IsMirror = bool.Parse(item["IsMirror"].InnerText),
                        Angle = int.Parse(item["Angle"].InnerText)
                    };
                    columns.Add(column);
                }

                //在哪个标高创建
                FilteredElementCollector collector2 = new FilteredElementCollector(document);
                Level level = collector2.OfClass(typeof(Level)).First(l => l.Name == levels.SelectedItem.ToString()) as Level;

                // 通过过滤器获取所有的族类型
                FilteredElementCollector collector3 = new FilteredElementCollector(document);
                var columnFamilySymbols = collector3.OfClass(typeof(FamilySymbol)).Where(f => (f as FamilySymbol).Family.FamilyCategory.Name == "结构柱").Cast<FamilySymbol>().ToList();

                //创建平面柱
                foreach (var item in columns)
                {
                    FamilyInstance column;
                    XYZ center = align + item.Point;
                    var familySymbol = columnFamilySymbols.FirstOrDefault(f => f.FamilyName == item.Name);
                    if (familySymbol == default(FamilySymbol))
                    {
                        continue;
                    }
                    using (Transaction transaction = new Transaction(document, "创建结构柱" + item.Name))
                    {
                        transaction.Start();
                        // 激活柱族类型
                        if (!familySymbol.IsActive)
                        {
                            familySymbol.Activate();
                        }

                        // 创建柱
                        column = document.Create.NewFamilyInstance(center, familySymbol, level, StructuralType.Column);
                        transaction.Commit();
                    }

                    if (!column.IsValidObject)
                    {
                        if (MessageBox.Show("是否继续创建剩余构件？", "构件重复", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        {
                            return;
                        }
                        continue;
                    }

                    ElementId elementId;
                    //镜像
                    if (item.IsMirror)
                    {
                        Plane plane = Plane.CreateByNormalAndOrigin(new XYZ(1, 0, 0), center);
                        elementId = MirrorElement(document, column.Id, plane).FirstOrDefault();
                    }
                    else
                    {
                        elementId = column.Id;
                    }
                    using (Transaction transaction = new Transaction(document, "旋转结构柱" + item.Name))
                    {
                        transaction.Start();
                        if (item.IsMirror)
                        {
                            document.Delete(column.Id);
                        }
                        if (elementId != default && document.GetElement(elementId).IsValidObject && item.Angle != 0)
                        {
                            //旋转柱
                            Line line = Line.CreateBound(center, new XYZ(center.X, center.Y, center.Z + 1));
                            ElementTransformUtils.RotateElement(document, elementId, line, item.Angle * Math.PI / 180);
                        }
                        transaction.Commit();
                        if (transaction.GetStatus() != TransactionStatus.Committed)
                        {
                            if (MessageBox.Show("是否继续创建剩余构件？", "构件重复", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                            {
                                using (Transaction tr = new Transaction(document, "删除取消构件"))
                                {
                                    tr.Start();
                                    document.Delete(column.Id);
                                    document.Delete(elementId);
                                    tr.Commit();
                                }
                                return;
                            }
                        }
                    }
                }

                LightningApp.ShowMsg("正在创建结构墙类型。。。", 0, true);
                //读取墙信息
                List<WallInfo> wallInfos = new List<WallInfo>();
                foreach (XmlNode item in root["WallInfos"].ChildNodes)
                {
                    WallInfo wallInfo = new WallInfo
                    {
                        Name = prefix + item["Name"].InnerText + postfix,
                        Width = int.Parse(item["Width"].InnerText)
                    };
                    wallInfos.Add(wallInfo);
                }

                //创建墙类型
                using (Transaction transaction = new Transaction(document, "创建墙类型"))
                {
                    transaction.Start();
                    foreach (var wallInfo in wallInfos)
                    {
                        CreateWallType(document, wallInfo.Name, wallInfo.Width);
                    }
                    transaction.Commit();
                }

                LightningApp.ShowMsg("正在创建结构墙。。。", 0, true);
                //读取平面墙
                List<Models.Wall> walls = new List<Models.Wall>();
                foreach (XmlNode item in root["Walls"].ChildNodes)
                {
                    string[] strings1 = item["Start"].InnerText.Split(',');
                    double x1 = double.Parse(strings1.First()) / 304.8;
                    double y1 = double.Parse(strings1.Last()) / 304.8;
                    XYZ start = new XYZ(x1, y1, 0);
                    string[] strings2 = item["End"].InnerText.Split(',');
                    double x2 = double.Parse(strings2.First()) / 304.8;
                    double y2 = double.Parse(strings2.Last()) / 304.8;
                    XYZ end = new XYZ(x2, y2, 0);
                    Models.Wall wall = new Models.Wall
                    {
                        Name = prefix + item["Name"].InnerText + postfix,
                        Start = start,
                        End = end
                    };
                    walls.Add(wall);
                }

                //创建平面墙
                FilteredElementCollector collector4 = new FilteredElementCollector(document);
                double dd = collector4.OfClass(typeof(Level)).Where(le => (le as Level).Elevation > level.Elevation).Min(l => (l as Level).Elevation - level.Elevation);

                using (Transaction transaction = new Transaction(document, "创建平面墙"))
                {
                    transaction.Start();
                    foreach (var item in walls)
                    {
                        Line line = Line.CreateBound(align + item.Start, align + item.End);
                        ElementId wallId = GetWallId(document, item.Name);
                        Wall wall = Wall.Create(document, line, wallId, level.Id, 3000 / 304.8, 0.0, false, true);
                        if (collector4.OfClass(typeof(Level)).ToList().Find(l => (l as Level).Elevation - level.Elevation == dd) is Level level2)
                        {
                            wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id); // 设置墙的顶部标高
                        }
                    }
                    LightningApp.ShowMsg("创建完成", 2);
                    transaction.Commit();
                }
            }
            catch (Exception exp)
            {
                LightningApp.ShowMsg("创建失败", 2);
                exp.Log(Information.ErrorLog);
            }
        }

        private static ElementId GetWallId(Document document, string name)
        {
            // 获取墙类型
            FilteredElementCollector collector = new FilteredElementCollector(document);
            var wllTypes = collector.OfClass(typeof(WallType)).ToList();
            Element element = wllTypes.Find(w => w.Name == name);
            return element != null ? element.Id : default;
        }

        public static ICollection<ElementId> MirrorElement(Document doc, ElementId elementId, Plane plane)
        {
            if (doc == null || plane == null || elementId == ElementId.InvalidElementId || !ElementTransformUtils.CanMirrorElement(doc, elementId))
                throw new ArgumentException("Argument invalid");

            ICollection<ElementId> result = new List<ElementId>();
            // create DocumentChanged event handler
            var documentChangedHandler = new EventHandler<DocumentChangedEventArgs>((sender, args) => result = args.GetAddedElementIds());

            // subscribe the event
            doc.Application.DocumentChanged += documentChangedHandler;
            using (Transaction transaction = new Transaction(doc, "镜像结构柱" + (doc.GetElement(elementId) as FamilyInstance).Symbol.FamilyName))
            {
                try
                {
                    transaction.Start();
                    ElementTransformUtils.MirrorElement(doc, elementId, plane);
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("ERROR", ex.ToString());
                    transaction.RollBack();
                }

                finally
                {
                    // unsubscribe the event
                    doc.Application.DocumentChanged -= documentChangedHandler;
                }
            }
            return result;
        }

        private static void CreateWallType(Document doc, string wallTypeName, int wallWidth)
        {
            // 获取墙类型
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var wllTypes = collector.OfClass(typeof(WallType)).ToList();
            Element element = wllTypes.Find(w => w.Name == wallTypeName);
            if (element != null)
            {
                return;
            }

            //查找一个基本墙体类型
            WallType baseWallType = null;
            foreach (WallType item in wllTypes.Cast<WallType>())
            {
                //遍历查找基本墙体作为基础墙体类型
                if (item.Category.Name == "墙" && item.FamilyName == "基本墙")
                {
                    baseWallType = item;
                    break;
                }
            }

            //创建新的墙体类型
            WallType newWallType = null;
            //复制成新的墙体类型
            newWallType = baseWallType.Duplicate(wallTypeName) as WallType;
            doc.Regenerate();
            //设置厚度
            CompoundStructure structure = newWallType.GetCompoundStructure();
            structure.SetLayerWidth(0, wallWidth / 304.8); //墙宽,换算成英寸
            newWallType.SetCompoundStructure(structure);  //修改后设置
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

        private static Reference GetTopLevel(Document doc)
        {
            FilteredElementCollector temc = new FilteredElementCollector(doc);
            temc.OfClass(typeof(Level));
            Level lvl = temc.First(m => m.Name == "高于参照标高") as Level;
            return new Reference(lvl);
        }

        private static Reference GetBottomLevel(Document doc)
        {
            FilteredElementCollector temc = new FilteredElementCollector(doc);
            temc.OfClass(typeof(Level));
            Level lvl = temc.First(m => m.Name == "低于参照标高") as Level;
            return new Reference(lvl);
        }

        private static View GetView(Document doc)
        {
            FilteredElementCollector viewFilter = new FilteredElementCollector(doc);
            viewFilter.OfClass(typeof(View));
            View v = viewFilter.First(m => m.Name == "前") as View;
            return v;
        }

        private static SketchPlane GetSketchPlane(Document doc)
        {
            FilteredElementCollector temc = new FilteredElementCollector(doc);
            temc.OfClass(typeof(SketchPlane));
            SketchPlane sketchPlane = temc.First(m => m.Name == "低于参照标高") as SketchPlane;
            return sketchPlane;
        }
    }
}