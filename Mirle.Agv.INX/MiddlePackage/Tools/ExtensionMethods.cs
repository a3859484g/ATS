using Newtonsoft.Json;

namespace Mirle.Agv.MiddlePackage.Umtc.Tools
{
    public static class ExtensionMethods
    {
        public static string GetJsonInfo(this object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented);
        }        
    }
}
