using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Plugins;

namespace Emby.HueControl
{
    public class Plugin : MediaBrowser.Common.Plugins.BasePlugin<Configuration.PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name
        {
            get
            {
                return "HueControl";
            }
        }

        public override string Description
        {
            get
            {
                return "HueControl for Emby";
            }
        }

        public static Plugin Instance { get; private set; }

        private Guid _id = new Guid("C99BAA29-5BE5-4A4E-94B3-13248F052B0F"); 
        public override Guid Id
        {
            get { return _id; }
        }


        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
                }

            };
        }
    }
}