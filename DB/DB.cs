using System;
using Microsoft.Data.Sqlite;

namespace aim.legacy.db
{
    /// <summary>
    /// Database connection manager for the Order Entry System.
    /// Handles SQLite database initialization and connection management.
    /// </summary>
    public static class DB
    {
        private static SqliteConnection _conn;
        private static readonly string DbFile = "orderentry.db";

        /// <summary>
        /// Returns database connection, creates new if needed.
        /// Initializes schema and seeds data when database is empty.
        /// </summary>
        public static SqliteConnection GetConn()
        {
            if (_conn == null)
            {
                try
                {
                    _conn = new SqliteConnection($"Data Source={DbFile}");
                    _conn.Open();
                    InitDB();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    Environment.Exit(1);
                }
            }
            return _conn;
        }

        /// <summary>
        /// Initialize database schema and seed with initial data if empty.
        /// Creates all required tables with proper structure.
        /// </summary>
        private static void InitDB()
        {
            using var stmt = _conn.CreateCommand();

            stmt.CommandText = @"CREATE TABLE IF NOT EXISTS customer (
                cust_id INTEGER PRIMARY KEY,
                cust_name TEXT NOT NULL,
                email TEXT,
                phone TEXT,
                address TEXT,
                customer_type TEXT DEFAULT 'STANDARD')";
            stmt.ExecuteNonQuery();

            // Add customer_type column to existing databases if it doesn't exist
            try
            {
                stmt.CommandText = "ALTER TABLE customer ADD COLUMN customer_type TEXT DEFAULT 'STANDARD'";
                stmt.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Column already exists, ignore
            }

            stmt.CommandText = @"CREATE TABLE IF NOT EXISTS product (
                prod_id INTEGER PRIMARY KEY,
                prod_name TEXT NOT NULL,
                unit_price REAL NOT NULL)";
            stmt.ExecuteNonQuery();

            stmt.CommandText = @"CREATE TABLE IF NOT EXISTS orders (
                order_id INTEGER PRIMARY KEY,
                cust_id INTEGER NOT NULL,
                cust_name TEXT,
                order_date TEXT,
                subtotal REAL,
                discount REAL,
                tax REAL,
                total REAL)";
            stmt.ExecuteNonQuery();

            stmt.CommandText = @"CREATE TABLE IF NOT EXISTS order_line (
                line_id INTEGER PRIMARY KEY,
                order_id INTEGER NOT NULL,
                prod_id INTEGER,
                prod_name TEXT,
                quantity INTEGER,
                unit_price REAL)";
            stmt.ExecuteNonQuery();

            stmt.CommandText = "SELECT COUNT(*) FROM customer";
            var count = Convert.ToInt32(stmt.ExecuteScalar());
            if (count == 0)
            {
                SeedData();
            }
        }

        /// <summary>
        /// Seed database with sample customer and product data.
        /// Also creates a few test orders to demonstrate the system.
        /// </summary>
        private static void SeedData()
        {
            using var stmt = _conn.CreateCommand();

            stmt.CommandText = "INSERT INTO customer VALUES (1, 'John Doe', 'john.doe@email.com', '555-0101', '123 Main St', 'STANDARD')";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO customer VALUES (2, 'Jane Smith', 'jane.smith@email.com', '555-0102', '456 Oak Ave', 'PREMIUM')";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO customer VALUES (3, 'Bob Johnson', 'bob.j@email.com', '555-0103', '789 Pine Rd', 'STANDARD')";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO customer VALUES (4, 'Alice Williams', 'alice.w@email.com', '555-0104', '321 Elm St', 'VIP')";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO customer VALUES (5, 'Charlie Brown', 'charlie.b@email.com', '555-0105', '654 Maple Dr', 'PREMIUM')";
            stmt.ExecuteNonQuery();

            stmt.CommandText = "INSERT INTO product VALUES (1, 'Laptop', 1299.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (2, 'Smartphone', 899.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (3, 'Tablet', 599.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (4, 'Monitor', 349.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (5, 'Keyboard', 149.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (6, 'Mouse', 29.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (7, 'Headphones', 199.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (8, 'Webcam', 89.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (9, 'USB Hub', 39.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO product VALUES (10, 'Desk Lamp', 49.99)";
            stmt.ExecuteNonQuery();

            stmt.CommandText = "INSERT INTO orders VALUES (1, 1, 'John Doe', '2024-01-15 10:30:00', 1929.97, 96.50, 274.99, 2108.46)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO order_line VALUES (1, 1, 1, 'Laptop', 1, 1299.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO order_line VALUES (2, 1, 3, 'Tablet', 1, 599.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO order_line VALUES (3, 1, 6, 'Mouse', 1, 29.99)";
            stmt.ExecuteNonQuery();

            stmt.CommandText = "INSERT INTO orders VALUES (2, 2, 'Jane Smith', '2024-01-16 14:15:00', 549.98, 27.50, 78.38, 600.86)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO order_line VALUES (4, 2, 4, 'Monitor', 1, 349.99)";
            stmt.ExecuteNonQuery();
            stmt.CommandText = "INSERT INTO order_line VALUES (5, 2, 7, 'Headphones', 1, 199.99)";
            stmt.ExecuteNonQuery();
        }

        /// <summary>
        /// Close database connection when application shuts down.
        /// </summary>
        public static void CloseConn()
        {
            if (_conn != null)
            {
                _conn.Dispose();
                _conn = null;
            }
        }
    }
}
