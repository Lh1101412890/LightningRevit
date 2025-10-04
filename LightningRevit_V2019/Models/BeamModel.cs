using Autodesk.Revit.DB;

namespace LightningRevit.Models
{
    /// <summary>
    /// 单跨梁
    /// </summary>
    public class BeamModel
    {
        public XYZ Start { get; set; }
        public XYZ End { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
