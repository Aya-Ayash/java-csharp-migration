using System;
using System.Collections.ObjectModel;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace aim.legacy.UI
{
    /// <summary>
    /// Screen for viewing and managing customer orders.
    /// Shows all orders with calculated totals and allows creating/editing orders.
    /// </summary>
    public partial class OrdersScreen : UserControl
    {
        private readonly ObservableCollection<OrderRow> _orders = new();

        public OrdersScreen()
        {
            InitializeComponent();
            OrderTable.ItemsSource = _orders;
        }

        public void Refresh()
        {
            LoadOrders();
        }

        private void LoadOrders()
        {
            _orders.Clear();
            try
            {
                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null) return;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT order_id, cust_name, order_date, subtotal, discount, tax, total FROM orders ORDER BY order_id";
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    _orders.Add(new OrderRow
                    {
                        OrderId = reader.GetInt64(0),
                        CustName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        OrderDate = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        Subtotal = reader.GetDouble(3),
                        Discount = reader.GetDouble(4),
                        Tax = reader.GetDouble(5),
                        Total = reader.GetDouble(6)
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading orders: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            var dialog = new OrderEditorDialog(owner, 0);
            dialog.ShowDialog();

            if (dialog.IsSaved)
            {
                LoadOrders();
            }
        }

        private void EditOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrderTable.SelectedItem as OrderRow;
            if (selected == null)
            {
                MessageBox.Show("Please select an order to edit", "Edit", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var owner = Window.GetWindow(this);
            var dialog = new OrderEditorDialog(owner, selected.OrderId);
            dialog.ShowDialog();

            if (dialog.IsSaved)
            {
                LoadOrders();
            }
        }

        private void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = OrderTable.SelectedItem as OrderRow;
            if (selected == null)
            {
                MessageBox.Show("Please select an order to delete", "Delete", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show("Are you sure you want to delete this order?", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var conn = aim.legacy.db.DB.GetConn();
                    if (conn == null) return;

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM order_line WHERE order_id = @id";
                        cmd.Parameters.AddWithValue("@id", selected.OrderId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM orders WHERE order_id = @id";
                        cmd.Parameters.AddWithValue("@id", selected.OrderId);
                        cmd.ExecuteNonQuery();
                    }

                    LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reportDir = "reports";
                var reportFilename = "OrderSummary.pdf";
                var filePath = Path.Combine(reportDir, reportFilename);

                Directory.CreateDirectory(reportDir);

                var document = new PdfDocument();
                var page = document.AddPage(PageSize.Letter);
                var gfx = XGraphics.FromPdfPage(page);

                var titleFont = new XFont("Helvetica", 18, XFontStyle.Bold);
                var headerFont = new XFont("Helvetica", 12, XFontStyle.Bold);
                var normalFont = new XFont("Helvetica", 10, XFontStyle.Regular);
                var smallFont = new XFont("Helvetica", 8, XFontStyle.Regular);

                const double margin = 50;
                double y = margin;

                gfx.DrawString("AIM Order Entry System", headerFont, XBrushes.Black, new XRect(0, y, page.Width, 20), XStringFormats.TopCenter);
                y += 25;

                gfx.DrawString("Order Summary Report", titleFont, XBrushes.Black, new XRect(0, y, page.Width, 25), XStringFormats.TopCenter);
                y += 30;

                var timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
                gfx.DrawString("Generated: " + timestamp, smallFont, XBrushes.Black, new XRect(0, y, page.Width, 15), XStringFormats.TopCenter);
                y += 40;

                var conn = aim.legacy.db.DB.GetConn();
                if (conn == null)
                {
                    MessageBox.Show("Database connection not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                double[] colWidths = { 50, 120, 90, 70, 70, 70, 70 };
                double colTotal = 0;
                foreach (var w in colWidths) colTotal += w;
                double scale = (page.Width - margin * 2) / colTotal;
                for (int i = 0; i < colWidths.Length; i++) colWidths[i] *= scale;

                string[] headers = { "Order ID", "Customer", "Date", "Subtotal", "Discount", "Tax", "Total" };
                double rowHeight = 22;
                var headerColor = XBrushes.LightGray;

                double x = margin;
                for (int i = 0; i < headers.Length; i++)
                {
                    gfx.DrawRectangle(headerColor, x, y, colWidths[i], rowHeight);
                    gfx.DrawString(headers[i], headerFont, XBrushes.Black,
                        new XRect(x, y, colWidths[i], rowHeight), XStringFormats.Center);
                    x += colWidths[i];
                }
                y += rowHeight;

                int totalOrders = 0;
                double totalRevenue = 0, totalDiscounts = 0, totalTax = 0;
                bool alternate = false;
                var rowColor = XBrushes.White;
                var altColor = new XSolidBrush(XColor.FromArgb(240, 240, 240));

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT order_id, cust_name, order_date, subtotal, discount, tax, total FROM orders ORDER BY order_id";
                    using var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        totalOrders++;
                        long orderId = reader.GetInt64(0);
                        string custName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        string orderDate = reader.IsDBNull(2) ? "" : FormatOrderDate(reader.GetString(2));
                        double subtotal = reader.GetDouble(3);
                        double discount = reader.GetDouble(4);
                        double tax = reader.GetDouble(5);
                        double total = reader.GetDouble(6);

                        totalRevenue += total;
                        totalDiscounts += discount;
                        totalTax += tax;

                        var bg = alternate ? altColor : rowColor;
                        x = margin;

                        DrawCell(gfx, orderId.ToString(), normalFont, x, y, colWidths[0], rowHeight, bg, true);
                        x += colWidths[0];
                        DrawCell(gfx, custName ?? "", normalFont, x, y, colWidths[1], rowHeight, bg, false);
                        x += colWidths[1];
                        DrawCell(gfx, orderDate ?? "", normalFont, x, y, colWidths[2], rowHeight, bg, false);
                        x += colWidths[2];
                        DrawCell(gfx, "$" + subtotal.ToString("N2", CultureInfo.InvariantCulture), normalFont, x, y, colWidths[3], rowHeight, bg, true);
                        x += colWidths[3];
                        DrawCell(gfx, "$" + discount.ToString("N2", CultureInfo.InvariantCulture), normalFont, x, y, colWidths[4], rowHeight, bg, true);
                        x += colWidths[4];
                        DrawCell(gfx, "$" + tax.ToString("N2", CultureInfo.InvariantCulture), normalFont, x, y, colWidths[5], rowHeight, bg, true);
                        x += colWidths[5];
                        DrawCell(gfx, "$" + total.ToString("N2", CultureInfo.InvariantCulture), normalFont, x, y, colWidths[6], rowHeight, bg, true);

                        y += rowHeight;
                        alternate = !alternate;

                        if (y > page.Height - margin - 100)
                        {
                            page = document.AddPage(PageSize.Letter);
                            gfx = XGraphics.FromPdfPage(page);
                            y = margin;
                        }
                    }
                }

                y += 20;
                gfx.DrawString("Summary Statistics", headerFont, XBrushes.Black, margin, y);
                y += 25;

                var summaryBg = new XSolidBrush(XColor.FromArgb(230, 230, 230));
                double sumCol1 = 150, sumCol2 = 100;
                DrawSummaryRow(gfx, "Total Orders:", totalOrders.ToString(), normalFont, headerFont, margin, y, sumCol1, sumCol2, summaryBg);
                y += 22;
                DrawSummaryRow(gfx, "Total Revenue:", "$" + totalRevenue.ToString("N2", CultureInfo.InvariantCulture), normalFont, headerFont, margin, y, sumCol1, sumCol2, summaryBg);
                y += 22;
                DrawSummaryRow(gfx, "Total Discounts:", "$" + totalDiscounts.ToString("N2", CultureInfo.InvariantCulture), normalFont, headerFont, margin, y, sumCol1, sumCol2, summaryBg);
                y += 22;
                DrawSummaryRow(gfx, "Total Tax Collected:", "$" + totalTax.ToString("N2", CultureInfo.InvariantCulture), normalFont, headerFont, margin, y, sumCol1, sumCol2, summaryBg);

                document.Save(filePath, false);

                MessageBox.Show($"Report generated successfully!\nSaved to: {filePath}", "Report Generated",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                var openResult = MessageBox.Show("Would you like to open the report?", "Open Report",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (openResult == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.GetFullPath(filePath),
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void DrawCell(XGraphics gfx, string text, XFont font, double x, double y, double w, double h, XBrush bg, bool rightAlign)
        {
            gfx.DrawRectangle(bg, x, y, w, h);
            gfx.DrawRectangle(XPens.Black, x, y, w, h);
            var format = rightAlign ? XStringFormats.TopRight : XStringFormats.TopLeft;
            var rect = new XRect(x + (rightAlign ? w - 4 : 4), y, w - 8, h);
            gfx.DrawString(text, font, XBrushes.Black, rect, format);
        }

        private static void DrawSummaryRow(XGraphics gfx, string label, string value, XFont normalFont, XFont headerFont,
            double x, double y, double col1, double col2, XBrush bg)
        {
            gfx.DrawRectangle(bg, x, y, col1, 20);
            gfx.DrawString(label, normalFont, XBrushes.Black, new XRect(x + 4, y, col1 - 8, 20), XStringFormats.TopLeft);
            gfx.DrawRectangle(bg, x + col1, y, col2, 20);
            gfx.DrawString(value, headerFont, XBrushes.Black, new XRect(x + col1, y, col2 - 4, 20), XStringFormats.TopRight);
        }

        private static string FormatOrderDate(string orderDate)
        {
            if (string.IsNullOrEmpty(orderDate) || orderDate.Length < 16) return orderDate ?? "";
            try
            {
                var parts = orderDate.Split(' ');
                if (parts.Length >= 2)
                {
                    var dateParts = parts[0].Split('-');
                    var time = parts[1].Length >= 5 ? parts[1].Substring(0, 5) : parts[1];
                    return $"{dateParts[1]}/{dateParts[2]}/{dateParts[0]} {time}";
                }
            }
            catch { }
            return orderDate;
        }

        private class OrderRow
        {
            public long OrderId { get; set; }
            public string CustName { get; set; } = "";
            public string OrderDate { get; set; } = "";
            public double Subtotal { get; set; }
            public double Discount { get; set; }
            public double Tax { get; set; }
            public double Total { get; set; }

            public string SubtotalDisplay => "$" + Subtotal.ToString("N2");
            public string DiscountDisplay => "$" + Discount.ToString("N2");
            public string TaxDisplay => "$" + Tax.ToString("N2");
            public string TotalDisplay => "$" + Total.ToString("N2");
        }
    }
}
