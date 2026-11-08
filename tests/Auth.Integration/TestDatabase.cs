using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

// Required before every mutating test, fixture, or review host.
static class TestDatabase
{
    public static string ConnectionString { get; private set; } = "";
    public static void RequireIsolated()
    {
        var value = Environment.GetEnvironmentVariable("KICKS_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Mutating tests require an explicit KICKS_TEST_CONNECTION to a restored Kicks_BookingTest database. Kicks is forbidden.");
        var builder = new SqlConnectionStringBuilder(value);
        if (!builder.IntegratedSecurity || !string.Equals(builder.DataSource, "localhost", StringComparison.OrdinalIgnoreCase)
            || !Regex.IsMatch(builder.InitialCatalog, @"^Kicks_BookingTest_\d{8}_[a-f0-9]{8}$"))
            throw new InvalidOperationException("Unsafe test connection. Only an explicitly named local restored Kicks_BookingTest database is allowed; Kicks is forbidden.");
        using var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN DB_NAME()=@name AND DB_NAME()<>N'Kicks' AND NOT EXISTS(SELECT 1 FROM sys.master_files test JOIN sys.master_files live ON live.physical_name=test.physical_name AND live.database_id=DB_ID(N'Kicks') WHERE test.database_id=DB_ID()) THEN 1 ELSE 0 END";
        command.Parameters.AddWithValue("@name", builder.InitialCatalog);
        if (Convert.ToInt32(command.ExecuteScalar()) != 1) throw new InvalidOperationException("Actual test database identity/file isolation check failed.");
        ConnectionString = builder.ConnectionString;
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
    }
}
