using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;
//using MongoDB.Driver.Builders;
//using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using System.Security.Cryptography;
using System.IO;
using MongoDB.Bson.Serialization.Attributes;

namespace DB_projectWPF
{
    public class User
    {
        public ObjectId Id { get; set; }
        public string name { get; set; }
        public string pwd { get; set; }
        public string salt { get; set; }
    }

    public class Host
    {
        public ObjectId Id { get; set; }
        public string mac { get; set; }
        public string name { get; set; }
        public ObjectId os { get; set; }
        public string action { get; set; }

    }

    public class Oses
    {
        public ObjectId Id { get; set; }
        public string os_name { get; set; }

    }
    [BsonIgnoreExtraElements]
    public class MachineStatus
    {
        public ObjectId Id { get; set; }
        public ObjectId hostid { get; set; }
        public ObjectId userid { get; set; }
        public string comment { get; set; }
        public string status { get; set; }
    }

    public class PasswordHash
    {
        private static String superStrongSalt = "OLOLO";
        public static String Compute(String pw, String salt)
        {
            String value_to_hash = salt + superStrongSalt + pw;
            HashAlgorithm hash = new SHA512Managed();
            byte[] value_to_hash_in_bytes = Encoding.UTF8.GetBytes(value_to_hash);
            byte[] hashBytes = hash.ComputeHash(value_to_hash_in_bytes);
            for (var i = 0; i < 32; i++)
            {
                hashBytes = hash.ComputeHash(hashBytes);
            }
            String hashValue = Convert.ToBase64String(hashBytes);
            return hashValue;
        }

        public static bool verifyPassword(String pw, String salt, String expectedHash)
        {
            String hashValue = Compute(pw, salt);
            return (hashValue == expectedHash);
        }

        public static String generateSalt()
        {
            string path = Path.GetRandomFileName();
            path = path.Replace(".", ""); // Remove period.
            return path;
        }
    }


    class MongoDBWrapper : IDB_wrapper
    {
        private MongoClient client;
        private IMongoDatabase db;

        private IMongoCollection<User> GetUsersCollection()
        {
            return db.GetCollection<User>("test_users");
        }

        private IMongoCollection<MachineStatus> GetMacinesStatusCollection()
        {
            return db.GetCollection<MachineStatus>("machine_status");
        }

        private IMongoCollection<Host> GetHostsCollection()
        {
            return db.GetCollection<Host>("hosts");
        }

        private IMongoCollection<Oses> GetOsesCollection()
        {
            return db.GetCollection<Oses>("oses");
        }

        public override void AddUser(string user_name, string pw)
        {
            try
            {
                String salt = PasswordHash.generateSalt();
                var user_table = GetUsersCollection();
                User user = new User
                {
                    Id = ObjectId.GenerateNewId(),
                    name = user_name,
                    pwd = PasswordHash.Compute(pw, salt),
                    salt = salt
                };
                user_table.InsertOne(user);

            }
            catch
            {
                throw new DBFailedToInsertData();
            }

        }

        public override void AddNewOs(string os_name)
        {
            try
            {
                String salt = PasswordHash.generateSalt();
                var user_table = GetOsesCollection();
                Oses os = new Oses
                {
                    Id = ObjectId.GenerateNewId(),
                    os_name = os_name
                };
                user_table.InsertOne(os);

            }
            catch
            {
                throw new DBFailedToInsertData();
            }

        }

        public override void AddMachine(String machineName, String mac, String os, String owner, String status, String action)
        {
            try
            {
                var id = fillHostTable(mac, machineName, findOsesByName(os).Id, action);
                fillStatusTable(id, findUser(owner).Id, status);
            }
            catch
            {
                throw new DBFailedToInsertData();
            }

        }

        public override void UpdateMachine(String machineName, String mac, String os, String owner, String status, String action)
        {
            try
            {
                var host_id = findHostInHostsByName(machineName).Id;
                var status_id = findMachineInMachineStatusByHostId(host_id).Id;
                updateHostTable(host_id, mac, machineName, findOsesByName(os).Id, action);
                updateStatusTable(status_id, host_id, findUser(owner).Id, status);
            }
            catch
            {
                throw new DBFailedToInsertData();
            }

        }

        public override List<Machine> getAllMachines()
        {
            var user_table = GetUsersCollection();
            var hosts_table = GetHostsCollection();
            var status_table = GetMacinesStatusCollection();
            try
            {
                List<Machine> resultMachinesStatusList = new List<Machine>();
                var status_table_list = status_table.Find(_ => true).ToList();

                foreach (var machine_status in status_table_list)
                {
                    Machine machine = new Machine();
                    machine.MachineName = findHostInHosts(machine_status.hostid).name;
                    machine.Status = machine_status.status;
                    machine.Ovner = findUserById(machine_status.userid).name;
                    resultMachinesStatusList.Add(machine);
                }
                return resultMachinesStatusList;
            }
            catch
            {
                throw new DBItemNotFound();
            }

        }

        public override List<String> getAllAvailableOses()
        {
            List<String> oses_list = new List<String>();
            var oses_table = GetOsesCollection();
            var oses_table_list = oses_table.Find(_ => true).ToList();
            foreach (var os in oses_table_list)
            {
                oses_list.Add(os.os_name);
            }
            return oses_list;
        }

        public override String FindUser(string user_name)
        {
            return findUser(user_name).Id.ToString();
        }

        public override String FindMachine(string machine_name)
        {
            return findHostInHostsByName(machine_name).Id.ToString();
        }

        public override MachineInfo GetAllMachineInfo(string machineName)
        {
            var host = findHostInHostsByName(machineName);
            var machine_status = findMachineInMachineStatusByHostId(host.Id);
            var user_name = findUserById(machine_status.userid).name;
            MachineInfo info = new MachineInfo();
            info.name = machineName;
            info.mac = host.mac;
            info.os = findOsesById(host.os).os_name;
            info.owner = user_name;
            info.status = machine_status.status;
            info.action = host.action;
            return info;
        }

        public override bool VerifyUserPassword(string user_name, string pw)
        {
            User user = findUser(user_name);
            if (PasswordHash.Compute(pw, user.salt) == user.pwd)
            {
                return true;
            }
            return false;
        }

        public override void Connect(String serverAddress)
        {
            client = new MongoClient(serverAddress);
            db = client.GetDatabase("DB_course");
        }

        private Host getHostData(String mac, String name, ObjectId os_id, String action, ObjectId id)
        {
            Host host = new Host
            {
                Id = id,
                mac = mac,
                name = name,
                os = os_id,
                action = action
            };
            return host;
        }

        private MachineStatus getStatusData(ObjectId hostid, ObjectId userid, String status, ObjectId id)
        {
            MachineStatus m_status = new MachineStatus
            {
                Id = id,
                hostid = hostid,
                userid = userid,
                comment = "",
                status = status
            };
            return m_status;
        }

        private ObjectId fillHostTable(String mac, String name, ObjectId os_id, String action)
        {
            var host_table = GetHostsCollection();
            var host = getHostData(mac, name, os_id, action, ObjectId.GenerateNewId());
            host_table.InsertOne(host);
            return host.Id;
        }
        private void fillStatusTable(ObjectId hostid, ObjectId userid, String status)
        {
            var machine_status_table = GetMacinesStatusCollection();
            var m_status = getStatusData(hostid, userid, status, ObjectId.GenerateNewId());
            machine_status_table.InsertOne(m_status);
        }

        private ObjectId updateHostTable(ObjectId host_id, String mac, String name, ObjectId os_id, String action)
        {
            var host_table = GetHostsCollection();
            var host = getHostData(mac, name, os_id, action, host_id);
            var filter = Builders<Host>.Filter.Eq("_id", host_id);
            host_table.ReplaceOne(filter, host);
            return host.Id;
        }
        private void updateStatusTable(ObjectId status_id, ObjectId hostid, ObjectId userid, String status)
        {
            var machine_status_table = GetMacinesStatusCollection();
            var m_status = getStatusData(hostid, userid, status, status_id);
            var filter = Builders<MachineStatus>.Filter.Eq("_id", status_id);
            machine_status_table.ReplaceOne(filter, m_status);
        }


        private User findUser(string user_name)
        {
            var user_table = GetUsersCollection();
            var filter = Builders<User>.Filter.Eq("name", user_name);
            try
            {
                var user = user_table.Find(filter).First();
                return user;
            }
            catch
            {
                throw new DBItemNotFound();
            }

        }

        private User findUserById(ObjectId id)
        {
            var user_table = GetUsersCollection();
            var filter = Builders<User>.Filter.Eq("_id", id);
            try
            {
                var user = user_table.Find(filter).First();
                return user;
            }
            catch
            {
                throw new DBItemNotFound();
            }

        }

        private Host findHostInHosts(ObjectId id)
        {
            var host_table = GetHostsCollection();
            var filter = Builders<Host>.Filter.Eq("_id", id);
            try
            {
                var host = host_table.Find(filter).First();
                return host;
            }
            catch
            {
                throw new DBItemNotFound();
            }
        }

        private MachineStatus findMachineInMachineStatusByHostId(ObjectId host_id)
        {
            var host_table = GetMacinesStatusCollection();
            var filter = Builders<MachineStatus>.Filter.Eq("hostid", host_id);
            try
            {
                var host = host_table.Find(filter).First();
                return host;
            }
            catch
            {
                throw new DBItemNotFound();
            }
        }

        private Host findHostInHostsByName(String name)
        {
            var host_table = GetHostsCollection();
            var filter = Builders<Host>.Filter.Eq("name", name);
            try
            {
                var host = host_table.Find(filter).First();
                return host;
            }
            catch
            {
                throw new DBItemNotFound();
            }
        }

        private Oses findOsesById(ObjectId os_id)
        {
            var host_table = GetOsesCollection();
            var filter = Builders<Oses>.Filter.Eq("_id", os_id);
            try
            {
                var host = host_table.Find(filter).First();
                return host;
            }
            catch
            {
                throw new DBItemNotFound();
            }
        }

        private Oses findOsesByName(String os_name)
        {
            var os_table = GetOsesCollection();
            var filter = Builders<Oses>.Filter.Eq("os_name", os_name);
            try
            {
                var host = os_table.Find(filter).First();
                return host;
            }
            catch
            {
                throw new DBItemNotFound();
            }
        }

    }
    public class MachineInfo
    {
        public string name { get; set; }
        public string mac { get; set; }
        public string os { get; set; }
        public string owner { get; set; }
        public string status { get; set; }
        public string action { get; set; }
    }
}
