namespace yourvrexperience.Networking
{
    public interface INetworkObject
    {
		string NameNetworkPrefab { get; }
		string NameNetworkPath { get; }
		bool LinkedToCurrentLevel { get; }
		NetworkObjectID NetworkGameIDView { get; }
		
		void SetInitData(string initializationData);
		void OnInitDataEvent(string initializationData);
		void ActivatePhysics(bool activation, bool force = false);
    }
}