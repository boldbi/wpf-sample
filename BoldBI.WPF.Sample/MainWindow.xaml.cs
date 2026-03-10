using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace BoldBI.WPF.Sample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.WindowState = WindowState.Maximized;
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var embedDetails = GetEmbedConfig();
                if (embedDetails == null)
                {
                    MessageBox.Show("Unable to read embedConfig.json or missing values.", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var payload = new
                {
                    email = embedDetails.UserEmail,
                    serverurl = embedDetails.ServerUrl,
                    siteidentifier = embedDetails.SiteIdentifier,
                    embedsecret = embedDetails.EmbedSecret,
                    dashboard = new { id = embedDetails.DashboardId }
                };

                using (var client = new HttpClient())
                {
                    var requestUrl = string.IsNullOrWhiteSpace(payload.siteidentifier)
                        ? $"{payload.serverurl}/api/embed/authorize"
                        : $"{payload.serverurl}/api/{payload.siteidentifier}/embed/authorize";

                    var jsonPayload = JsonConvert.SerializeObject(payload);
                    using (var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                    {
                        var response = await client.PostAsync(requestUrl, httpContent).ConfigureAwait(false);
                        response.EnsureSuccessStatusCode();
                        var resultContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        await InitializeAsync(resultContent).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => MessageBox.Show($"Initialization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        async Task InitializeAsync(string resultContent)
        {
            var embed = GlobalAppSettings.EmbedDetails ?? new EmbedDetails();
            var serverBase = string.IsNullOrWhiteSpace(embed.SiteIdentifier)
                ? embed.ServerUrl?.TrimEnd('/')
                : $"{embed.ServerUrl?.TrimEnd('/')}/{embed.SiteIdentifier.Trim('/')}";

            var html = "<!DOCTYPE html><html><head><meta http-equiv='X-UA-Compatible' content='IE=Edge' />" +
                       "<script type='text/javascript' src='https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/jquery.min.js'></script>" +
                       "<script src='https://cdn.polyfill.io/v2/polyfill.min.js'></script>" +
                       "<script type='text/javascript' src='https://cdn.boldbi.com/embedded-sdk/latest/boldbi-embed.js'></script>" +
                       "<script type='text/javascript'>$(document).ready(function() { this.dashboard = BoldBI.create({ serverUrl:'" + serverBase + "', dashboardId:'" + embed.DashboardId + "',embedContainerId: 'dashboard',embedType:'" + embed.EmbedType + "',environment:'" + embed.Environment + "',authorizationServer:{url: '', data:" + resultContent + "}}); console.log(this.dashboard); this.dashboard.loadDashboard(); });</script>" +
                       "</head><body><div id='viewer-section'><div id='dashboard'></div></div></body></html>";

            var filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "MyWebView.html");
            File.WriteAllText(filePath, html, Encoding.UTF8);
            var uri = new Uri(filePath).AbsoluteUri;

            await Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    if (wbSample == null)
                    {
                        MessageBox.Show("WebView control 'wbSample' not found in XAML.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await wbSample.EnsureCoreWebView2Async();
                    wbSample.CoreWebView2.Navigate(uri);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"WebView initialization failed: {ex.Message}", "WebView Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        EmbedDetails GetEmbedConfig()
        {
            try
            {
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var jsonString = File.ReadAllText(System.IO.Path.Combine(basePath, "embedConfig.json"));
                var details = JsonConvert.DeserializeObject<EmbedDetails>(jsonString);
                GlobalAppSettings.EmbedDetails = details;
                return details;
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show($"Failed to read embedConfig.json: {ex.Message}", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error));
                return null;
            }
        }
}
}
