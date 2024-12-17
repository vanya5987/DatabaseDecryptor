using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace ReportGenerator
{
    public class ReportGenerator
    {
        private readonly string _dbPath;
        private readonly string _secretKey;

        public ReportGenerator(string dbPath, string secretKey)
        {
            _dbPath = dbPath;
            _secretKey = secretKey;
        }

        public void GenerateReports()
        {
            Console.WriteLine("Инициализация SQLCipher...");
            SQLitePCL.Batteries_V2.Init();

            Console.WriteLine("Создание соединения с базой данных и установка ключа...");

            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            using (var connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                SetCipherKey(connection);

                using (var context = new ApplicationDbContext(connection))
                {
                    Console.WriteLine("Генерация отчетов...");
                    GetRegistrationsByCountry(context);
                    GetRegistrationsByRegion(context, "Россия");
                    GetRegistrationsByMonth(context, 2024);
                }
            }
        }

        private void SetCipherKey(SqliteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                // Устанавливаем совместимость с SQLCipher 3
                command.CommandText = "PRAGMA cipher_compatibility = 3;";
                command.ExecuteNonQuery();

                // Устанавливаем ключ
                command.CommandText = $"PRAGMA key = '{_secretKey}';";
                command.ExecuteNonQuery();

                Console.WriteLine("Ключ шифрования установлен успешно.");
            }
        }

        private void GetRegistrationsByCountry(ApplicationDbContext context)
        {
            var result = context.Users
                .Where(u => u.IsOwner == 1 && u.Confirmed == 1)
                .GroupBy(u => u.Country)
                .Select(g => new { Country = g.Key, Count = g.Count() });

            Console.WriteLine("\nОтчет по странам:");
            foreach (var item in result)
                Console.WriteLine($"{item.Country}: {item.Count}");
        }

        private void GetRegistrationsByRegion(ApplicationDbContext context, string country)
        {
            var result = context.Users
                .Where(u => u.IsOwner == 1 && u.Confirmed == 1 && u.Country == country)
                .GroupBy(u => u.Region)
                .Select(g => new { Region = g.Key, Count = g.Count() });

            Console.WriteLine($"\nОтчет по регионам для страны {country}:");
            foreach (var item in result)
                Console.WriteLine($"{item.Region}: {item.Count}");
        }

        private void GetRegistrationsByMonth(ApplicationDbContext context, int year)
        {
            var result = context.Users
                .Where(u => u.IsOwner == 1 && u.Confirmed == 1 &&
                            u.PurchaseDate.StartsWith(year.ToString()))
                .GroupBy(u => u.PurchaseDate.Substring(5, 2))
                .Select(g => new { Month = g.Key, Count = g.Count() });

            Console.WriteLine($"\nОтчет по месяцам за {year} год:");
            foreach (var item in result)
                Console.WriteLine($"Месяц {item.Month}: {item.Count}");
        }
    }

    public class ApplicationDbContext : DbContext
    {
        private readonly DbConnection _connection;

        public DbSet<User> Users { get; set; }

        public ApplicationDbContext(DbConnection connection)
        {
            _connection = connection;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_connection);
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public string Country { get; set; }
        public string Region { get; set; }
        public string PurchaseDate { get; set; }
        public int IsOwner { get; set; }
        public int Confirmed { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SQLitePCL.Batteries_V2.Init(); // Обязательная инициализация для SQLCipher

            string dbPath = @"C:\Users\admin\source\repos\ТестРашифровки\ТестРашифровки\Data\test.db";
            string secretKey = "secret_key"; // Ваш ключ шифрования

            var reportGenerator = new ReportGenerator(dbPath, secretKey);
            reportGenerator.GenerateReports();
        }
    }
}
