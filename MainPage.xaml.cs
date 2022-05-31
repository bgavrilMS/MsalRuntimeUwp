using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Identity.Client.NativeInterop;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Security.Authentication.Web;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MsalRuntimeUwp
{
    [ComImport, Guid("45D64A29-A63E-4CB6-B498-5781D298CB4F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ICoreWindowInterop
    {
        IntPtr WindowHandle { get; }
        bool MessageHandled { set; }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static readonly string s_clientID = "1d18b3b0-251b-4714-a02a-9956cec86c2d";
        private static readonly string s_authority = "https://login.microsoftonline.com/common/";
        private const string s_scopes = "user.read";

        private string _redirectUri = null;

        public MainPage()
        {
            this.InitializeComponent();


            // returns something like s-1-15-2-2601115387-131721061-1180486061-1362788748-631273777-3164314714-2766189824
            string sid = WebAuthenticationBroker.GetCurrentApplicationCallbackUri().Host;

            // use uppercase S
            sid = sid.Replace('s', 'S');

            // the redirect URI
            _redirectUri = $"ms-appx-web://microsoft.aad.brokerplugin/{sid}";
        }

        /// <summary>
        ///  UWP does not expose the concept of Window handle, as it's all handed internally. But you can still get it via COM.
        /// </summary>
        /// <returns></returns>
        private static IntPtr GetUwpWindowHandle()
        {
            dynamic corewin = Windows.UI.Core.CoreWindow.GetForCurrentThread();
            var interop = (ICoreWindowInterop)corewin;
            var handle = interop.WindowHandle;

            return handle;
        }

        string _lastAccountId = null;

        private async void Interactive_Click(object sender, RoutedEventArgs e)
        {
            using (var core = new Core())
            using (AuthParameters authParams = GetCommonAuthParameters())
            {
                using (var result = await core.SignInInteractivelyAsync(
                    GetUwpWindowHandle(),
                    authParams,
                    Guid.NewGuid().ToString("D"),
                    accountHint: "",
                    cancellationToken: default)
                    .ConfigureAwait(true)) // stay on UI thread for simplicity
                {
                    await LogResultAsync(result);
                }
            }
        }

        private async Task LogResultAsync(AuthResult result)
        {
            if (result.IsSuccess)
            {

                await DisplayMessageAsync($"[WamBroker] Got a token for {result.Account.Id}");
                _lastAccountId = result.Account.Id;
            }
            else
            {
                await DisplayMessageAsync($"[WamBroker] Could not login interactively. {result.Error}");
            }
        }

        private AuthParameters GetCommonAuthParameters()
        {
            var authParams = new AuthParameters(s_clientID, s_authority);
            authParams.RequestedScopes = s_scopes;
            authParams.RedirectUri = _redirectUri;

            // TODO: optionally add login hint

            return authParams;
        }

        private async void SilentDefaultWindowsAccount_Click(object sender, RoutedEventArgs e)
        {
            using (var core = new Core())
            using (var authParams = GetCommonAuthParameters())
            {
                using (AuthResult result = await core.SignInAsync(
                        GetUwpWindowHandle(),
                        authParams,
                        Guid.NewGuid().ToString("D"),
                        default))
                {
                    await LogResultAsync(result);
                }
            }
        }

        private object AcquireToken(string v)
        {
            throw new NotImplementedException();
        }

        private async Task DisplayMessageAsync(string message)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                   () =>
                   {
                       Log.Text = message;
                   });
        }
    }
}
