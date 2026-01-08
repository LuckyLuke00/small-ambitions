using UnityEngine;

namespace SmallAmbitions
{
    public enum MotiveType
    {
        Energy,
    }

    [System.Serializable]
    public sealed class Motive
    {
        [field: SerializeField] public float BaseDecayRate { get; private set; } = 1f;
        [field: SerializeField] public float CurrentValue { get; private set; } = 100f;
        [field: SerializeField] public float MaxValue { get; private set; } = 100f;
        [field: SerializeField] public float minValue { get; private set; } = 0f;

        public Motive()
        {
            CurrentValue = Mathf.Clamp(CurrentValue, minValue, MaxValue);
        }

        public Motive(Motive other)
        {
            BaseDecayRate = other.BaseDecayRate;
            CurrentValue = other.CurrentValue;
            MaxValue = other.MaxValue;
            minValue = other.minValue;

            CurrentValue = Mathf.Clamp(CurrentValue, minValue, MaxValue);
        }

        public void FillToMax()
        {
            CurrentValue = MaxValue;
        }

        public void AddValue(float amount)
        {
            CurrentValue = Mathf.Clamp(CurrentValue + amount, minValue, MaxValue);
        }

        public void Decay(float deltaTime)
        {
            CurrentValue = Mathf.Clamp(CurrentValue - (BaseDecayRate * deltaTime), minValue, MaxValue);
        }
    }
}
