using Autodesk.Revit.DB;

namespace LightningRevit.Extension
{
    public static class ImportInstanceExtension
    {
        /// <summary>
        /// 获取导入实例的文件路径
        /// </summary>
        /// <param name="importInstance"></param>
        /// <returns></returns>
        public static string GetFilePath(this ImportInstance importInstance)
        {
            Document document = importInstance.Document;
            var type = document.GetElement(importInstance.GetTypeId());
            string filePath = ModelPathUtils.ConvertModelPathToUserVisiblePath(type.GetExternalFileReference().GetAbsolutePath());
            return filePath;
        }
    }
}
