using Microsoft.Data.Sqlite;
using System;

class Program
{
    static void Main()
    {
        using (var connection = new SqliteConnection("Data Source=227project/lms.db"))
        {
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT Id, Email
                FROM AspNetUsers
                WHERE Email IN ('admin@lms.com', 'john@lms.com', 'sarah@lms.com', 'mike@lms.com')
            ";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Console.WriteLine($"ID: {reader.GetString(0)}, Email: {reader.GetString(1)}");
                }
            }
        }
    }
}
