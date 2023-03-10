using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
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
            //this.RenderSize = Screen.PrimaryScreen.WorkingArea.Size;
            this.WindowState = WindowState.Maximized;
            //To set embed_server_timestamp to overcome the EmbedCodeValidation failing while different timezone using at client application.
            decimal time = (decimal)Math.Round((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalMilliseconds / 1000);
            var dashboardServerApiUrl = EmbedProperties.RootUrl + "api/" + EmbedProperties.SiteIdentifier;

            var embedQuerString = "embed_nonce=" + Guid.NewGuid() +
             "&embed_dashboard_id=" + EmbedProperties.DashboardId +
             "&embed_timestamp=" + Math.Round(time) +
             "&embed_expirationtime=100000";


            embedQuerString += "&embed_user_email=" + EmbedProperties.UserEmail;
            double timeStamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            embedQuerString += "&embed_server_timestamp=" + timeStamp;
            var embedDetailsUrl = "/embed/authorize?" + embedQuerString + "&embed_signature=" + GetSignatureUrl(embedQuerString);

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(dashboardServerApiUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                var result = client.GetAsync(dashboardServerApiUrl + embedDetailsUrl).Result;
                string resultContent = result.Content.ReadAsStringAsync().Result;
                //wbSample.ObjectForScripting = this;
                string path = System.IO.Path.GetFullPath("Embedwrapper.js");
                InitializeAsync(resultContent);
                }
        }

        async void InitializeAsync(string resultContent)
        {
            var html = @"
            <!DOCTYPE html>
<html><head><meta http-equiv='X-UA-Compatible' content='IE=Edge' />
<script type='text/javascript' src='https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.0/jquery.min.js'>
</script>
<script src='https://cdn.polyfill.io/v2/polyfill.min.js'></script>
<script type='text/javascript' src='https://cdn.boldbi.com/embedded-sdk/v6.1.8/boldbi-embed.js'></script>" +
"<script type='text/javascript'>$(document).ready(function() " +
"{this.dashboard = BoldBI.create({ serverUrl:'" + EmbedProperties.RootUrl + EmbedProperties.SiteIdentifier + "', dashboardId:'" + EmbedProperties.DashboardId + "',embedContainerId: 'dashboard',embedType:'" + EmbedProperties.EmbedType + "',environment:'" + EmbedProperties.Environment + "',width: window.innerWidth - 230 + 'px',height: window.innerHeight - 200 + 'px',expirationTime: 100000,authorizationServer:{url: '', data:" + resultContent + "},dashboardSettings:{showExport: false,showRefresh: false,showMoreOption: false}});console.log(this.dashboard);this.dashboard.loadDashboard();});</script>" +
"</head>" +
"<body><div id ='viewer-section'>" +
"<div id ='dashboard'></div></div>" +
"</body>" +
"</html>";
          
            await wbSample.EnsureCoreWebView2Async(null);
            if (wbSample != null && wbSample.CoreWebView2 != null)
            {
                File.WriteAllText(System.IO.Path.Combine(Environment.CurrentDirectory, @"MyWebView.html"), html);
                wbSample.CoreWebView2.Navigate(System.IO.Path.Combine(Environment.CurrentDirectory, @"MyWebView.html"));
            }
        }
        public string GetSignatureUrl(string message)
        {
            var encoding = new System.Text.UTF8Encoding();
            var keyBytes = encoding.GetBytes(EmbedProperties.EmbedSecret);
            var messageBytes = encoding.GetBytes(message);
            using (var hmacsha1 = new HMACSHA256(keyBytes))
            {
                var hashMessage = hmacsha1.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashMessage);
            }
        }
    }
}
