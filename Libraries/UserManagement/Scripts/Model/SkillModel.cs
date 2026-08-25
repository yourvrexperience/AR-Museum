namespace yourvrexperience.UserManagement
{
	public class SkillModel
	{
		private string _name;
		private int _value;

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}
		public int Value
		{
			get { return _value; }
			set { _value = value; }
		}

		public SkillModel(string name, int value)
		{
			this._name = name;
			this._value = value;
		}

		public SkillModel Clone()
		{
			return new SkillModel(_name, _value);
		}

		public void Copy(SkillModel _skill)
		{
			_name = _skill.Name;
			_value = _skill.Value;
		}

		public override string ToString()
		{
			return "(" + _name + "," + _value + ")";
		}
	}
}