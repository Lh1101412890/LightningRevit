using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace LightningRevit.LightningExtension
{
    /// <summary>
    /// 选择结构柱、建筑柱、墙、梁、板
    /// </summary>
    public class ConnectionElementSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem.Category == null)
            {
                return false;
            }
            else
            {
                // 只允许选择柱、墙、梁、板
                return
#if R24 || R25 || R26
                   elem.Category.Id.Value == (long)BuiltInCategory.OST_StructuralColumns
                || elem.Category.Id.Value == (long)BuiltInCategory.OST_Columns
                || elem.Category.Id.Value == (long)BuiltInCategory.OST_Walls
                || elem.Category.Id.Value == (long)BuiltInCategory.OST_StructuralFraming
                || elem.Category.Id.Value == (long)BuiltInCategory.OST_Floors;
#else
                    elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralColumns
                 || elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Columns
                 || elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Walls
                 || elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_StructuralFraming
                 || elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors;
#endif
            }
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}