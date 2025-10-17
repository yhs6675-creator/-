// Systems/LVRogueRegistry.cs
// Rogue 변환 대상 타입 저장/조회

namespace CLVCompat.Systems
{
    public static class LVRogueRegistry
    {
        private static readonly System.Collections.Generic.HashSet<int> _rogueSet = new();

        public static void Register(int itemType)
        {
            if (itemType > 0) _rogueSet.Add(itemType);
        }

        public static void Unregister(int itemType)
        {
            if (itemType > 0) _rogueSet.Remove(itemType);
        }

        public static bool IsRegistered(int itemType)
        {
            return itemType > 0 && _rogueSet.Contains(itemType);
        }
    }
}
