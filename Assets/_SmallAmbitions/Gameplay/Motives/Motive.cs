using UnityEngine;

namespace SmallAmbitions
{
    public enum MotiveType
    {
        Ambition, // How desperately our hero pretends they'll be productive today
        Energy,   // How much coffee the character needs
    }

    [System.Serializable]
    public sealed class Motive
    {
        [Tooltip("The base rate at which this motive changes per second. Positive values increase the motive, negative values decrease it.")]
        [field: SerializeField] public float BaseRate { get; private set; } = 1f;
        [field: SerializeField] public float CurrentValue { get; private set; } = 100f;
        [field: SerializeField] public float MaxValue { get; private set; } = 100f;
        [field: SerializeField] public float MinValue { get; private set; } = 0f;

        private float _rateModifier = 0f;

        public Motive()
        {
            CurrentValue = Mathf.Clamp(CurrentValue, MinValue, MaxValue);
        }

        public Motive(Motive other)
        {
            BaseRate = other.BaseRate;
            CurrentValue = other.CurrentValue;
            MaxValue = other.MaxValue;
            MinValue = other.MinValue;

            CurrentValue = Mathf.Clamp(CurrentValue, MinValue, MaxValue);
        }

        public void FillToMax()
        {
            CurrentValue = MaxValue;
        }

        public void AddValue(float amount)
        {
            CurrentValue = Mathf.Clamp(CurrentValue + amount, MinValue, MaxValue);
        }

        public void AddRateModifier(float delta)
        {
            _rateModifier += delta;
        }

        public void RemoveRateModifier(float delta)
        {
            _rateModifier -= delta;
        }

        public void Tick(float deltaTime)
        {
            float rate = BaseRate + _rateModifier;
            CurrentValue = Mathf.Clamp(CurrentValue + rate * deltaTime, MinValue, MaxValue);
        }
    }
}
