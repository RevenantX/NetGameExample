using System;

namespace Code.Shared
{ 
    public static class NetworkGeneral
    {
        public const int ProtocolId = 1;
        public static readonly int PacketTypesCount = Enum.GetValues(typeof(PacketType)).Length;

        public const int MaxGameSequence = 512;

        public static int SeqDiff(int a, int b)
        {
            return (a - b) % MaxGameSequence;
        }
    }
}
