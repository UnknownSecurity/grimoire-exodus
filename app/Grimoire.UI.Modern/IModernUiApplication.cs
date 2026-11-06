using Newtonsoft.Json.Linq;

namespace Grimoire.UI.Modern
{
    internal interface IModernUiApplication
    {
        JObject GetBootstrap();

        JToken Invoke(string method, JObject parameters);
    }
}
