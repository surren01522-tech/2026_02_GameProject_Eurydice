using UnityEngine;
using GameFramework.Core;
using GameFramework.Data;
using GameFramework.Gameplay;

namespace GameFramework.Services
{
    /// <summary>
    /// Shared gameplay stat calculator for framework systems.
    /// Final stat formula is: (baseValue + additive) * multiplier.
    /// </summary>
    public class StatService : MonoSingleton<StatService>
    {
        public float GetAdditive(string statKey)
        {
            if (string.IsNullOrWhiteSpace(statKey))
                return 0f;

            return GetAugmentValue(statKey, GameplayModifierOperation.Add)
                   + GetTraitValue(statKey, GameplayModifierOperation.Add);
        }

        public float GetMultiplier(string statKey)
        {
            if (string.IsNullOrWhiteSpace(statKey))
                return 1f;

            return GetAugmentValue(statKey, GameplayModifierOperation.Multiply)
                   * GetTraitValue(statKey, GameplayModifierOperation.Multiply);
        }

        public float Evaluate(string statKey, float baseValue)
        {
            return (baseValue + GetAdditive(statKey)) * GetMultiplier(statKey);
        }

        public float Evaluate(string statKey, float baseValue, float extraAdditive, float extraMultiplier = 1f)
        {
            return (baseValue + GetAdditive(statKey) + extraAdditive) * (GetMultiplier(statKey) * extraMultiplier);
        }

        public bool HasModifier(string statKey)
        {
            return !Mathf.Approximately(GetAdditive(statKey), 0f)
                   || !Mathf.Approximately(GetMultiplier(statKey), 1f);
        }

        public StatBreakdown GetBreakdown(string statKey, float baseValue)
        {
            var additive = GetAdditive(statKey);
            var multiplier = GetMultiplier(statKey);
            return new StatBreakdown(baseValue, additive, multiplier);
        }

        private static float GetAugmentValue(string statKey, GameplayModifierOperation operation)
        {
            return AugmentManager.HasInstance
                ? AugmentManager.Instance.GetModifierValue(statKey, operation)
                : operation == GameplayModifierOperation.Multiply ? 1f : 0f;
        }

        private static float GetTraitValue(string statKey, GameplayModifierOperation operation)
        {
            return TraitManager.HasInstance
                ? TraitManager.Instance.GetModifierValue(statKey, operation)
                : operation == GameplayModifierOperation.Multiply ? 1f : 0f;
        }
    }

    public readonly struct StatBreakdown
    {
        public readonly float BaseValue;
        public readonly float Additive;
        public readonly float Multiplier;
        public readonly float FinalValue;

        public StatBreakdown(float baseValue, float additive, float multiplier)
        {
            BaseValue = baseValue;
            Additive = additive;
            Multiplier = multiplier;
            FinalValue = (baseValue + additive) * multiplier;
        }
    }
}
