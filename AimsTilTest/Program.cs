using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AimsTilTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var sql = new SQL();

            var backup = String.Empty;
            try
            {
                backup = sql.BackupDB("GOHSQLXX", "GL");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message.ToString());
                return;
            }
            

            try
            {
                sql.RestoreDB("GOHSQLTEST", "GL", backup);
            }
            catch (DatabaseException e)
            {
                Console.WriteLine(e.Message.ToString());
            }

            if(File.Exists(backup))
                Console.WriteLine("{0} not deleted. Please delete manually.",backup);
            Console.Write("Press Enter To Close.");
            Console.ReadLine();
        }
    }
}
