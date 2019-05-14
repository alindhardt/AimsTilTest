using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using System.Data.SqlClient;
using System.IO;

namespace AimsTilTest
{
    public class SQL
    {
        public string UNCPath { get; set; }
        private string path = @"\\gohsqltest\gohops";

        public SQL()
        {
            UNCPath = ConfigurationManager.AppSettings["UNCPath"];
        }
        //private string connectionString = "Data Source=GOHSQLXX; Initial Catalog = GL; Integrated Security = SSPI";

        public string BackupDB(string server, string database)
        {
            if (String.IsNullOrWhiteSpace(server))
                throw new ArgumentNullException("server");
            if (String.IsNullOrWhiteSpace(database))
                throw new ArgumentNullException("database");

            //var connectionString = "Data Source=" + server + "; Initial Catalog = " + database + "; Integrated Security = SSPI";
            var connectionString = String.Format("Data Source={0}; Initial Catalog = {1}; Integrated Security = SSPI", server, database);
            var connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch(SqlException e)
            {
                var exception = String.Format("Error connecting to server or database.\nServer: {0}, Database: {1}.\n\n{2}",server, database,e.Message);
                throw new DatabaseException(exception);
            }


            var timeStamp = DateTime.Now.ToString("ddMMyy-HHmm");
            var fullPath = UNCPath + database + timeStamp + ".bak";

            var query = "BACKUP DATABASE " + database + " TO DISK ='" + fullPath + "'";
            
            var sql = new SqlCommand(query, connection);
            sql.CommandTimeout = 900;

            try
            {
                Console.WriteLine("Backup started: {0}", fullPath);
                var start = DateTime.Now;
                sql.ExecuteNonQuery();
                var stop = DateTime.Now;
                Console.WriteLine("Backup completed. Elapsed time: {0:mm\\:ss}", (start - stop));
            }
            catch(Exception e)
            {
                if (File.Exists(fullPath))
                {
                    var exception = String.Format("Backup Failed and {0} still exists:\n{1}",fullPath , e.Message);
                    throw new DatabaseException(exception);
                }
                else
                {
                    var exception = String.Format("Backup Failed:\n{0}", e.Message);
                    throw new DatabaseException(exception);
                }
            }
            finally
            {
                connection.Close();
            }

            return fullPath;
        }

        public void RestoreDB(string server, string database, string path)
        {
            if (String.IsNullOrWhiteSpace(server))
                throw new ArgumentNullException("server");
            if (String.IsNullOrWhiteSpace(database))
                throw new ArgumentNullException("database");
            if (String.IsNullOrWhiteSpace(path))
                throw new ArgumentNullException("path");

            var connectionString = "Data Source=" + server + "; Initial Catalog = Master; Integrated Security = SSPI";
            var connection = new SqlConnection(connectionString);
            connection.Open();


            try
            {
                var query = "ALTER DATABASE " + database + " SET Single_User With Rollback Immediate";

                var sql = new SqlCommand(query, connection);
                sql.CommandTimeout = 4000;
                sql.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                var exception = String.Format("Error:\n{0}\n, StackTrace:\n{1}\n", e.Message, e.StackTrace);
                throw new DatabaseException(exception);
            }
            finally
            {
                try
                {
                    var query = "RESTORE DATABASE " + database + " FROM DISK ='" + path + "'";

                    var sql = new SqlCommand(query, connection);
                    sql.CommandTimeout = 4000;


                    Console.WriteLine("Restore Started: {0}/{1} with {2}", server, database, path);
                    var start = DateTime.Now;
                    sql.ExecuteNonQuery();
                    var stop = DateTime.Now;
                    Console.WriteLine("Restore completed. Elaspsed time: {0:mm\\:ss}", (start - stop));


                    File.Delete(path);
                }
                catch (Exception e)
                {
                    var exception = String.Format("Error:\n{0}\n, StackTrace:\n{1}\n", e.Message, e.StackTrace);
                    throw new DatabaseException(exception);
                }
                finally
                {
                    var query = "ALTER DATABASE " + database + " SET Multi_User";
                    var sql = new SqlCommand(query, connection);
                    sql.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }
    }

    [Serializable]
    public class DatabaseException : Exception
    {
        public DatabaseException() { }
        public DatabaseException(string message) : base(message) { }
        public DatabaseException(string message, Exception inner) : base(message, inner) { }
        protected DatabaseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
