using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace DB_projectWPF
{
    public class MachineStatusTableElement
    {
        public String MachineName { get; set; }
        public String Status { get; set; }
        public String Owner { get; set; }
    }

    public class DelegateCommand : ICommand
    {
        private readonly Action<object> _executeAction;

        public DelegateCommand(Action<object> executeAction)
        {
            _executeAction = executeAction;
        }

        public void Execute(object parameter) => _executeAction(parameter);

        public bool CanExecute(object parameter) => true;

        public event EventHandler CanExecuteChanged;
    }

    public class ViewModel : INotifyPropertyChanged
    {
        private String userFieldText;
        private String passwdFieldText;
        private String statusField;
        private String addUpdateStatusField;
        private String addOsStatusField;
        private Machine selectedMachine;
        private MachineInfo selectedMachineInfo;
        private String selectedOs;
        private String newOsName;

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName]string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }


        // Data bindings
        private ObservableCollection<Machine> _machines = new ObservableCollection<Machine>();
        public ObservableCollection<Machine> Machines
        {
            get => _machines;
            set => SetProperty(ref _machines, value);
        }

        private ObservableCollection<String> _oses = new ObservableCollection<String>();
        public ObservableCollection<String> Oses
        {
            get => _oses;
            set => SetProperty(ref _oses, value);
        }

        private ObservableCollection<String> _machineStatuses = new ObservableCollection<String>();
        public ObservableCollection<String> MachineStatuses
        {
            get => _machineStatuses;
            set => SetProperty(ref _machineStatuses, value);
        }

        private ObservableCollection<String> _machineActions = new ObservableCollection<String>();
        public ObservableCollection<String> MachineActions
        {
            get => _machineActions;
            set => SetProperty(ref _machineActions, value);
        }

        public String UserFieldText
        {
            get => userFieldText;
            set => SetProperty(ref userFieldText, value);
        }

        public String NewOsName
        {
            get => newOsName;
            set => SetProperty(ref newOsName, value);
        }

        public Machine SelectedMachine
        {
            get => selectedMachine;
            set => SetProperty(ref selectedMachine, value);
        }

        public MachineInfo SelectedMachineInfo
        {
            get => selectedMachineInfo;
            set => SetProperty(ref selectedMachineInfo, value);
        }

        public String SelectedOs
        {
            get => selectedOs;
            set => SetProperty(ref selectedOs, value);
        }

        public String PasswdFieldText
        {
            get => passwdFieldText;
            set => SetProperty(ref passwdFieldText, value);
        }

        public String StatusField
        {
            get => statusField;
            set => SetProperty(ref statusField, value);
        }

        public String AddUpdateStatusField
        {
            get => addUpdateStatusField;
            set => SetProperty(ref addUpdateStatusField, value);
        }

        public String AddOsStatusField
        {
            get => addOsStatusField;
            set => SetProperty(ref addOsStatusField, value);
        }

        // Actions binding
        private readonly DelegateCommand _addUserCommand;
        public ICommand AddUserCommand => _addUserCommand;

        private readonly DelegateCommand _loginCommand;
        public ICommand LoginCommand => _loginCommand;

        private readonly DelegateCommand _addUpdateCommand;
        public ICommand AddUpdateCommand => _addUpdateCommand;

        private readonly DelegateCommand _addNewOsCommand;
        public ICommand AddNewOsCommand => _addNewOsCommand;


        private Model model;
        bool login_status;
        String currntUserName;
        public ViewModel()
        {
            wrapper = new MongoDBWrapper();
            wrapper.Connect("mongodb://127.0.0.1:27017");
            this.model = new Model(wrapper);
            userFieldText = "admin";
            statusField = "Waiting For Action";
            login_status = false;

            _addUserCommand = new DelegateCommand(addUser);
            _loginCommand = new DelegateCommand(login);
            _addUpdateCommand = new DelegateCommand(addOrUpdate);
            _addNewOsCommand = new DelegateCommand(addNewOs);
        }

        public void addUser(object commandParameter)
        {
            StatusField = model.AddUser(userFieldText, passwdFieldText).description;
        }

        public void addNewOs(object commandParameter)
        {
            if (login_status)
            {
                var status = model.AddOs(NewOsName);
                AddOsStatusField = status.description;
                if (status == Statuses.Success)
                {
                    var tmpStorage = SelectedMachineInfo.os;
                    fillOses();
                    SelectedMachineInfo.os = tmpStorage;
                }
            }
        }

        public void addOrUpdate(object commandParameter)
        {
            if (login_status)
            {
                var status = model.AddOrUpdateMachine(SelectedMachineInfo);
                AddUpdateStatusField = status.description;
                if (status == Statuses.Success)
                {
                    fillTable();
                }

            }
            else
            {
                AddUpdateStatusField = Statuses.NotLoggined.description;
            }

        }

        public void login(object commandParameter)
        {
            var status = model.Login(userFieldText, passwdFieldText);
            StatusField = status.description;
            if (status == Statuses.Success)
            {
                login_status = true;
                currntUserName = UserFieldText;
                afterLoginInit();
            }
        }

        public void afterLoginInit()
        {
            fillTable();
            fillOses();
            fillStatuses();
            fillActions();
        }

        private IDB_wrapper wrapper;

        internal IDB_wrapper Wrapper { get => wrapper; set => wrapper = value; }

        public void fillTable()
        {
            var tmp = GetAllMachines();
            Machines.Clear();
            foreach (var item in tmp)
            {
                Machines.Add(item);
            }

        }

        public void fillOses()
        {
            var tmp = wrapper.getAllAvailableOses();
            Oses.Clear();
            foreach (var item in tmp)
            {
                Oses.Add(item);

            }
            if (SelectedMachineInfo != null)
            {
                SelectedMachineInfo = wrapper.GetAllMachineInfo(SelectedMachineInfo.name);
            }
        }

        public void fillActions()
        {
            MachineActions.Add("boot");
            MachineActions.Add("install");
            MachineActions.Add("restore");
        }

        public void fillStatuses()
        {
            MachineStatuses.Add("Free");
            MachineStatuses.Add("Disabled");
            MachineStatuses.Add("Offline");
        }

        public List<Machine> GetAllMachines()
        {
            return wrapper.getAllMachines();
        }

        public void OpenSelectedMachine()
        {
            if (login_status)
            {
                if (SelectedMachine.MachineName != null)
                {
                    SelectedMachineInfo = wrapper.GetAllMachineInfo(SelectedMachine.MachineName);
                }
            }
            else
            {
                StatusField = Statuses.NotLoggined.description;
            }
        }


    }

}
