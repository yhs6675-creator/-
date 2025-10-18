using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using CLVCompat;

namespace CLVCompat.Systems
{
    /// <summary>
    /// 칼라미티 Rogue 스텔스와의 상호 운용을 담당.
    /// Mod.Call, 리플렉션, 폴백 순으로 시도하여 스텔스 수치와 소비, 보정 곡선을 제공한다.
    /// </summary>
    internal static class RogueStealthBridge
    {
        private const float FallbackNormalDrainRatio = 0.12f;
        private const float FallbackStrikeDrainRatio = 1f;
        private const float FallbackDamageBonusMin = 1f;
        private const float FallbackDamageBonusMax = 1.25f;

        private static readonly string[] GetStealthCallNames = { "GetStealth", "GetCurrentStealth" };
        private static readonly string[] GetStealthMaxCallNames = { "GetMaxStealth", "GetStealthCap" };
        private static readonly string[] StrikeReadyCallNames = { "StealthStrikeAvailable", "CanStealthStrike" };
        private static readonly string[] ConsumeCallNames = { "ConsumeStealth", "ConsumeStealthByAttacking", "UseStealth" };
        private static readonly string[] RogueDamageMultCallNames = { "GetRogueDamageMultiplier", "GetRogueDamageMult" };
        private static readonly string[] NotifyStrikeCallNames = { "NotifyStealthStrikeFired", "OnStealthStrike" };

        private static readonly string[] CalamityPlayerTypeNames =
        {
            "CalamityMod.CalPlayer.CalamityPlayer",
            "CalamityMod.CalamityPlayer"
        };

        private static readonly string[] StealthFieldCandidates = { "stealth", "rogueStealth", "currentStealth" };
        private static readonly string[] MaxStealthFieldCandidates = { "maxStealth", "rogueStealthMax", "stealthMax" };
        private static readonly string[] StrikeReadyFieldCandidates = { "stealthStrikeReady", "stealthStrikeEnabled", "rogueStealthStrikeReady" };
        private static readonly string[] StrikeReadyMethodCandidates = { "StealthStrikeAvailable", "CanStealthStrike" };
        private static readonly string[] ConsumeMethodCandidates = { "ConsumeStealthByAttacking", "ConsumeStealth", "UseStealth" };

        private static readonly float[] FallbackCurrent = new float[Main.maxPlayers];
        private static readonly float[] FallbackMax = new float[Main.maxPlayers];
        private static readonly bool[] FallbackInitialized = new bool[Main.maxPlayers];

        private static bool loggedCallStealthSuccess;
        private static bool loggedCallStealthFailure;
        private static bool loggedCallStealthMaxSuccess;
        private static bool loggedCallStealthMaxFailure;
        private static bool loggedCallStrikeSuccess;
        private static bool loggedCallStrikeFailure;
        private static bool loggedCallConsumeSuccess;
        private static bool loggedCallConsumeFailure;
        private static bool loggedReflectionStealthSuccess;
        private static bool loggedReflectionStealthFailure;
        private static bool loggedFallbackStealth;
        private static bool loggedFallbackConsume;

        private static Mod cachedCalamity;
        private static bool calamityChecked;

        private static ReflectionCache reflection;
        private static bool reflectionChecked;

        private static Mod Calamity
        {
            get
            {
                if (!calamityChecked)
                {
                    calamityChecked = true;
                    ModLoader.TryGetMod("CalamityMod", out cachedCalamity);
                }

                return cachedCalamity;
            }
        }

        private static void LogInfoOnce(ref bool flag, string message)
        {
            if (flag)
                return;

            flag = true;
            ModContent.GetInstance<CalamityLunarVeilCompat>().Logger.Info(message);
        }

        private static void LogWarnOnce(ref bool flag, string message)
        {
            if (flag)
                return;

            flag = true;
            ModContent.GetInstance<CalamityLunarVeilCompat>().Logger.Warn(message);
        }

        public static bool TryGetStealth(Player player, out float current, out float max)
        {
            if (player == null)
            {
                current = 0f;
                max = 0f;
                return false;
            }

            bool callCurSuccess = TryCallGetStealthCurrent(player, out var callCur);
            bool callMaxSuccess = TryCallGetStealthMax(player, out var callMax);

            if (callCurSuccess && callMaxSuccess)
            {
                current = Sanitize(callCur);
                max = Sanitize(callMax);
                UpdateFallback(player, current, max);
                return true;
            }

            if (TryReflectGetStealth(player, out current, out max))
            {
                current = Sanitize(current);
                max = Sanitize(max);
                UpdateFallback(player, current, max);
                return true;
            }

            if (TryFallbackGetStealth(player, out current, out max))
            {
                LogWarnOnce(ref loggedFallbackStealth, "[LVCompat] RogueStealthBridge falling back to cached stealth values.");
                return true;
            }

            current = 0f;
            max = 0f;
            return false;
        }

        public static bool TrySetStealth(Player player, float value)
        {
            if (player == null)
                return false;

            value = Sanitize(value);

            if (TryReflectSetStealth(player, value))
            {
                if (TryGetStealth(player, out var curAfterReflect, out var maxAfterReflect))
                    UpdateFallback(player, curAfterReflect, maxAfterReflect);
                else
                    UpdateFallback(player, value, Math.Max(value, 1f));
                return true;
            }

            if (TryFallbackGetStealth(player, out _, out var fallbackMax))
            {
                value = MathHelper.Clamp(value, 0f, Math.Max(fallbackMax, 1f));
                SetFallback(player, value, Math.Max(fallbackMax, 1f));
                return true;
            }

            return false;
        }

        public static bool IsStrikeReady(Player player)
        {
            if (player == null)
                return false;

            if (TryCallStrikeReady(player, out var ready))
                return ready;

            if (TryReflectStrikeReady(player, out ready))
                return ready;

            if (TryFallbackGetStealth(player, out var cur, out var max) && max > 0f)
                return cur >= max - 1e-3f;

            return false;
        }

        public static float ConsumeNormalThrow(Player player)
        {
            return ConsumeForAttack(player, strike: false);
        }

        public static float ConsumeStrike(Player player)
        {
            return ConsumeForAttack(player, strike: true);
        }

        public static float ConsumeForAttack(Player player, bool strike)
        {
            if (player == null)
                return 0f;

            TryFallbackGetStealth(player, out var beforeCur, out var beforeMax);

            if (TryCallConsume(player, out var consumed))
            {
                bool updated = TryGetStealth(player, out var afterCur, out var afterMax);
                if (updated)
                {
                    if (consumed <= 0f)
                        consumed = Math.Max(0f, Sanitize(beforeCur) - afterCur);
                    return consumed;
                }

                if (consumed <= 0f)
                {
                    var ratio = strike ? FallbackStrikeDrainRatio : FallbackNormalDrainRatio;
                    consumed = MathHelper.Clamp(beforeMax * ratio, 0f, beforeCur);
                }

                AdjustFallbackDelta(player, consumed, strike);
                return consumed;
            }

            if (TryReflectConsume(player, strike, beforeCur, out var reflectConsumed))
                return reflectConsumed;

            if (TryFallbackGetStealth(player, out var cur, out var max))
            {
                LogWarnOnce(ref loggedFallbackConsume, "[LVCompat] RogueStealthBridge consuming stealth via cached fallback.");
                var drain = MathHelper.Clamp(max * (strike ? FallbackStrikeDrainRatio : FallbackNormalDrainRatio), 0f, cur);
                SetFallback(player, cur - drain, max);
                return drain;
            }

            return 0f;
        }

        public static bool TryGetRogueMult(Player player, out float mult)
        {
            mult = 1f;
            if (player == null)
                return false;

            if (TryCallRogueMult(player, out mult))
            {
                mult = Sanitize(mult);
                return true;
            }

            if (TryReflectRogueMult(player, out mult))
            {
                mult = Sanitize(mult);
                return true;
            }

            if (TryFallbackGetStealth(player, out var cur, out var max) && max > 0f)
            {
                mult = EvalFallbackRogueMult(MathHelper.Clamp(cur / max, 0f, 1f));
                return true;
            }

            return false;
        }

        public static float EvalFallbackRogueMult(float ratio)
        {
            ratio = MathHelper.Clamp(ratio, 0f, 1f);
            return FallbackDamageBonusMin + (FallbackDamageBonusMax - FallbackDamageBonusMin) * ratio;
        }

        public static void NotifyStealthStrikeFired(Player player, Projectile projectile)
        {
            var calamity = Calamity;
            if (calamity == null)
                return;

            foreach (var callName in NotifyStrikeCallNames)
            {
                try
                {
                    calamity.Call(callName, player, projectile);
                    return;
                }
                catch
                {
                }
            }
        }

        // ───────── Mod.Call helpers ─────────

        private static bool TryCallGetStealthCurrent(Player player, out float current)
        {
            current = 0f;
            var calamity = Calamity;
            if (calamity == null)
                return false;

            foreach (var callName in GetStealthCallNames)
            {
                try
                {
                    var ret = calamity.Call(callName, player);
                    if (TryUnpackNumber(ret, out current))
                    {
                        LogInfoOnce(ref loggedCallStealthSuccess, $"[LVCompat] RogueStealthBridge using Calamity Mod.Call '{callName}' for current stealth.");
                        return true;
                    }
                }
                catch
                {
                }
            }

            LogWarnOnce(ref loggedCallStealthFailure, "[LVCompat] RogueStealthBridge could not read current stealth via Calamity Mod.Call.");
            return false;
        }

        private static bool TryCallGetStealthMax(Player player, out float max)
        {
            max = 0f;
            var calamity = Calamity;
            if (calamity == null)
                return false;

            foreach (var callName in GetStealthMaxCallNames)
            {
                try
                {
                    var ret = calamity.Call(callName, player);
                    if (TryUnpackNumber(ret, out max))
                    {
                        LogInfoOnce(ref loggedCallStealthMaxSuccess, $"[LVCompat] RogueStealthBridge using Calamity Mod.Call '{callName}' for max stealth.");
                        return true;
                    }
                }
                catch
                {
                }
            }

            LogWarnOnce(ref loggedCallStealthMaxFailure, "[LVCompat] RogueStealthBridge could not read max stealth via Calamity Mod.Call.");
            return false;
        }

        private static bool TryCallStrikeReady(Player player, out bool ready)
        {
            ready = false;
            var calamity = Calamity;
            if (calamity == null)
                return false;

            foreach (var callName in StrikeReadyCallNames)
            {
                try
                {
                    var ret = calamity.Call(callName, player);
                    if (ret is bool b)
                    {
                        ready = b;
                        LogInfoOnce(ref loggedCallStrikeSuccess, $"[LVCompat] RogueStealthBridge using Calamity Mod.Call '{callName}' for stealth strike availability.");
                        return true;
                    }
                }
                catch
                {
                }
            }

            LogWarnOnce(ref loggedCallStrikeFailure, "[LVCompat] RogueStealthBridge could not query stealth strike availability via Calamity Mod.Call.");
            return false;
        }

        private static bool TryCallConsume(Player player, out float consumed)
        {
            consumed = 0f;
            var calamity = Calamity;
            if (calamity == null)
                return false;

            foreach (var callName in ConsumeCallNames)
            {
                try
                {
                    var ret = calamity.Call(callName, player);
                    if (TryUnpackNumber(ret, out consumed))
                    {
                        LogInfoOnce(ref loggedCallConsumeSuccess, $"[LVCompat] RogueStealthBridge using Calamity Mod.Call '{callName}' for stealth consumption.");
                        return true;
                    }

                    if (ret is bool && TryFallbackGetStealth(player, out var cur, out var max))
                    {
                        consumed = MathHelper.Clamp(max - cur, 0f, max);
                        LogInfoOnce(ref loggedCallConsumeSuccess, $"[LVCompat] RogueStealthBridge using Calamity Mod.Call '{callName}' for stealth consumption.");
                        return true;
                    }
                }
                catch
                {
                }
            }

            LogWarnOnce(ref loggedCallConsumeFailure, "[LVCompat] RogueStealthBridge could not consume stealth via Calamity Mod.Call.");
            return false;
        }

        private static bool TryCallRogueMult(Player player, out float mult)
        {
            mult = 1f;
            var calamity = Calamity;
            if (calamity == null)
                return false;

            foreach (var callName in RogueDamageMultCallNames)
            {
                try
                {
                    var ret = calamity.Call(callName, player);
                    if (TryUnpackNumber(ret, out mult))
                        return true;
                }
                catch
                {
                }
            }

            return false;
        }

        private static bool TryUnpackNumber(object value, out float number)
        {
            switch (value)
            {
                case float f:
                    number = f;
                    return true;
                case double d:
                    number = (float)d;
                    return true;
                case int i:
                    number = i;
                    return true;
                case null:
                    number = 0f;
                    return false;
            }

            number = 0f;
            return false;
        }

        // ───────── Reflection helpers ─────────

        private static bool TryReflectGetStealth(Player player, out float current, out float max)
        {
            current = 0f;
            max = 0f;
            if (!TryResolveReflection(player, out var cache, out var instance))
            {
                LogWarnOnce(ref loggedReflectionStealthFailure, "[LVCompat] RogueStealthBridge could not resolve Calamity reflection for stealth values.");
                return false;
            }

            current = Sanitize(cache.ReadStealth(instance));
            max = Sanitize(cache.ReadMaxStealth(instance));
            LogInfoOnce(ref loggedReflectionStealthSuccess, "[LVCompat] RogueStealthBridge using Calamity reflection for stealth values.");
            return true;
        }

        private static bool TryReflectSetStealth(Player player, float value)
        {
            if (!TryResolveReflection(player, out var cache, out var instance))
                return false;

            var max = Sanitize(cache.ReadMaxStealth(instance));
            value = MathHelper.Clamp(value, 0f, Math.Max(max, 1f));
            cache.WriteStealth(instance, value);
            return true;
        }

        private static bool TryReflectStrikeReady(Player player, out bool ready)
        {
            ready = false;
            if (!TryResolveReflection(player, out var cache, out var instance))
                return false;

            if (cache.InvokeStrikeReady(instance, out ready))
                return true;

            if (cache.StrikeReadyField != null)
            {
                ready = cache.ReadStrikeReady(instance);
                return true;
            }

            var max = Sanitize(cache.ReadMaxStealth(instance));
            if (max > 0f)
            {
                ready = Sanitize(cache.ReadStealth(instance)) >= max - 1e-3f;
                return true;
            }

            return false;
        }

        private static bool TryReflectConsume(Player player, bool strike, float beforeCur, out float consumed)
        {
            consumed = 0f;
            if (!TryResolveReflection(player, out var cache, out var instance))
                return false;

            if (cache.ConsumeMethod != null)
            {
                try
                {
                    var ret = cache.InvokeConsume(instance);
                    if (!TryUnpackNumber(ret, out consumed))
                        consumed = 0f;
                }
                catch
                {
                    consumed = 0f;
                }

                var afterCur = Sanitize(cache.ReadStealth(instance));
                var afterMax = Sanitize(cache.ReadMaxStealth(instance));
                UpdateFallback(player, afterCur, afterMax);

                if (consumed <= 0f)
                    consumed = Math.Max(0f, Sanitize(beforeCur) - afterCur);

                return true;
            }

            var cur = Sanitize(cache.ReadStealth(instance));
            var max = Sanitize(cache.ReadMaxStealth(instance));
            var ratio = strike ? FallbackStrikeDrainRatio : FallbackNormalDrainRatio;
            var drain = MathHelper.Clamp(max * ratio, 0f, cur);
            cache.WriteStealth(instance, cur - drain);
            cache.WriteStrikeReady(instance, false);
            UpdateFallback(player, cur - drain, max);
            consumed = drain;
            return true;
        }

        private static bool TryReflectRogueMult(Player player, out float mult)
        {
            mult = 1f;
            if (!TryResolveReflection(player, out var cache, out var instance))
                return false;

            var max = Sanitize(cache.ReadMaxStealth(instance));
            if (max <= 0f)
                return false;

            var cur = Sanitize(cache.ReadStealth(instance));
            mult = EvalFallbackRogueMult(MathHelper.Clamp(cur / max, 0f, 1f));
            return true;
        }

        private static bool TryResolveReflection(Player player, out ReflectionCache cache, out object instance)
        {
            cache = null;
            instance = null;

            if (!reflectionChecked)
            {
                reflectionChecked = true;
                reflection = ReflectionCache.Create();
            }

            if (reflection == null)
                return false;

            instance = reflection.FetchCalamityPlayer(player);
            if (instance == null)
                return false;

            cache = reflection;
            return true;
        }

        private sealed class ReflectionCache
        {
            public Type PlayerType { get; private set; }
            public FieldInfo StealthField { get; private set; }
            public FieldInfo MaxStealthField { get; private set; }
            public FieldInfo StrikeReadyField { get; private set; }
            public MethodInfo StrikeReadyMethod { get; private set; }
            public MethodInfo ConsumeMethod { get; private set; }
            private MethodInfo getModPlayerGeneric;

            private ReflectionCache()
            {
            }

            public static ReflectionCache Create()
            {
                var calamity = Calamity;
                if (calamity?.Code == null)
                    return null;

                foreach (var name in CalamityPlayerTypeNames)
                {
                    var type = calamity.Code.GetType(name) ?? Type.GetType(name);
                    if (type == null)
                        type = Type.GetType($"{name}, CalamityMod") ?? Type.GetType($"{name}, CalamityModPublic");

                    if (type == null)
                        continue;

                    var cache = new ReflectionCache { PlayerType = type };
                    cache.StealthField = cache.FindField(type, StealthFieldCandidates);
                    cache.MaxStealthField = cache.FindField(type, MaxStealthFieldCandidates);
                    cache.StrikeReadyField = cache.FindField(type, StrikeReadyFieldCandidates);
                    cache.StrikeReadyMethod = cache.FindMethod(type, StrikeReadyMethodCandidates);
                    cache.ConsumeMethod = cache.FindMethod(type, ConsumeMethodCandidates);
                    cache.getModPlayerGeneric = FindGenericGetter();

                    if (cache.StealthField != null && cache.getModPlayerGeneric != null)
                        return cache;
                }

                return null;
            }

            private static MethodInfo FindGenericGetter()
            {
                foreach (var method in typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!method.IsGenericMethodDefinition)
                        continue;

                    if (method.Name != "GetModPlayer")
                        continue;

                    if (method.GetParameters().Length != 0)
                        continue;

                    return method;
                }

                return null;
            }

            private FieldInfo FindField(Type type, IEnumerable<string> candidates)
            {
                foreach (var name in candidates)
                {
                    var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null)
                        return field;
                }

                return null;
            }

            private MethodInfo FindMethod(Type type, IEnumerable<string> candidates)
            {
                foreach (var name in candidates)
                {
                    var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (method != null)
                        return method;
                }

                return null;
            }

            public object FetchCalamityPlayer(Player player)
            {
                if (PlayerType == null || getModPlayerGeneric == null)
                    return null;

                try
                {
                    var method = getModPlayerGeneric.MakeGenericMethod(PlayerType);
                    return method.Invoke(player, Array.Empty<object>());
                }
                catch
                {
                    return null;
                }
            }

            public float ReadStealth(object instance)
            {
                return Convert.ToSingle(StealthField?.GetValue(instance) ?? 0f);
            }

            public float ReadMaxStealth(object instance)
            {
                if (MaxStealthField == null)
                    return 0f;

                return Convert.ToSingle(MaxStealthField.GetValue(instance));
            }

            public bool ReadStrikeReady(object instance)
            {
                if (StrikeReadyField == null)
                    return false;

                return Convert.ToBoolean(StrikeReadyField.GetValue(instance));
            }

            public bool InvokeStrikeReady(object instance, out bool ready)
            {
                ready = false;
                if (StrikeReadyMethod == null)
                    return false;

                try
                {
                    var ret = StrikeReadyMethod.Invoke(instance, Array.Empty<object>());
                    if (ret is bool b)
                    {
                        ready = b;
                        return true;
                    }
                }
                catch
                {
                }

                return false;
            }

            public object InvokeConsume(object instance)
            {
                if (ConsumeMethod == null)
                    return null;

                try
                {
                    return ConsumeMethod.Invoke(instance, Array.Empty<object>());
                }
                catch
                {
                    return null;
                }
            }

            public void WriteStealth(object instance, float value)
            {
                StealthField?.SetValue(instance, value);
            }

            public void WriteStrikeReady(object instance, bool value)
            {
                StrikeReadyField?.SetValue(instance, value);
            }
        }

        // ───────── Fallback storage ─────────

        private static bool TryFallbackGetStealth(Player player, out float current, out float max)
        {
            int idx = player.whoAmI;
            if ((uint)idx >= FallbackCurrent.Length)
            {
                current = 0f;
                max = 0f;
                return false;
            }

            if (!FallbackInitialized[idx])
            {
                FallbackInitialized[idx] = true;
                FallbackCurrent[idx] = 1f;
                FallbackMax[idx] = 1f;
            }

            current = FallbackCurrent[idx];
            max = Math.Max(FallbackMax[idx], 1f);
            return true;
        }

        private static void UpdateFallback(Player player, float current, float max)
        {
            int idx = player.whoAmI;
            if ((uint)idx >= FallbackCurrent.Length)
                return;

            FallbackInitialized[idx] = true;
            current = MathHelper.Clamp(Sanitize(current), 0f, Math.Max(Sanitize(max), 0f));
            max = Math.Max(Sanitize(max), 1f);
            FallbackCurrent[idx] = current;
            FallbackMax[idx] = max;
        }

        private static void SetFallback(Player player, float current, float max)
        {
            int idx = player.whoAmI;
            if ((uint)idx >= FallbackCurrent.Length)
                return;

            FallbackInitialized[idx] = true;
            current = MathHelper.Clamp(Sanitize(current), 0f, Math.Max(Sanitize(max), 0f));
            max = Math.Max(Sanitize(max), 1f);
            FallbackCurrent[idx] = current;
            FallbackMax[idx] = max;
        }

        private static void AdjustFallbackDelta(Player player, float consumed, bool strike)
        {
            if (!TryFallbackGetStealth(player, out var cur, out var max))
                return;

            cur = MathHelper.Clamp(cur - Math.Max(Sanitize(consumed), 0f), 0f, max);
            if (strike)
                cur = Math.Max(cur, 0f);
            SetFallback(player, cur, max);
        }

        private static float Sanitize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            return value;
        }
    }
}
