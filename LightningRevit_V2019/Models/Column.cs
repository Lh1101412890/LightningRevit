using Autodesk.Revit.DB;

namespace LightningRevit.Models
{
    public class Column
    {
        public string Name { get; set; }
        public XYZ Point { get; set; }
        public bool IsMirror { get; set; }
        public int Angle { get; set; }
    }
}