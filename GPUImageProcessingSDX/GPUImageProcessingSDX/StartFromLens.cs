using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace GPUImageProcessingSDX
{
    class StartFromLens : UriMapperBase
    {
        private string tempUri;

        public override Uri MapUri(Uri uri)
        {
            tempUri = uri.ToString();

            // Look for a URI from the lens picker.
            if (tempUri.Contains("ViewfinderLaunch"))
            {
                App.LaunchedFromLens = true;
                // Launch as a lens, launch viewfinder screen.
                return new Uri("/MainPage.xaml", UriKind.Relative);
            }

            // Otherwise perform normal launch.
            return uri;
        }
    }
}
