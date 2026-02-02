using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace aim.legacy.UI
{
    /// <summary>
    /// Complex dialog for creating and editing orders.
    /// Handles customer selection, line items, and automatic pricing calculations.
    /// Implements temp-table pattern for managing line items before save.
    /// </summary>
    public partial class OrderEditorDialog : Window
    {
        private long _orderId;
        private bool _saved;
        private readonly Dictionary<string, long> _customerMap = new();
        private readonly Dictionary<string, string> _customerTypeMap = new();
        private readonly ObservableCollection<TempLine> _tempLines = new();
        private readonly List<TempLine> _tempLinesList = new();

        private const decimal TaxRateStandard = 0.14975m; // 14.975%

        private class TempLine
        {
            public long LineId { get; set; }
            public long ProdId { get; set; }
            public string ProdName { get; set; } = "";
            public int Qty { get; set; }
            public decimal Price { get; set; }
            public string PriceDisplay => "$" + Price.ToString("N2");
            public decimal LineTotal => Price * Qty;
            public string LineTotalDisplay => "$" + LineTotal.ToString("N2");
        }

        public long OrderId => _orderId;
        public bool IsSaved => _saved;

        public OrderEditorDialog(Window owner, long orderId)
        {
            Owner = owner;
            _orderId = orderId;

            InitializeComponent();

            Title = orderId == 0 ? "New Order" : "Edit Order";
            LinesDataGrid.ItemsSource = _tempLines;

            LoadCustomers();
            if (orderId > 0)
                LoadOrder();
            CalculateTotals();
        }

        private void CustomerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateTotals();
        }

        private void LoadCustomers()
        {
            try
            {
                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null) return;

                CustomerComboBox.Items.Clear();
                _customerMap.Clear();
                _customerTypeMap.Clear();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT cust_id, cust_name, customer_type FROM customer ORDER BY cust_name";
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    long id = reader.GetInt64(0);
                    string name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    string customerType = reader.IsDBNull(2) ? "STANDARD" : reader.GetString(2);

                    CustomerComboBox.Items.Add(name);
                    _customerMap[name] = id;
                    _customerTypeMap[name] = customerType ?? "STANDARD";
                }
            }
            catch (Exception ex)
            {
                StatusTextBox.Text = "Error loading customers: " + ex.Message;
            }
        }

        private void LoadOrder()
        {
            try
            {
                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null) return;

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT cust_name FROM orders WHERE order_id = @id";
                    cmd.Parameters.AddWithValue("@id", _orderId);
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        string custName = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        CustomerComboBox.SelectedItem = custName;
                    }
                }

                _tempLines.Clear();
                _tempLinesList.Clear();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT line_id, prod_id, prod_name, quantity, unit_price FROM order_line WHERE order_id = @id";
                    cmd.Parameters.AddWithValue("@id", _orderId);
                    using var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var line = new TempLine
                        {
                            LineId = reader.GetInt64(0),
                            ProdId = reader.GetInt64(1),
                            ProdName = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            Qty = reader.GetInt32(3),
                            Price = reader.GetDecimal(4)
                        };
                        _tempLines.Add(line);
                        _tempLinesList.Add(line);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBox.Text = "Error loading order: " + ex.Message;
            }
        }

        private void RefreshLines()
        {
            _tempLines.Clear();
            foreach (var line in _tempLinesList)
                _tempLines.Add(line);
        }

        private void CalculateTotals()
        {
            decimal subtotal = _tempLinesList.Sum(l => l.Price * l.Qty);
            subtotal = Math.Round(subtotal, 2);

            string customerName = CustomerComboBox.SelectedItem as string;
            string customerType = customerName != null && _customerTypeMap.TryGetValue(customerName, out var ct) ? ct : "STANDARD";

            decimal discount = 0;
            decimal taxRate = TaxRateStandard;

            if (customerType == "VIP")
            {
                discount = subtotal * 0.20m;
                taxRate = 0.10m;
            }
            else if (customerType == "PREMIUM")
            {
                if (subtotal >= 1500) discount = subtotal * 0.18m;
                else if (subtotal >= 800) discount = subtotal * 0.12m;
                else if (subtotal >= 400) discount = subtotal * 0.07m;
                taxRate = 0.12m;
            }
            else
            {
                if (subtotal >= 2000) discount = subtotal * 0.15m;
                else if (subtotal >= 1000) discount = subtotal * 0.10m;
                else if (subtotal >= 500) discount = subtotal * 0.05m;
            }
            discount = Math.Round(discount, 2);

            decimal taxableAmount = subtotal - discount;
            decimal tax = Math.Round(taxableAmount * taxRate, 2);
            decimal total = Math.Round(subtotal - discount + tax, 2);

            SubtotalLabel.Text = "$" + subtotal.ToString("N2");
            DiscountLabel.Text = "$" + discount.ToString("N2");
            TaxLabel.Text = "$" + tax.ToString("N2");
            TotalLabel.Text = "$" + total.ToString("N2");
        }

        private void AddLineButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null) return;

                var products = new List<(long id, string name, decimal price)>();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT prod_id, prod_name, unit_price FROM product ORDER BY prod_name";
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        products.Add((
                            reader.GetInt64(0),
                            reader.IsDBNull(1) ? "" : reader.GetString(1),
                            reader.GetDecimal(2)
                        ));
                    }
                }

                if (products.Count == 0)
                {
                    MessageBox.Show("No products available", "Add Line", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new AddLineDialog(this, products);
                if (dialog.ShowDialog() == true && dialog.Result != null)
                {
                    var r = dialog.Result;
                    long nextLineId = _tempLinesList.Count + 1;
                    var line = new TempLine
                    {
                        LineId = nextLineId,
                        ProdId = r.ProdId,
                        ProdName = r.ProdName,
                        Qty = r.Quantity,
                        Price = r.Price
                    };
                    _tempLinesList.Add(line);
                    _tempLines.Add(line);
                    CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error adding line: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveLineButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = LinesDataGrid.SelectedItem as TempLine;
            if (selected == null)
            {
                MessageBox.Show("Please select a line to remove", "Remove Line", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _tempLinesList.Remove(selected);
            RefreshLines();
            CalculateTotals();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string customerName = CustomerComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(customerName))
            {
                MessageBox.Show("Please select a customer", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_customerMap.TryGetValue(customerName, out long custId))
            {
                MessageBox.Show("Customer not found", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var errors = new List<string>();

            if (_tempLinesList.Count == 0)
                errors.Add("Order must have at least one line item");

            for (int i = 0; i < _tempLinesList.Count; i++)
            {
                var line = _tempLinesList[i];
                if (line.Qty <= 0)
                    errors.Add($"Line {i + 1}: Quantity must be positive");
                if (line.Price < 0)
                    errors.Add($"Line {i + 1}: Unit price must be zero or greater");
            }

            decimal subtotal = _tempLinesList.Sum(l => l.Price * l.Qty);
            subtotal = Math.Round(subtotal, 2);

            string customerType = _customerTypeMap.TryGetValue(customerName, out var ct) ? ct : "STANDARD";
            decimal discount = 0;
            decimal maxDiscountRate = 0.15m;

            if (customerType == "VIP")
            {
                discount = subtotal * 0.20m;
                maxDiscountRate = 0.20m;
            }
            else if (customerType == "PREMIUM")
            {
                if (subtotal >= 1500) discount = subtotal * 0.18m;
                else if (subtotal >= 800) discount = subtotal * 0.12m;
                else if (subtotal >= 400) discount = subtotal * 0.07m;
                maxDiscountRate = 0.18m;
            }
            else
            {
                if (subtotal >= 2000) discount = subtotal * 0.15m;
                else if (subtotal >= 1000) discount = subtotal * 0.10m;
                else if (subtotal >= 500) discount = subtotal * 0.05m;
            }
            discount = Math.Round(discount, 2);

            if (subtotal > 0)
            {
                decimal discountRate = discount / subtotal;
                if (discountRate > maxDiscountRate)
                    errors.Add($"Discount cannot exceed {(int)(maxDiscountRate * 100)}%");
            }

            if (errors.Count > 0)
            {
                StatusTextBox.Text = "Validation errors:\n" + string.Join("\n", errors.Select(err => "- " + err));
                MessageBox.Show("Validation errors:\n\n" + string.Join("\n", errors), "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null) return;

                decimal taxRate = TaxRateStandard;
                if (customerType == "VIP") taxRate = 0.10m;
                else if (customerType == "PREMIUM") taxRate = 0.12m;

                decimal taxableAmount = subtotal - discount;
                decimal tax = Math.Round(taxableAmount * taxRate, 2);
                decimal total = Math.Round(subtotal - discount + tax, 2);

                var custNameEscaped = customerName.Replace("'", "''");

                if (_orderId == 0)
                {
                    long nextOrderId = 1;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT MAX(order_id) FROM orders";
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            nextOrderId = Convert.ToInt64(result) + 1;
                        _orderId = nextOrderId;
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO orders (order_id, cust_id, cust_name, order_date, subtotal, discount, tax, total) " +
                            "VALUES (@id, @custId, @custName, @orderDate, @subtotal, @discount, @tax, @total)";
                        cmd.Parameters.AddWithValue("@id", _orderId);
                        cmd.Parameters.AddWithValue("@custId", custId);
                        cmd.Parameters.AddWithValue("@custName", custName);
                        cmd.Parameters.AddWithValue("@orderDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@subtotal", subtotal);
                        cmd.Parameters.AddWithValue("@discount", discount);
                        cmd.Parameters.AddWithValue("@tax", tax);
                        cmd.Parameters.AddWithValue("@total", total);
                        cmd.ExecuteNonQuery();
                    }

                    long nextLineId = 1;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT MAX(line_id) FROM order_line";
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            nextLineId = Convert.ToInt64(result) + 1;
                    }

                    foreach (var line in _tempLinesList)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO order_line (line_id, order_id, prod_id, prod_name, quantity, unit_price) " +
                            "VALUES (@lineId, @orderId, @prodId, @prodName, @qty, @price)";
                        cmd.Parameters.AddWithValue("@lineId", nextLineId++);
                        cmd.Parameters.AddWithValue("@orderId", _orderId);
                        cmd.Parameters.AddWithValue("@prodId", line.ProdId);
                        cmd.Parameters.AddWithValue("@prodName", line.ProdName);
                        cmd.Parameters.AddWithValue("@qty", line.Qty);
                        cmd.Parameters.AddWithValue("@price", line.Price);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "UPDATE orders SET cust_id = @custId, cust_name = @custName, " +
                            "subtotal = @subtotal, discount = @discount, tax = @tax, total = @total WHERE order_id = @id";
                        cmd.Parameters.AddWithValue("@id", _orderId);
                        cmd.Parameters.AddWithValue("@custId", custId);
                        cmd.Parameters.AddWithValue("@custName", custName);
                        cmd.Parameters.AddWithValue("@subtotal", subtotal);
                        cmd.Parameters.AddWithValue("@discount", discount);
                        cmd.Parameters.AddWithValue("@tax", tax);
                        cmd.Parameters.AddWithValue("@total", total);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM order_line WHERE order_id = @id";
                        cmd.Parameters.AddWithValue("@id", _orderId);
                        cmd.ExecuteNonQuery();
                    }

                    long nextLineId = 1;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT MAX(line_id) FROM order_line";
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            nextLineId = Convert.ToInt64(result) + 1;
                    }

                    foreach (var line in _tempLinesList)
                    {
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = "INSERT INTO order_line (line_id, order_id, prod_id, prod_name, quantity, unit_price) " +
                            "VALUES (@lineId, @orderId, @prodId, @prodName, @qty, @price)";
                        cmd.Parameters.AddWithValue("@lineId", nextLineId++);
                        cmd.Parameters.AddWithValue("@orderId", _orderId);
                        cmd.Parameters.AddWithValue("@prodId", line.ProdId);
                        cmd.Parameters.AddWithValue("@prodName", line.ProdName);
                        cmd.Parameters.AddWithValue("@qty", line.Qty);
                        cmd.Parameters.AddWithValue("@price", line.Price);
                        cmd.ExecuteNonQuery();
                    }
                }

                StatusTextBox.Text = "Order saved successfully";
                _saved = true;

                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                timer.Tick += (s, args) => { timer.Stop(); DialogResult = true; Close(); };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving order: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _saved = false;
            DialogResult = false;
            Close();
        }
    }
}
