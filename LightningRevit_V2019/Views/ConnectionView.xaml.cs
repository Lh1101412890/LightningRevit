using System;
using System.Collections.Generic;
using System.Linq;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using LightningRevit.LightningExtension;

using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Controls.CheckBox;

namespace LightningRevit.Views
{
    /// <summary>
    /// ConnectionView.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectionView : ViewBase
    {
        private readonly ExternalEventHandler handler;
        private int fun;
        private double total;
        private double n;
        private double percent;
        /// <summary>
        /// 边界框允许误差
        /// </summary>
        private readonly double tolerance = 1.01;
        private bool selecting;
        public ConnectionView(UIApplication uIApplication) : base(uIApplication)
        {
            InitializeComponent();
            handler = new ExternalEventHandler(Connection, "图元连接设置");
            fun = 0;
            total = 0;
            n = 0;
            percent = 0;
            selecting = false;
        }

        private void Connection_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (selecting)
            {
                return;
            }
            Button button = sender as Button;
            if (button.Content.ToString() == "项目")
            {
                fun = 1;
            }
            handler.Raise();
        }

        private void Connection(UIApplication uIApplication)
        {
            UIDocument uIDocument = uIApplication.ActiveUIDocument;
            Document doc = uIDocument.Document;
            LightningApp.ShowMsg("正在设置连接，已完成 0 %", 0, true);

            FilteredElementCollector collector1;
            FilteredElementCollector collector2;
            FilteredElementCollector collector3;
            FilteredElementCollector collector4;
            FilteredElementCollector collector5;
            FilteredElementCollector collector6;
            FilteredElementCollector collector7;
            FilteredElementCollector collector8;
            // 项目
            if (fun == 1)
            {
                collector1 = new FilteredElementCollector(doc);
                collector2 = new FilteredElementCollector(doc);
                collector3 = new FilteredElementCollector(doc);
                collector4 = new FilteredElementCollector(doc);

                collector5 = new FilteredElementCollector(doc);
                collector6 = new FilteredElementCollector(doc);
                collector7 = new FilteredElementCollector(doc);
                collector8 = new FilteredElementCollector(doc);
            }
            // 选择
            else
            {
                IList<Reference> references;
                try
                {
                    LightningApp.ShowMsg("请选择要设置连接的图元", 0, true);
                    selecting = true;
                    references = uIDocument.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new ConnectionElementSelectionFilter(), "请选择要设置连接的图元");
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    LightningApp.ShowMsg("", 0);
                    selecting = false;
                    return;
                }
                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                {
                    LightningApp.ShowMsg("视图未激活，请移动当前视图后重试", 2);
                    selecting = false;
                    return;
                }
                selecting = false;
                List<ElementId> elementIds = new List<ElementId>();
                foreach (var item in references)
                {
                    elementIds.Add(doc.GetElement(item).Id);
                }

                collector1 = new FilteredElementCollector(doc, elementIds);
                collector2 = new FilteredElementCollector(doc, elementIds);
                collector3 = new FilteredElementCollector(doc, elementIds);
                collector4 = new FilteredElementCollector(doc, elementIds);

                collector5 = new FilteredElementCollector(doc, elementIds);
                collector6 = new FilteredElementCollector(doc, elementIds);
                collector7 = new FilteredElementCollector(doc, elementIds);
                collector8 = new FilteredElementCollector(doc, elementIds);
            }
            // 结构
            List<Element> list_columnS = collector1.OfCategory(BuiltInCategory.OST_StructuralColumns).OfClass(typeof(FamilyInstance)).ToElements().ToList();
            List<Element> list_wallS = collector2.OfCategory(BuiltInCategory.OST_Walls).OfClass(typeof(Wall)).Where(w => IsStructural(w)).ToList();
            List<Element> list_beamS = collector3.OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance)).Where(b => IsStructural(b)).ToList(); ;
            List<Element> list_floorS = collector4.OfCategory(BuiltInCategory.OST_Floors).OfClass(typeof(Floor)).Where(f => IsStructural(f)).ToList();
            //建筑
            List<Element> list_columnA = collector5.OfCategory(BuiltInCategory.OST_Columns).OfClass(typeof(FamilyInstance)).ToList();
            List<Element> list_beamA = collector6.OfCategory(BuiltInCategory.OST_StructuralFraming).OfClass(typeof(FamilyInstance)).Where(b => !IsStructural(b)).ToList();
            List<Element> list_wallA = collector7.OfCategory(BuiltInCategory.OST_Walls).OfClass(typeof(Wall)).Where(w => !IsStructural(w)).ToList();
            List<Element> list_floorA = collector8.OfCategory(BuiltInCategory.OST_Floors).OfClass(typeof(Floor)).Where(f => !IsStructural(f)).ToList();

            //统计图元个数（要扣减其他图元的图元数量）
            if (check.IsChecked == true)
            {
                total += list_columnS.Count + list_wallS.Count + list_beamS.Count + list_floorS.Count;
            }
            else
            {
                total += Group1Check() ? list_columnS.Count : 0;
                total += Group2Check() ? list_wallS.Count : 0;
                total += Group3Check() ? list_beamS.Count : 0;
                total += Group4Check() ? list_floorS.Count : 0;
            }
            total += Group5Check() ? list_columnA.Count : 0;
            total += Group6Check() ? list_beamA.Count : 0;
            total += Group7Check() ? list_wallA.Count : 0;
            total += Group8Check() ? list_floorA.Count : 0;

            using (Transaction transaction = new Transaction(doc, "图元连接设置"))
            {
                transaction.Start();
                if (check.IsChecked == true)
                {
                    foreach (Element column in list_columnS)
                    {
                        List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, tolerance);
                        foreach (Element ele in column_box_eles)
                        {

                            string name = ele.Category.Name;
                            if (name == "建筑柱"
                                || (name == "墙" && (check1.IsChecked == true || !IsStructural(ele)))
                                || (name == "结构框架" && (check2.IsChecked == true || !IsStructural(ele)))
                                || (name == "楼板" && (check3.IsChecked == true || !IsStructural(ele))))
                            {
                                JudgeConnection(doc, column, ele);
                            }
                        }
                        ShowProgress();
                    }
                    foreach (Element wall in list_wallS)
                    {
                        List<Element> column_box_eles = Get_Boundingbox_eles(doc, wall, tolerance);
                        foreach (Element ele in column_box_eles)
                        {
                            string name = ele.Category.Name;
                            if (name == "建筑柱"
                                || (name == "结构柱" && check4.IsChecked == true)
                                || (name == "墙" && !IsStructural(ele))
                                || (name == "结构框架" && (check5.IsChecked == true || !IsStructural(ele)))
                                || (name == "楼板" && (check6.IsChecked == true || !IsStructural(ele))))
                            {
                                JudgeConnection(doc, wall, ele);
                            }
                        }
                        ShowProgress();
                    }
                    foreach (Element beam in list_beamS)
                    {
                        List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, tolerance);
                        foreach (Element ele in beam_box_eles)
                        {
                            string name = ele.Category.Name;
                            if (name == "建筑柱"
                                || (name == "结构柱" && check7.IsChecked == true)
                                || (name == "墙" && (check8.IsChecked == true || !IsStructural(ele)))
                                || (name == "结构框架" && !IsStructural(ele))
                                || (name == "楼板" && (check9.IsChecked == true || !IsStructural(ele))))
                            {
                                JudgeConnection(doc, beam, ele);
                            }
                        }
                        ShowProgress();
                    }
                    foreach (Element floor in list_floorS)
                    {
                        List<Element> beam_box_eles = Get_Boundingbox_eles(doc, floor, tolerance);
                        foreach (Element ele in beam_box_eles)
                        {
                            string name = ele.Category.Name;
                            if (name == "建筑柱"
                                || (name == "结构柱" && check10.IsChecked == true)
                                || (name == "墙" && (check11.IsChecked == true || !IsStructural(ele)))
                                || (name == "结构框架" && (check12.IsChecked == true || !IsStructural(ele)))
                                || (name == "楼板" && !IsStructural(ele))
)
                            {
                                JudgeConnection(doc, floor, ele);
                            }
                        }
                        ShowProgress();
                    }
                }
                else
                {
                    if (Group1Check())
                    {
                        foreach (Element column in list_columnS)
                        {
                            List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, tolerance);
                            foreach (Element ele in column_box_eles)
                            {
                                string name = ele.Category.Name;
                                if (name == "墙" && check1.IsChecked == true && IsStructural(ele)
                                    || (name == "结构框架" && check2.IsChecked == true && IsStructural(ele))
                                    || (name == "楼板" && check3.IsChecked == true && IsStructural(ele)))
                                {
                                    JudgeConnection(doc, column, ele);
                                }
                            }
                            ShowProgress();
                        }
                    }
                    if (Group2Check())
                    {
                        foreach (Element wall in list_wallS)
                        {
                            List<Element> column_box_eles = Get_Boundingbox_eles(doc, wall, tolerance);
                            foreach (Element ele in column_box_eles)
                            {
                                string name = ele.Category.Name;
                                if ((name == "结构柱" && check4.IsChecked == true)
                                    || (name == "结构框架" && check5.IsChecked == true && IsStructural(ele))
                                    || (name == "楼板" && check6.IsChecked == true && IsStructural(ele)))
                                {
                                    JudgeConnection(doc, wall, ele);
                                }
                            }
                            ShowProgress();
                        }
                    }
                    if (Group3Check())
                    {
                        foreach (Element beam in list_beamS)
                        {
                            List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, tolerance);
                            foreach (Element ele in beam_box_eles)
                            {
                                string name = ele.Category.Name;
                                if ((name == "结构柱" && check7.IsChecked == true)
                                    || (name == "墙" && check8.IsChecked == true && IsStructural(ele))
                                    || (name == "楼板" && check9.IsChecked == true && IsStructural(ele)))
                                {
                                    JudgeConnection(doc, beam, ele);
                                }
                            }
                            ShowProgress();
                        }
                    }
                    if (Group4Check())
                    {
                        foreach (Element beam in list_floorS)
                        {
                            List<Element> beam_box_eles = Get_Boundingbox_eles(doc, beam, tolerance);
                            foreach (Element ele in beam_box_eles)
                            {
                                string name = ele.Category.Name;
                                if ((name == "结构柱" && check10.IsChecked == true)
                                    || (name == "墙" && check11.IsChecked == true && IsStructural(ele))
                                    || (name == "结构框架" && check12.IsChecked == true && IsStructural(ele)))
                                {
                                    JudgeConnection(doc, beam, ele);
                                }
                            }
                            ShowProgress();
                        }
                    }
                }
                if (Group5Check())
                {
                    foreach (Element column in list_columnA)
                    {
                        List<Element> column_box_eles = Get_Boundingbox_eles(doc, column, tolerance);
                        foreach (Element ele in column_box_eles)
                        {
                            string name = ele.Category.Name;
                            if ((name == "结构框架" && check13.IsChecked == true && !IsStructural(ele))
                                || (name == "墙" && check14.IsChecked == true && !IsStructural(ele))
                                || (name == "楼板" && check15.IsChecked == true && !IsStructural(ele)))
                            {
                                JudgeConnection(doc, column, ele);
                            }
                        }
                        ShowProgress();
                    }
                }
                if (Group6Check())
                {
                    foreach (Element beam in list_beamA)
                    {
                        List<Element> column_box_eles = Get_Boundingbox_eles(doc, beam, tolerance);
                        foreach (Element ele in column_box_eles)
                        {
                            string name = ele.Category.Name;
                            if ((name == "建筑柱" && check16.IsChecked == true)
                                || (name == "墙" && check17.IsChecked == true && !IsStructural(ele))
                                || (name == "楼板" && check18.IsChecked == true && !IsStructural(ele)))
                            {
                                JudgeConnection(doc, beam, ele);
                            }
                        }
                        ShowProgress();
                    }
                }
                if (Group7Check())
                {
                    foreach (Element wall in list_wallA)
                    {
                        List<Element> beam_box_eles = Get_Boundingbox_eles(doc, wall, tolerance);
                        foreach (Element ele in beam_box_eles)
                        {
                            string name = ele.Category.Name;
                            if ((name == "建筑柱" && check19.IsChecked == true)
                                || (name == "结构框架" && check20.IsChecked == true && !IsStructural(ele))
                                || (name == "楼板" && check21.IsChecked == true && !IsStructural(ele)))
                            {
                                JudgeConnection(doc, wall, ele);
                            }
                        }
                        ShowProgress();
                    }
                }
                if (Group8Check())
                {
                    foreach (Element floor in list_floorA)
                    {
                        List<Element> beam_box_eles = Get_Boundingbox_eles(doc, floor, tolerance);
                        foreach (Element ele in beam_box_eles)
                        {
                            string name = ele.Category.Name;
                            if ((name == "建筑柱" && check22.IsChecked == true)
                                || (name == "结构框架" && check23.IsChecked == true && !IsStructural(ele))
                                || (name == "墙" && check24.IsChecked == true && !IsStructural(ele)))
                            {
                                JudgeConnection(doc, floor, ele);
                            }
                        }
                        ShowProgress();
                    }
                }
                LightningApp.ShowMsg("连接设置完成", 2);
                Close();
                transaction.Commit();
            }
        }

        /// <summary>
        /// 判断墙、梁、楼板是否为结构
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static bool IsStructural(Element element)
        {
#if R24
            long value = element.Category.Id.Value;
            switch (value)
            {
                case (long)BuiltInCategory.OST_Walls:
                    return element.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).AsValueString() == "是";
                case (long)BuiltInCategory.OST_StructuralFraming:
                    return element.get_Parameter(BuiltInParameter.INSTANCE_STRUCT_USAGE_PARAM).AsValueString() != "其他";
                case (long)BuiltInCategory.OST_Floors:
                    return element.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).AsValueString() == "是";
                default:
                    return false;
            }
#elif R25 || R26
            long value = element.Category.Id.Value;
            return value switch
            {
                (long)BuiltInCategory.OST_Walls => element.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).AsValueString() == "是",
                (long)BuiltInCategory.OST_StructuralFraming => element.get_Parameter(BuiltInParameter.INSTANCE_STRUCT_USAGE_PARAM).AsValueString() != "其他",
                (long)BuiltInCategory.OST_Floors => element.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).AsValueString() == "是",
                _ => false,
            };
#else
            int value = element.Category.Id.IntegerValue;
            switch (value)
            {
                case (int)BuiltInCategory.OST_Walls:
                    return element.get_Parameter(BuiltInParameter.WALL_STRUCTURAL_SIGNIFICANT).AsValueString() == "是";
                case (int)BuiltInCategory.OST_StructuralFraming:
                    return element.get_Parameter(BuiltInParameter.INSTANCE_STRUCT_USAGE_PARAM).AsValueString() != "其他";
                case (int)BuiltInCategory.OST_Floors:
                    return element.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL).AsValueString() == "是";
                default:
                    return false;
            }
#endif
        }

        private void ShowProgress()
        {
            n++;
            double temp = Math.Round(n / total * 100);
            if (percent != temp)
            {
                percent = temp;
                LightningApp.ShowMsg("正在设置连接，已完成 " + percent + " %", 0, true);
            }
        }

        private static List<Element> Get_Boundingbox_eles(Document doc, Element element, double tolerance)
        {
            //获取元素boundingbox相交的元素
            List<Element> eles_list = new List<Element>();
            XYZ element_max_boundingBox = element.get_BoundingBox(doc.ActiveView).Max;
            XYZ element_min_boundingBox = element.get_BoundingBox(doc.ActiveView).Min;
            Outline element_Outline = new Outline(element_min_boundingBox, element_max_boundingBox);
            //element_Outline.Scale(offset);
            FilteredElementCollector element_collector = new FilteredElementCollector(doc);

            ElementClassFilter elementClassFilter_beamorcolumn = new ElementClassFilter(typeof(FamilyInstance));
            ElementClassFilter elementClassFilter_floor = new ElementClassFilter(typeof(Floor));
            ElementClassFilter elementClassFilter_wall = new ElementClassFilter(typeof(Wall));
            List<ElementFilter> elementClassFilters = new List<ElementFilter>
            {
                elementClassFilter_beamorcolumn,
                elementClassFilter_floor,
                elementClassFilter_wall
            };

            LogicalOrFilter logicalOr = new LogicalOrFilter(elementClassFilters);
            element_collector.WherePasses(logicalOr).WherePasses(new BoundingBoxIntersectsFilter(element_Outline, tolerance));
            foreach (Element near_ele in element_collector)
            {
                eles_list.Add(near_ele);
            }
            return eles_list;
        }

        private static void JudgeConnection(Document doc, Element ele1st, Element ele2st)
        {
            bool ifJoined = JoinGeometryUtils.AreElementsJoined(doc, ele1st, ele2st);
            if (ifJoined)
            {
                //判断连接关系是否正确，若不正确切换连接关系
                bool if1stCut2st = JoinGeometryUtils.IsCuttingElementInJoin(doc, ele1st, ele2st);
                if (if1stCut2st != true)
                {
                    try
                    {
                        JoinGeometryUtils.SwitchJoinOrder(doc, ele2st, ele1st);
                    }
                    catch
                    {
                        //跳过----小帅帅呆了
                    }
                }
            }
            else
            {
                try
                {
                    //尝试连接几何
                    JoinGeometryUtils.JoinGeometry(doc, ele1st, ele2st);
                }
                catch
                {

                }
            }
        }

        private void CheckBoxS_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }
            CheckBox checkBox = sender as CheckBox;
            switch (checkBox.Content)
            {
                case "结构柱切结构墙":
                    check4.IsChecked = false;
                    break;
                case "结构柱切结构梁":
                    check7.IsChecked = false;
                    break;
                case "结构柱切结构板":
                    check10.IsChecked = false;
                    break;
                case "结构墙切结构柱":
                    check1.IsChecked = false;
                    break;
                case "结构墙切结构梁":
                    check8.IsChecked = false;
                    break;
                case "结构墙切结构板":
                    check11.IsChecked = false;
                    break;
                case "结构梁切结构柱":
                    check2.IsChecked = false;
                    break;
                case "结构梁切结构墙":
                    check5.IsChecked = false;
                    break;
                case "结构梁切结构板":
                    check12.IsChecked = false;
                    break;
                case "结构板切结构柱":
                    check3.IsChecked = false;
                    break;
                case "结构板切结构墙":
                    check6.IsChecked = false;
                    break;
                case "结构板切结构梁":
                    check9.IsChecked = false;
                    break;
                default:
                    break;
            }
        }

        private void CheckBoxA_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }
            CheckBox checkBox = sender as CheckBox;
            switch (checkBox.Content)
            {
                case "建筑柱切建筑梁":
                    check16.IsChecked = false;
                    break;
                case "建筑柱切建筑墙":
                    check19.IsChecked = false;
                    break;
                case "建筑柱切建筑板":
                    check22.IsChecked = false;
                    break;
                case "建筑梁切建筑柱":
                    check13.IsChecked = false;
                    break;
                case "建筑梁切建筑墙":
                    check20.IsChecked = false;
                    break;
                case "建筑梁切建筑板":
                    check23.IsChecked = false;
                    break;
                case "建筑墙切建筑柱":
                    check14.IsChecked = false;
                    break;
                case "建筑墙切建筑梁":
                    check17.IsChecked = false;
                    break;
                case "建筑墙切建筑板":
                    check24.IsChecked = false;
                    break;
                case "建筑板切建筑柱":
                    check15.IsChecked = false;
                    break;
                case "建筑板切建筑梁":
                    check18.IsChecked = false;
                    break;
                case "建筑板切建筑墙":
                    check21.IsChecked = false;
                    break;
                default:
                    break;
            }

        }

        /// <summary>
        /// 结构柱组
        /// </summary>
        /// <returns></returns>
        private bool Group1Check()
        {
            return check1.IsChecked == true || check2.IsChecked == true || check3.IsChecked == true;
        }
        /// <summary>
        /// 结构墙组
        /// </summary>
        /// <returns></returns>
        private bool Group2Check()
        {
            return check4.IsChecked == true || check5.IsChecked == true || check6.IsChecked == true;
        }
        /// <summary>
        /// 结构梁组
        /// </summary>
        /// <returns></returns>
        private bool Group3Check()
        {
            return check7.IsChecked == true || check8.IsChecked == true || check9.IsChecked == true;
        }
        /// <summary>
        /// 结构板组
        /// </summary>
        /// <returns></returns>
        private bool Group4Check()
        {
            return check10.IsChecked == true || check11.IsChecked == true || check12.IsChecked == true;
        }
        /// <summary>
        /// 建筑柱组
        /// </summary>
        /// <returns></returns>
        private bool Group5Check()
        {
            return check13.IsChecked == true || check14.IsChecked == true || check15.IsChecked == true;
        }
        /// <summary>
        /// 建筑梁组
        /// </summary>
        /// <returns></returns>
        private bool Group6Check()
        {
            return check16.IsChecked == true || check17.IsChecked == true || check18.IsChecked == true;
        }
        /// <summary>
        /// 建筑墙组
        /// </summary>
        /// <returns></returns>
        private bool Group7Check()
        {
            return check19.IsChecked == true || check20.IsChecked == true || check21.IsChecked == true;
        }
        /// <summary>
        /// 建筑板组
        /// </summary>
        /// <returns></returns>
        private bool Group8Check()
        {
            return check22.IsChecked == true || check23.IsChecked == true || check24.IsChecked == true;
        }
    }
}