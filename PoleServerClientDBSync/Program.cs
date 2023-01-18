
using System.Data.SqlClient;
using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;

namespace MicrosoftSyncFramework
{
    class Program
    {
        static string sServerConnection =
            @"Data Source=SELI136724;Initial Catalog=PoleServer;Integrated Security=True";

        static string sClientConnection =
            @"Data Source=SELI136724;Initial Catalog=PoleClient;Integrated Security=True";

        static readonly string sScope = "MainScope";
        static SqlConnection serverConn = new SqlConnection(sServerConnection);
        static SqlConnection clientConn = new SqlConnection(sClientConnection);

        //Get Data From Client Provision
        public static void ProvisionClient()
        {
            try
            {
                //Drop scope_Info Table
                string cmdText = @"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES 
               WHERE TABLE_NAME='scope_info') DROP table scope_info";
                clientConn.Open();
                SqlCommand cmd = new SqlCommand(cmdText, clientConn);
                cmd.ExecuteScalar();
                clientConn.Close();

                List<string> tables = new List<string>();
                tables.Add("Employee"); // Add Tables in List

                DbSyncScopeDescription scopeDesc = new DbSyncScopeDescription("MainScope");
                foreach (var tbl in tables) //Add Tables in Scope
                {
                    scopeDesc.Tables.Add(SqlSyncDescriptionBuilder.GetDescriptionForTable(tbl, clientConn));
                }

                SqlSyncScopeProvisioning clientProvision = new SqlSyncScopeProvisioning(clientConn, scopeDesc); //Provisioning

                if (!clientProvision.ScopeExists(Program.sScope))
                    clientProvision.Apply();
            }
            catch
            {
                // Shut down database connections.
                clientConn.Close();
                clientConn.Dispose();
            }
        }

        //Set Data To Server Provision
        public static void ProvisionServer()
        {
            try
            {
                string cmdText = @"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES 
                   WHERE TABLE_NAME='scope_info') DROP table scope_info";
                serverConn.Open();
                SqlCommand cmd = new SqlCommand(cmdText, serverConn);
                cmd.ExecuteScalar();
                serverConn.Close();

                List<string> tables = new List<string>();
                tables.Add("Employee");

                var scopeDesc = new DbSyncScopeDescription("MainScope");
                foreach (var tbl in tables)
                {
                    scopeDesc.Tables.Add(SqlSyncDescriptionBuilder.GetDescriptionForTable(tbl, serverConn));
                }

                SqlSyncScopeProvisioning serverProvision = new SqlSyncScopeProvisioning(serverConn, scopeDesc); // Create Provision From All Tables

                if (!serverProvision.ScopeExists(Program.sScope))
                    serverProvision.Apply();
            }
            catch
            {
                // Shut down database connections.
                serverConn.Close();
                serverConn.Dispose();
            }
        }

        static void Program_ApplyChangeFailed(object sender, DbApplyChangeFailedEventArgs e)
        {
            Console.WriteLine(e.Conflict.Type);
            Console.WriteLine(e.Error);
        }

        public static void Sync()
        {
            try
            {
                SyncOrchestrator syncOrchestrator = new SyncOrchestrator();

                //syncOrchestrator.LocalProvider = new SqlSyncProvider(sScope, clientConn);
                //syncOrchestrator.RemoteProvider = new SqlSyncProvider(sScope, serverConn);
                //syncOrchestrator.Direction = SyncDirectionOrder.Upload;


                // Create provider for SQL Server
                SqlSyncProvider serverProvider = new SqlSyncProvider(Program.sScope, serverConn);

                // Set the command timeout and maximum transaction size for the SQL Azure provider.
                SqlSyncProvider clientProvider = new SqlSyncProvider(Program.sScope, clientConn);

                // Set Local provider of SyncOrchestrator to the server provider
                syncOrchestrator.LocalProvider = serverProvider;

                // Set Remote provider of SyncOrchestrator to the client provider
                syncOrchestrator.RemoteProvider = clientProvider;

                // Set the direction of SyncOrchestrator session to Upload and Download
                syncOrchestrator.Direction = SyncDirectionOrder.UploadAndDownload;

                ((SqlSyncProvider)syncOrchestrator.LocalProvider).ApplyChangeFailed += new EventHandler<DbApplyChangeFailedEventArgs>(Program_ApplyChangeFailed);

                // Create SyncOperations Statistics Object
                SyncOperationStatistics syncStats = syncOrchestrator.Synchronize();

                Console.WriteLine("Start Time: " + syncStats.SyncStartTime);
                Console.WriteLine("Total Changes Uploaded: " + syncStats.UploadChangesTotal);
                Console.WriteLine("Complete Time: " + syncStats.SyncEndTime);
                Console.WriteLine(String.Empty);
                Console.ReadLine();

                // Shut down database connections.
                serverConn.Close();
                serverConn.Dispose();
                clientConn.Close();
                clientConn.Dispose();
            }
            catch
            {
                // Shut down database connections.
                serverConn.Close();
                serverConn.Dispose();
                clientConn.Close();
                clientConn.Dispose();
            }
        }
        static void Main(string[] args)
        {
            ProvisionClient();
            ProvisionServer();
            Sync();
        }
    }
}