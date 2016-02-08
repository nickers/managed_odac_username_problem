using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace managed_odac_username_chars
{
	class Program
	{
		private static bool UserExists(string username, OracleConnection con)
		{
			string sql = "SELECT count(1) from all_users where username=upper(:username)";
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = sql;
				cmd.BindByName = true;
				cmd.CommandType = CommandType.Text;
				using (var param = new OracleParameter("username", OracleDbType.Varchar2, ParameterDirection.Input))
				{
					param.Value = username;
					cmd.Parameters.Add(param);
					object o = cmd.ExecuteScalar();
					return ((decimal)o)!= 0;
				}
			}
		}

		private static void CreateUser(string username, string pass, OracleConnection con)
		{

			string sql = "create user " + username + " identified by " + pass;
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = sql;
				cmd.BindByName = true;
				cmd.CommandType = CommandType.Text;
				cmd.ExecuteNonQuery();
			}
		}

		private static void GrantConnect(string username, OracleConnection con)
		{
			string sql = "GRANT CONNECT TO " + username;
			using (var cmd = con.CreateCommand())
			{
				cmd.CommandText = sql;
				cmd.BindByName = true;
				cmd.CommandType = CommandType.Text;
				cmd.ExecuteNonQuery();
			}
		}

		private static void DropUser(string username, OracleConnection con)
		{
			try
			{
				string sql = "DROP USER " + username;
				using (var cmd = con.CreateCommand())
				{
					cmd.CommandText = sql;
					cmd.BindByName = true;
					cmd.CommandType = CommandType.Text;
					cmd.ExecuteNonQuery();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("DROP USER failed: {0}", e.Message);
			}
		}

		private static void PrintDebug(OracleConnection con)
		{
			var debug = new Dictionary<string, string>
			{
				{ "NLS_DATABASE_PARAMETERS",  "SELECT parameter, value FROM NLS_DATABASE_PARAMETERS order by parameter asc"},
				{ "V$VERSION",  "SELECT banner, ' ' FROM V$VERSION order by banner asc"},
			};
			foreach (var e in debug)
			{
				Console.WriteLine("### {0} ###", e.Key);
				using (var cmd = con.CreateCommand())
				{
					cmd.CommandText = e.Value;
					cmd.CommandType = CommandType.Text;
					using (var reader = cmd.ExecuteReader())
					{
						while (reader.Read())
						{
							Console.WriteLine(@"{0,-24} = {1}", reader.GetString(0), reader.GetString(1));
						}
					}
				}
			}
		}

		private static bool TryConnect(string username, string password, string database)
		{
			OracleConnectionStringBuilder conStr = new OracleConnectionStringBuilder()
			{
				UserID = username,
				Password = password,
				DataSource = database
			};
			using (var con = new OracleConnection(conStr.ToString()))
			{
				try
				{
					con.Open();
				}
				catch (OracleException e)
				{
					Console.WriteLine("Exception on TryConnect() = {0}", e.Message);
					return false;
				}
				var si = con.GetSessionInfo();
				si.Language = "POLISH";
				con.SetSessionInfo(si);
				try
				{
					return true;
				}
				finally
				{
					con.Close();
				}
			}
		}

		private const string ProblematicUsername = @"ZAŻÓŁĆGĘŚLĄJAŹŃ";
		private const string ProblematicUserPass = @"TEST";

		static void Main(string[] args)
		{
			if (args.Length != 3)
			{
				Console.WriteLine("Usage: program.exe <username> <password> <db>");
				Environment.Exit(1);
			}

			string username = args[0];
			string password = args[1];
			string database = args[2];

			OracleConnectionStringBuilder conStr = new OracleConnectionStringBuilder()
			{
				UserID = username,
				Password = password,
				DataSource = database
			};

			using (var con = new OracleConnection(conStr.ToString()))
			{
				con.Open();
				var si = con.GetSessionInfo();
				si.Language = "POLISH";
				con.SetSessionInfo(si);
				try
				{
					bool exists = UserExists(ProblematicUsername, con);
					Console.WriteLine("User exists? = {0}", exists);
					if (!exists)
					{
						CreateUser(ProblematicUsername, ProblematicUserPass, con);
					}
					GrantConnect(ProblematicUsername, con);

					PrintDebug(con);
				}
				finally
				{
					con.Close();
				}
			}


			if (TryConnect(ProblematicUsername, ProblematicUserPass, database))
			{
				Console.WriteLine("OK, connected!");
			}

			using (var con = new OracleConnection(conStr.ToString()))
			{
				con.Open();
				var si = con.GetSessionInfo();
				si.Language = "POLISH";
				con.SetSessionInfo(si);
				DropUser(ProblematicUsername, con);
			}


			/*Console.WriteLine("Press enter...");
			Console.ReadLine();*/
		}
	}
}
