using Autodesk.Revit.DB;

namespace LightningRevit.LightningExtension
{
    public class FamilyLoadOptions : IFamilyLoadOptions
    {
        private readonly bool _overwrite;
        private readonly FamilySource _source;
        /// <summary>
        /// 加载族设置
        /// </summary>
        /// <param name="overwrite">是否覆盖原有参数</param>
        /// <param name="source">使用的共享族类型</param>
        public FamilyLoadOptions(bool overwrite, FamilySource source)
        {
            _overwrite = overwrite;
            _source = source;
        }
        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = _overwrite;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            overwriteParameterValues = _overwrite;
            source = _source;
            return true;
        }
    }
}