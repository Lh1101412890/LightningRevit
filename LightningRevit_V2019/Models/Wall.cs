using Autodesk.Revit.DB;

namespace LightningRevit.Models
{
    public class Wall
    {
        public string Name { get; set; }
        public XYZ Start { get; set; }
        public XYZ End { get; set; }
    }
}
