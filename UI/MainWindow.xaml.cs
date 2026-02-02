using System.Windows;
using System.Windows.Controls;

namespace aim.legacy.UI
{
    /// <summary>
    /// Main application window and entry point.
    /// Provides navigation between customer and order management screens.
    /// Uses ContentControl for switching between different views (CardLayout equivalent).
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly CustomersScreen _customersScreen;
        private readonly OrdersScreen _ordersScreen;

        public MainWindow()
        {
            // Initialize database connection on startup
            // This ensures the database is ready before any screens load
            aim.legacy.db.DB.GetConn();

            InitializeComponent();

            _customersScreen = new CustomersScreen();
            _ordersScreen = new OrdersScreen();

            ShowCustomersScreen();
        }

        private void CustomersItem_Click(object sender, RoutedEventArgs e)
        {
            ShowCustomersScreen();
        }

        private void OrdersItem_Click(object sender, RoutedEventArgs e)
        {
            ShowOrdersScreen();
        }

        private void ExitItem_Click(object sender, RoutedEventArgs e)
        {
            aim.legacy.db.DB.CloseConn();
            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            aim.legacy.db.DB.CloseConn();
        }

        /// <summary>
        /// Switch to customers screen and refresh the data.
        /// Uses ContentControl to swap views without creating new instances.
        /// </summary>
        public void ShowCustomersScreen()
        {
            _customersScreen.Refresh();
            MainContent.Content = _customersScreen;
        }

        /// <summary>
        /// Switch to orders screen and refresh the data.
        /// Orders screen shows all customer orders with totals.
        /// </summary>
        public void ShowOrdersScreen()
        {
            _ordersScreen.Refresh();
            MainContent.Content = _ordersScreen;
        }
    }
}
