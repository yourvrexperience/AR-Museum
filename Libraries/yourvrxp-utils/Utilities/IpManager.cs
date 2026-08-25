using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;


namespace yourvrexperience.Utils
{
    public class IpManager
    {
        public static string GetPrefixIP(ADDRESSFAM Addfam)
        {
            string ip = GetIP(Addfam);
            int index = ip.LastIndexOf('.') + 1;
            return ip.Substring(0, index);
        }

        public static string GetExtensionIP(ADDRESSFAM Addfam)
        {
            string ip = GetIP(Addfam);
            int index = ip.LastIndexOf('.') + 1;
            return ip.Substring(index, ip.Length - index);
        }

        public static string GetIP(ADDRESSFAM Addfam)
        {
            if (Addfam == ADDRESSFAM.IPv6 && !Socket.OSSupportsIPv6)
            {
                return null;
            }

            string output = "";

            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                NetworkInterfaceType _type1 = NetworkInterfaceType.Wireless80211;
                NetworkInterfaceType _type2 = NetworkInterfaceType.Ethernet;

                if ((item.NetworkInterfaceType == _type1 || item.NetworkInterfaceType == _type2) && item.OperationalStatus == OperationalStatus.Up)
#endif
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        //IPv4
                        if (Addfam == ADDRESSFAM.IPv4)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                output = ip.Address.ToString();
                            }
                        }

                        //IPv6
                        else if (Addfam == ADDRESSFAM.IPv6)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                output = ip.Address.ToString();
                            }
                        }
                    }
                }
            }
            return output;
        }
    }

    public enum ADDRESSFAM
    {
        IPv4, IPv6
    }
}