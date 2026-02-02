using System.Windows;

namespace aim.legacy.UI
{
    public class AddLineResult
    {
        public long ProdId { get; set; }
        public string ProdName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public partial class AddLineDialog : Window
    {
        public AddLineResult Result { get; private set; }

        public AddLineDialog(Window owner, System.Collections.Generic.List<(long id, string name, decimal price)> products)
        {
            Owner = owner;
            InitializeComponent();

            foreach (var p in products)
            {
                ProductComboBox.Items.Add(new ProductItem { Id = p.id, Name = p.name, Price = p.price });
            }
            if (ProductComboBox.Items.Count > 0)
                ProductComboBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProductComboBox.SelectedItem is not ProductItem item)
            {
                MessageBox.Show("Please select a product.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text.Trim(), out int qty) || qty < 1)
            {
                MessageBox.Show("Please enter a valid quantity (positive integer).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result = new AddLineResult { ProdId = item.Id, ProdName = item.Name, Quantity = qty, Price = item.Price };
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private class ProductItem
        {
            public long Id { get; set; }
            public string Name { get; set; } = "";
            public decimal Price { get; set; }
            public override string ToString() => $"{Name} - ${Price:N2}";
        }
    }
}
