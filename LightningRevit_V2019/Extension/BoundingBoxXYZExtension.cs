using Autodesk.Revit.DB;

namespace LightningRevit.Extension
{
    public static class BoundingBoxXYZExtension
    {
        public static XYZ GetCenter(this BoundingBoxXYZ boundingBoxXYZ)
        {
            XYZ max = boundingBoxXYZ.Max;
            XYZ min = boundingBoxXYZ.Min;
            return new XYZ((max.X + min.X) / 2, (max.Y + min.Y) / 2, (max.Z + min.Z) / 2);
        }
    }
}
