using System.Windows;

namespace aim.legacy.UI
{
    /// <summary>
    /// Dialog for adding or editing a customer record.
    /// </summary>
    public partial class CustomerDialog : Window
    {
        private bool _saved;

        public long CustomerId { get; }
        public string CustomerName => NameTextBox.Text.Trim();
        public string Email => EmailTextBox.Text.Trim();
        public string Phone => PhoneTextBox.Text.Trim();
        public string Address => AddressTextBox.Text.Trim();
        public string CustomerType => (CustomerTypeComboBox.SelectedItem as string) ?? "STANDARD";

        public bool IsSaved => _saved;

        public CustomerDialog(Window owner, long id, string name, string email, string phone, string address, string customerType)
        {
            Owner = owner;
            CustomerId = id;

            InitializeComponent();

            Title = id == 0 ? "Add Customer" : "Edit Customer";

            CustomerTypeComboBox.ItemsSource = new[] { "STANDARD", "PREMIUM", "VIP" };
            CustomerTypeComboBox.SelectedItem = customerType ?? "STANDARD";

            if (id > 0)
            {
                NameTextBox.Text = name ?? "";
                EmailTextBox.Text = email ?? "";
                PhoneTextBox.Text = phone ?? "";
                AddressTextBox.Text = address ?? "";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Name is required", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _saved = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _saved = false;
            DialogResult = false;
            Close();
        }
    }
}
