using System;

namespace Code.Shared
{ 
    public static class NetworkGeneral
    {
        public static readonly int PacketTypesCount = Enum.GetValues(typeof(PacketType)).Length;
    }
}