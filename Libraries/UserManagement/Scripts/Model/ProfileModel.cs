using yourvrexperience.Utils;

namespace yourvrexperience.UserManagement
{
    public class ProfileModel
    {
        private long _id;
        private long _user;
        private string _name = "";
        private string _address = "";
        private string _description = "";
        private string _data = "";
        private string _data2 = "";
        private string _data3 = "";
        private string _data4 = "";
        private string _data5 = "";
        private bool _autorun = false;

        public long Id
        {
            get { return _id; }
            set { _id = value; }
        }
        public long User
        {
            get { return _user; }
            set { _user = value; }
        }
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        public string Address
        {
            get { return _address; }
            set { _address = value; }
        }
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
        public string Data
        {
            get { return _data; }
            set { _data = value; }
        }
        public string Data2
        {
            get { return _data2; }
            set { _data2 = value; }
        }
        public string Data3
        {
            get { return _data3; }
            set { _data3 = value; }
        }
        public string Data4
        {
            get { return _data4; }
            set { _data4 = value; }
        }
        public string Data5
        {
            get { return _data5; }
            set { _data5 = value; }
        }
        public bool Autorun
        {
            get { return _autorun; }
            set { _autorun = value; }
        }

        public ProfileModel(long id, long user, string name, string address, string description, string data, string data2, string data3, string data4, string data5, bool autorun)
        {
            this._id = id;
            this._user = user;
            this._name = name;
            this._address = address;
            this._description = description;
            this._data = data;
            this._data2 = data2;
            this._data3 = data3;
            this._data4 = data4;
            this._data5 = data5;
            this._autorun = autorun;
        }

        public void Copy(ProfileModel profile)
        {
            _id = profile.Id;
            _user = profile.User;
            _name = profile.Name;
            _address = profile.Address;
            _description = profile.Description;
            _data = profile.Data;
            _data2 = profile.Data2;
            _data3 = profile.Data3;
            _data4 = profile.Data4;
            _data5 = profile.Data5;
            _autorun = profile.Autorun;
        }

        public ProfileModel Clone()
        {
            return new ProfileModel(_id, _user, _name, _address, _description, _data, _data2, _data3, _data4, _data5, _autorun);
        }

        public static string FormatPacketProfile(long idUser, long id, string name = "", string address = "", string description = "", string data = "", string data2 = "", string data3 = "", string data4 = "", string data5 = "", int autorun = 0)
        {
            return UserModel.ACCOUNT_DATA_PROFILE + CommController.TOKEN_SEPARATOR_EVENTS +
                id + CommController.TOKEN_SEPARATOR_EVENTS +
                name + CommController.TOKEN_SEPARATOR_EVENTS +
                address + CommController.TOKEN_SEPARATOR_EVENTS +
                description + CommController.TOKEN_SEPARATOR_EVENTS +
                data + CommController.TOKEN_SEPARATOR_EVENTS +
                data2 + CommController.TOKEN_SEPARATOR_EVENTS +
                data3 + CommController.TOKEN_SEPARATOR_EVENTS +
                data4 + CommController.TOKEN_SEPARATOR_EVENTS +
                data5 + CommController.TOKEN_SEPARATOR_EVENTS +
                autorun;
        }
    }
}