using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Linq;
using NLog;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Configuration;

namespace datamanager
{
    class Program
    {
        static string provider;
        static string databaseName;
        static string serverName;
        static string serverLogin;
        static string password;
        static int port;

        static void Main(string[] args)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            LogManager.Setup().LoadConfigurationFromFile(config.FilePath);

            LogManager.GetCurrentClassLogger().Info($"Запуск приложения");

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                LogManager.GetCurrentClassLogger().Fatal(e.ExceptionObject as Exception, "Ошибка выполнения приложения");
                LogManager.Shutdown();
            };

            string connectionString = GetConnectionString();
            var matches = Regex.Matches(connectionString, @"(?<Key>[^=;]+)=(?<Val>[^;]+)");

            provider = matches.FirstOrDefault(c => c.Groups["Key"].Value == "XpoProvider")?.Groups["Val"]?.Value;

            databaseName = "";
            switch (provider)
            {
                case "MSSqlServer":
                    databaseName = matches.FirstOrDefault(c => c.Groups["Key"].Value == "initial catalog")?.Groups["Val"]?.Value;
                    serverName = matches.FirstOrDefault(c => c.Groups["Key"].Value == "data source")?.Groups["Val"]?.Value;

                    break;

                case "Postgres":
                    databaseName = matches.FirstOrDefault(c => c.Groups["Key"].Value == "database")?.Groups["Val"]?.Value;
                    serverName = matches.FirstOrDefault(c => c.Groups["Key"].Value == "server")?.Groups["Val"]?.Value;

                    break;
            }


            serverLogin = matches.FirstOrDefault(c => c.Groups["Key"].Value == "user id")?.Groups["Val"]?.Value;
            password = matches.FirstOrDefault(c => c.Groups["Key"].Value == "password")?.Groups["Val"]?.Value;
            port = int.Parse(matches.First(c => c.Groups["Key"].Value == "port")?.Groups["Val"]?.Value ?? "5432");

            bool newDb = !IsStorageExist(databaseName);

            if (newDb)
                CreateStorage();

            DevExpress.Xpo.Metadata.XPDictionary dict = new DevExpress.Xpo.Metadata.ReflectionDictionary();
            DevExpress.Xpo.DB.IDataStore store = XpoDefault.GetConnectionProvider(GetSystemConnectionString(), DevExpress.Xpo.DB.AutoCreateOption.DatabaseAndSchema);

            List<Assembly> src = new List<Assembly>();
            src.AddRange(AppDomain.CurrentDomain.GetAssemblies());
            if (!src.Where(c => c == typeof(DbAbstractDataObject).Assembly).Any())
                src.Add(typeof(DbAbstractDataObject).Assembly);

            dict.GetDataStoreSchema(src);
            XpoDefault.DataLayer = new ThreadSafeDataLayer(dict, store);

            Session session = new();
            session.CreateObjectTypeRecords();
            session.UpdateSchema();

            //DataInitializer initializer = new(@"..\..\..\..\InitPackage.xml");
            DataInitializer initializer = new(@"..\..\..\..\SymptomsPackage.xml");
            initializer.Seed(session);

            LogManager.GetCurrentClassLogger().Info($"Завершение приложения");
            LogManager.Shutdown();
        }

        public static string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["Data"].ConnectionString;
        }

        public static string GetSystemConnectionString()
        {
            return GetConnectionToDB(databaseName);
        }

        public static string GetConnectionString(String usr, String pwd)
        {
            return MSSqlConnectionProvider.GetConnectionString(serverName, usr, pwd, databaseName);
        }

        public static void CreateStorage()
        {
            LogManager.GetCurrentClassLogger().Info($"Создание базы данных {databaseName}");
            XpoDefault.ConnectionString = null;

            if (IsStorageExist(databaseName))
            {
                XpoDefault.DataLayer = null;
                throw new Exception(String.Format("База данных с именем [{0}] уже существует!", databaseName));
            }

            switch (provider)
            {
                //case "MSSqlServer":
                //    {
                //        IDataLayer dataLayer = DevExpress.Xpo.XpoDefault.GetDataLayer(GetConnectionToDB("master"), DevExpress.Xpo.DB.AutoCreateOption.None);
                //        IDbCommand iCommand = dataLayer.Connection.CreateCommand();
                //        iCommand.CommandText = $"SELECT name FROM sysdatabases WHERE name='{databaseName}'";
                //        object x = iCommand.ExecuteScalar();
                //    }

                //    break;


                case "Postgres":
                    {
                        IDataLayer dataLayer = DevExpress.Xpo.XpoDefault.GetDataLayer(GetConnectionToDB("postgres"), DevExpress.Xpo.DB.AutoCreateOption.None);
                        IDbCommand iCommand = dataLayer.Connection.CreateCommand();
                        iCommand.CommandText = $"CREATE DATABASE {databaseName} WITH OWNER = {serverLogin} ENCODING = 'UTF8' LC_COLLATE = 'ru_RU.UTF-8' LC_CTYPE = 'ru_RU.UTF-8' TABLESPACE = pg_default CONNECTION LIMIT = -1 IS_TEMPLATE = False;";
                        object x = iCommand.ExecuteScalar();
                    }
                    break;

                default:
                    throw new Exception("Неизвестный провайдер СУБД");
            }
        }

        public static bool IsStorageExist(string dbName)
        {
            LogManager.GetCurrentClassLogger().Info($"Проверка существования базы данных {dbName}");

            switch (provider)
            {
                case "MSSqlServer":
                    {
                        IDataLayer dataLayer = DevExpress.Xpo.XpoDefault.GetDataLayer(GetConnectionToDB("master"), DevExpress.Xpo.DB.AutoCreateOption.None);
                        IDbCommand iCommand = dataLayer.Connection.CreateCommand();
                        iCommand.CommandText = $"SELECT name FROM sysdatabases WHERE name='{dbName}'";
                        object x = iCommand.ExecuteScalar();
                        return x != null;
                    }

                    break;


                case "Postgres":
                    {
                        IDataLayer dataLayer = DevExpress.Xpo.XpoDefault.GetDataLayer(GetConnectionToDB("postgres"), DevExpress.Xpo.DB.AutoCreateOption.None);
                        IDbCommand iCommand = dataLayer.Connection.CreateCommand();
                        iCommand.CommandText = $"SELECT datname FROM pg_catalog.pg_database where datname = '{dbName}';";
                        object x = iCommand.ExecuteScalar();
                        return x != null;
                    }
                    break;

                default:
                    throw new Exception("Неизвестный провайдер СУБД");
            }
        }

        protected static string GetConnectionToDB(string dbName)
        {
            switch (provider)
            {
                case "MSSqlServer":
                    return DevExpress.Xpo.DB.MSSqlConnectionProvider.GetConnectionString(serverName, serverLogin, password, dbName);

                case "Postgres":
                    return DevExpress.Xpo.DB.PostgreSqlConnectionProvider.GetConnectionString(serverName, port, serverLogin, password, dbName);

                default:
                    throw new Exception("Неизвестный провайдер СУБД");

            }
        }

    }
}
