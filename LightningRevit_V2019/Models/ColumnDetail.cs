using System.Collections.Generic;

using Autodesk.Revit.DB;

namespace LightningRevit.Models
{
    public class ColumnDetail
    {
        public string Name { get; set; }
        public List<XYZ> Points { get; set; }
        public CurveArrArray GetCurve()
        {
            if (Points.Count < 4)
            {
                return null;
            }
            List<Line> lines = new List<Line>();
            for (int i = 0; i < Points.Count; i++)
            {
                XYZ start = Points[i];
                XYZ end = i == Points.Count - 1 ? Points[0] : Points[i + 1];
                Line line = Line.CreateBound(start, end);
                lines.Add(line);
            }
            CurveArrArray curveArrArray = new CurveArrArray();
            CurveArray curveArray = new CurveArray();
            foreach (var item in lines)
            {
                curveArray.Append(item);
            }
            curveArrArray.Append(curveArray);
            return curveArrArray;
        }
    }
}