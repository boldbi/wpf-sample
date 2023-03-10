namespace BoldBI.WPF.Sample
{
    public class EmbedProperties
    {
        // Enter the server bi url
        public static string RootUrl = "http://localhost:5000/bi/";

        // Enter your server identifier 
        public static string SiteIdentifier = "site/site1";

        //Your Bold BI application environment. (If Cloud, you should use cloud, if  Enterprise, you should use onpremise)
        public static string Environment = "onpremise"; 
        public static string EmbedType = "component";

        // Enter the required dashboard id 
        public static string DashboardId = "";

        //UserEmail of the Admin in your Bold BI, which will be used to get the dashboards list  
        public static string UserEmail = "User email here";  

        // Get the embedSecret key from Bold BI.
        public static string EmbedSecret = "Embed secret key here";

    }
}
