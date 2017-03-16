using Microsoft.Extensions.Configuration;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace FluentSchedulerDemo
{
    public class DbHelper
    {
        public static IDbConnection GetConnection()
        {
            var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();

            var connectionStringsSection = configuration.GetSection("ConnectionStrings");
            var connstr2 = configuration["ConnectionStrings:SqlServer"];

            //Console.WriteLine($"ConnectionStrings = {configuration["ConnectionStrings"]}");
            //Console.WriteLine($"Logging = {configuration["Logging"]}");
            //Console.WriteLine(
            //    $"ConnectionStrings = {configuration["ConnectionStrings:SqlServer"]}");

            var conn = new SqlConnection(connstr2);
            return conn;
        }
    }
}
