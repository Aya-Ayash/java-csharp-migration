using System.Windows;

namespace aim.legacy
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // DB auto-initializes SQLite (orderentry.db) with schema and seed data on first use
        }
    }
}
