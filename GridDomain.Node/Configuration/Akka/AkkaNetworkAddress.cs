namespace GridDomain.Node.Configuration.Akka
{
    public class AkkaNetworkAddress : IAkkaNetworkAddress
    {
        public AkkaNetworkAddress(string systemName, string host, int port, string publicHost = null)
        {
            SystemName = systemName;
            Host = host;
            PortNumber = port;
            PublicHost = publicHost ?? Host;
        }

        public string SystemName { get; }
        public string Host { get; }
        public string PublicHost { get; }
        public int PortNumber { get; }

        public override bool Equals(object obj)
        {
            return Equals((AkkaNetworkAddress) obj);
        }

        protected bool Equals(AkkaNetworkAddress other)
        {
            return string.Equals(SystemName, other.SystemName) && string.Equals(Host, other.Host) &&
                   PortNumber == other.PortNumber;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SystemName != null ? SystemName.GetHashCode() : 0;
                hashCode = (hashCode*397) ^ (Host != null ? Host.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ PortNumber;
                return hashCode;
            }
        }
    }
}