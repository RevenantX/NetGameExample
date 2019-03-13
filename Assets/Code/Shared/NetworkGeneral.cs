using System;

namespace Code.Shared
{
    public enum PacketType : byte
    {
        Movement,
        Spawn,
        Serialized
    }
    
    public static class NetworkGeneral
    {
        public static readonly int PacketTypesCount = Enum.GetValues(typeof(PacketType)).Length;
        public const float FramesPerSecond = 60.0f;
        public const float FixedDelta = 1.0f / FramesPerSecond;
    }
}