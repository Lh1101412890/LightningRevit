using System;

using Autodesk.Revit.DB;

namespace LightningRevit.Extension
{
    public static class XYZExtension
    {
        public static double GetDistanceTo(this XYZ point1, XYZ point2)
        {
            return Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
        }

        /// <summary>
        /// 获取两点之间的斜率（二维XY平面）
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public static double GetK2dTo(this XYZ point1, XYZ point2)
        {
            return point1.X == point2.X
                ? point1.Y <= point2.Y ? double.PositiveInfinity : double.NegativeInfinity
                : (point2.Y - point1.Y) / (point2.X - point1.X);
        }
    }
}