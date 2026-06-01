using System.Net;
using System.Net.Sockets;

namespace AgencyCampaign.Domain.ValueObjects
{
    // Anonimiza enderecos IP de visitantes para fins de LGPD: zera a parte identificadora
    // (ultimo octeto no IPv4, ultimos 80 bits no IPv6) antes de persistir o registro de acesso.
    public static class IpAnonymizer
    {
        public static string? Anonymize(string? ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip, out IPAddress? address))
            {
                return null;
            }

            byte[] bytes = address.GetAddressBytes();

            if (address.AddressFamily == AddressFamily.InterNetwork && bytes.Length == 4)
            {
                bytes[3] = 0;
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6 && bytes.Length == 16)
            {
                for (int index = 6; index < bytes.Length; index++)
                {
                    bytes[index] = 0;
                }
            }
            else
            {
                return null;
            }

            return new IPAddress(bytes).ToString();
        }
    }
}
