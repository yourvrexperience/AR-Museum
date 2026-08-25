namespace yourvrexperience.Networking
{
	public interface INetworkInitialData
	{
		public string ProviderName { get; }
		public string GetInitialData();
		public void ApplyInitialData(string data, bool linkedToLevel);
	}
}