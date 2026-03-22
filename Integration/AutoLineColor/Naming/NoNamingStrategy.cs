namespace AutoLineColor.Naming
{
    internal class NoNamingStrategy : INamingStrategy
    {
        public string GetName(in TransportLine transportLine, ushort lineId = 0)
        {
            return null;
        }
    }
}