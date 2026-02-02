using System;
using System.Collections.ObjectModel;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Windows;
using System.Windows.Controls;

namespace aim.legacy.UI
{
    /// <summary>
    /// Screen for managing customer records.
    /// Provides functionality to view, add, edit, delete, and search customers.
    /// </summary>
    public partial class CustomersScreen : UserControl
    {
        private readonly ObservableCollection<CustomerRow> _customers = new();

        public CustomersScreen()
        {
            InitializeComponent();
            CustomerTable.ItemsSource = _customers;
        }

        public void Refresh()
        {
            LoadCustomers();
        }

        private void LoadCustomers()
        {
            _customers.Clear();
            try
            {
                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null) return;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT cust_id, cust_name, email, phone, address, customer_type FROM customer ORDER BY cust_id";
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    _customers.Add(new CustomerRow
                    {
                        CustId = reader.GetInt64(0),
                        CustName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Address = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        CustomerType = reader.IsDBNull(5) ? "" : reader.GetString(5)
                    });
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchField.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                LoadCustomers();
                return;
            }

            _customers.Clear();
            try
            {
                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null) return;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT cust_id, cust_name, email, phone, address, customer_type FROM customer " +
                    "WHERE LOWER(cust_name) LIKE @query ORDER BY cust_id";
                cmd.Parameters.AddWithValue("@query", "%" + query.ToLower() + "%");

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    _customers.Add(new CustomerRow
                    {
                        CustId = reader.GetInt64(0),
                        CustName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Email = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        Address = reader.IsDBNull(4) ? "" : reader.GetString(4),
                        CustomerType = reader.IsDBNull(5) ? "" : reader.GetString(5)
                    });
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error searching customers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            LoadCustomers();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            var dialog = new CustomerDialog(owner, 0, "", "", "", "", "STANDARD");
            dialog.ShowDialog();

            if (dialog.IsSaved)
            {
                try
                {
                    var conn = aim.legacy.db.DB.GetConn();
                    if (conn == null) return;

                    long nextId = 1;
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT MAX(cust_id) FROM customer";
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            nextId = Convert.ToInt64(result) + 1;
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO customer (cust_id, cust_name, email, phone, address, customer_type) " +
                            "VALUES (@id, @name, @email, @phone, @address, @type)";
                        cmd.Parameters.AddWithValue("@id", nextId);
                        cmd.Parameters.AddWithValue("@name", dialog.CustomerName);
                        cmd.Parameters.AddWithValue("@email", dialog.Email);
                        cmd.Parameters.AddWithValue("@phone", dialog.Phone);
                        cmd.Parameters.AddWithValue("@address", dialog.Address);
                        cmd.Parameters.AddWithValue("@type", dialog.CustomerType);
                        cmd.ExecuteNonQuery();
                    }

                    LoadCustomers();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error adding customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = CustomerTable.SelectedItem as CustomerRow;
            if (selected == null)
            {
                MessageBox.Show("Please select a customer to edit", "Edit", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var owner = Window.GetWindow(this);
            var dialog = new CustomerDialog(owner, selected.CustId, selected.CustName, selected.Email, selected.Phone, selected.Address, selected.CustomerType);
            dialog.ShowDialog();

            if (dialog.IsSaved)
            {
                try
                {
                    var conn = aim.legacy.db.DB.GetConn();
                    if (conn == null) return;

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "UPDATE customer SET cust_name = @name, email = @email, phone = @phone, address = @address, customer_type = @type WHERE cust_id = @id";
                    cmd.Parameters.AddWithValue("@id", selected.CustId);
                    cmd.Parameters.AddWithValue("@name", dialog.CustomerName);
                    cmd.Parameters.AddWithValue("@email", dialog.Email);
                    cmd.Parameters.AddWithValue("@phone", dialog.Phone);
                    cmd.Parameters.AddWithValue("@address", dialog.Address);
                    cmd.Parameters.AddWithValue("@type", dialog.CustomerType);
                    cmd.ExecuteNonQuery();

                    LoadCustomers();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = CustomerTable.SelectedItem as CustomerRow;
            if (selected == null)
            {
                MessageBox.Show("Please select a customer to delete", "Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this customer?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var conn = aim.legacy.db.DB.GetConn();
                    if (conn == null) return;

                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM customer WHERE cust_id = @id";
                    cmd.Parameters.AddWithValue("@id", selected.CustId);
                    cmd.ExecuteNonQuery();

                    LoadCustomers();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private class CustomerRow
        {
            public long CustId { get; set; }
            public string CustName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Address { get; set; } = "";
            public string CustomerType { get; set; } = "";
        }
    }
}
