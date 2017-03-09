using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Charts.Startup))]
namespace Charts
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
