using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;


using System.Threading.Tasks;

namespace DB_projectWPF
{
    public class Status
    {
        String status;

        public Status(String description)
        {
            status = description;
        }

        public string description { get => status; set => status = value; }
    }
    public static class Statuses
    {
        public static Status
            UserAlreadyExist = new Status("UserAlreadyExist"),
            Success = new Status("Success"),
            GeneralError = new Status("Some Shit Happens"),
            IncorrectPass = new Status("Incorrect pass typed"),
            IncorrectDataInserted = new Status("Incorrect Data Inserted"),
            NotLoggined = new Status("User not loggined"),
            IncorrectUser = new Status("You cant set as owner another user"),
            UserDoesNotExist = new Status("User Does Not Exist");
    };

    class Model
    {
        private IDB_wrapper db_wrapper;

        public Model(IDB_wrapper wrapper)
        {
            this.db_wrapper = wrapper;
        }

        private bool is_user_exist(String userName)
        {
            try
            {
                var uid = db_wrapper.FindUser(userName);
                return true;
            }
            catch (DBItemNotFound e)
            {
                return false;
            }
            catch
            {
                throw new GeneralException();
            }
        }

        private bool is_machine_exist(String machineName)
        {
            try
            {
                var uid = db_wrapper.FindMachine(machineName);
                return true;
            }
            catch (DBItemNotFound e)
            {
                return false;
            }
            catch
            {
                throw new GeneralException();
            }
        }

        public bool is_inserted_valid(String data)
        {
            if (data == null)
            {
                return false;
            }
            if (data.Length == 0)
            {
                return false;
            }
            return true;
        }

        public Status AddOrUpdateMachine(MachineInfo info)
        {
            if (!is_user_exist(info.owner))
            {
                return Statuses.UserDoesNotExist;
            }
            if (!is_machine_exist(info.name))
            {
                return AddMachine(info.name, info.mac, info.os, info.owner, info.status, info.action);
            }
            else
            {
                return UpdateMachine(info.name, info.mac, info.os, info.owner, info.status, info.action);
            }

        }

        public Status AddMachine(String machineName, String mac, String os, String owner, String status, String action)
        {
            try
            {
                db_wrapper.AddMachine(machineName, mac, os, owner, status, action);
                return Statuses.Success;
            }
            catch
            {
                return Statuses.GeneralError;
            }
            
        }

        public Status AddOs(String osName)
        {
            try
            {
                if (!is_inserted_valid(osName))
                {
                    return Statuses.IncorrectDataInserted;
                }
                db_wrapper.AddNewOs(osName);
                return Statuses.Success;
            }
            catch
            {
                return Statuses.GeneralError;
            }

        }

        public Status UpdateMachine(String machineName, String mac, String os, String owner, String status, String action)
        {
            try
            {
                db_wrapper.UpdateMachine(machineName, mac, os, owner, status, action);
                return Statuses.Success;
            }
            catch
            {
                return Statuses.GeneralError;
            }
        }

        public Status AddUser(String userName, String pw)
        {
            if (!is_inserted_valid(userName) || !is_inserted_valid(pw))
            {
                return Statuses.IncorrectDataInserted;
            }
            try
            {
                if (is_user_exist(userName))
                {
                    return Statuses.UserAlreadyExist;
                }
                
                db_wrapper.AddUser(userName, pw);
                return Statuses.Success;
            }
            catch
            {
                return Statuses.GeneralError;
            }

        }

        public Status Login(String userName, String pw)
        {
            if (!is_inserted_valid(userName) || !is_inserted_valid(pw))
            {
                return Statuses.IncorrectDataInserted;
            }
            try
            {
                if (!is_user_exist(userName))
                {
                    return Statuses.UserDoesNotExist;
                }
                String salt = PasswordHash.generateSalt();
                if (db_wrapper.VerifyUserPassword(userName, pw))
                {
                    return Statuses.Success;
                }
                else
                {
                    return Statuses.IncorrectPass;
                }
            }
            catch
            {
                return Statuses.GeneralError;
            }

        }
    }

    abstract class IDB_wrapper
    {
        public abstract void Connect(String connectionAddres);
        public abstract String FindUser(String user_name);
        public abstract String FindMachine(String machine_name);
        public abstract bool VerifyUserPassword(String user_name, String pw);
        public abstract void AddUser(String user_name, String pw);
        public abstract void AddNewOs(string os_name);
        public abstract void AddMachine(String machineName, String mac, String os, String owner, String status, String action);
        public abstract void UpdateMachine(String machineName, String mac, String os, String owner, String status, String action);
        public abstract List<Machine> getAllMachines();
        public abstract List<String> getAllAvailableOses();
        public abstract MachineInfo GetAllMachineInfo(string machineName);

    }

    public class Machine
    {
        public Machine() { }

        public string MachineName { get; set; }
        public string Status { get; set; }
        public string Ovner { get; set; }
    }

    public class DBItemNotFound : Exception { };
    public class DBFailedToInsertData : Exception { };
    public class GeneralException : Exception { };
}
