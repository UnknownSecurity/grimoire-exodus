using Newtonsoft.Json.Linq;
using System;

namespace Grimoire.UI.Modern
{
    internal sealed class RootModernUiApplication : IModernUiApplication
    {
        private readonly Root root;

        public RootModernUiApplication(Root root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
        }

        public JObject GetBootstrap()
        {
            return root.BuildModernUiBootstrap();
        }

        public JToken Invoke(string method, JObject parameters)
        {
            return root.InvokeModernUiAction(method, parameters ?? new JObject());
        }
    }
}
