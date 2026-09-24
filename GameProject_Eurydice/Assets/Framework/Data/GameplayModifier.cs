using System;

namespace GameFramework.Data
{
    public enum GameplayModifierOperation
    {
        Add,
        Multiply
    }

    [Serializable]
    public class GameplayModifier
    {
        public string statKey = "attack_power";
        public GameplayModifierOperation operation = GameplayModifierOperation.Add;
        public float value = 1f;
    }
}
