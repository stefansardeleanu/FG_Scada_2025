namespace FG_Scada_2025
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            // FIXED: Removed duplicate CountyPage registration
            Routing.RegisterRoute("CountyPage", typeof(Views.CountyPage));
            Routing.RegisterRoute("SitePage", typeof(Views.SitePage));
            Routing.RegisterRoute("SensorsPage", typeof(Views.SensorsPage));
            Routing.RegisterRoute("AlarmHistoryPage", typeof(Views.AlarmHistoryPage));
            Routing.RegisterRoute("ConnectionTestPage", typeof(Views.ConnectionTestPage));
        }
    }
}