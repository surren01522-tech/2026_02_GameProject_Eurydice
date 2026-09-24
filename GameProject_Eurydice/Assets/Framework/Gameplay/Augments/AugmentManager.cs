using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Core;
using GameFramework.Data;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    public class AugmentManager : MonoSingleton<AugmentManager>, ISavable
    {
        [SerializeField] private AugmentDatabase database;
        [SerializeField] private int defaultChoiceCount = 3;

        private readonly Dictionary<string, int> _selectedCounts = new();
        private readonly List<AugmentData> _currentChoices = new();
        private readonly System.Random _rng = new();

        public string SaveKey => "augments";
        public AugmentDatabase Database => database;
        public IReadOnlyList<AugmentData> CurrentChoices => _currentChoices;
        public IReadOnlyDictionary<string, int> SelectedCounts => _selectedCounts;

        protected override void OnInitialize()
        {
            if (database == null)
                database = Resources.Load<AugmentDatabase>("AugmentDatabase");

            SaveManager.Instance.Register(this);
            EventBus.Subscribe<AugmentChoicesRequestedEvent>(OnChoicesRequested);
        }

        protected override void OnDestroy()
        {
            EventBus.Unsubscribe<AugmentChoicesRequestedEvent>(OnChoicesRequested);
            base.OnDestroy();
        }

        private void OnChoicesRequested(AugmentChoicesRequestedEvent e)
            => OfferChoices(e.ChoiceCount > 0 ? e.ChoiceCount : defaultChoiceCount);

        public void OfferChoices(int choiceCount = 3)
        {
            _currentChoices.Clear();
            if (database == null)
                return;

            var pool = new List<AugmentData>();
            foreach (var augment in database.augments)
            {
                if (augment == null || string.IsNullOrWhiteSpace(augment.id))
                    continue;

                int picked = GetPickCount(augment.id);
                int maxPickCount = Mathf.Max(augment.maxPickCount, augment.unique ? 1 : augment.maxPickCount);
                if (augment.unique && picked > 0)
                    continue;
                if (maxPickCount > 0 && picked >= maxPickCount)
                    continue;

                pool.Add(augment);
            }

            Shuffle(pool);
            int finalCount = Mathf.Min(choiceCount, pool.Count);
            for (int i = 0; i < finalCount; i++)
                _currentChoices.Add(pool[i]);

            EventBus.Publish(new AugmentChoicesGeneratedEvent { ChoiceCount = _currentChoices.Count });
            UIManager.Instance.Show<AugmentChoicePopup>("AugmentChoicePopup", _currentChoices);
        }

        public bool SelectAugment(string augmentId)
        {
            var augment = database?.Get(augmentId);
            if (augment == null)
                return false;

            int picked = GetPickCount(augmentId);
            int maxPickCount = Mathf.Max(augment.maxPickCount, augment.unique ? 1 : augment.maxPickCount);
            if (augment.unique && picked > 0)
                return false;
            if (maxPickCount > 0 && picked >= maxPickCount)
                return false;

            _selectedCounts[augmentId] = picked + 1;

            if (!string.IsNullOrWhiteSpace(augment.rewardItemId) && augment.rewardAmount > 0)
                InventoryManager.Instance.AddItem(augment.rewardItemId, augment.rewardAmount);

            _currentChoices.Clear();
            EventBus.Publish(new AugmentSelectedEvent { AugmentId = augmentId });
            EventBus.Publish(new StatModifiersChangedEvent { Source = "Augment" });
            return true;
        }

        public int GetPickCount(string augmentId)
            => _selectedCounts.TryGetValue(augmentId, out var count) ? count : 0;

        public float GetModifierValue(string statKey, GameplayModifierOperation operation)
        {
            float total = operation == GameplayModifierOperation.Multiply ? 1f : 0f;
            if (database == null)
                return total;

            foreach (var pair in _selectedCounts)
            {
                var augment = database.Get(pair.Key);
                if (augment == null)
                    continue;

                foreach (var modifier in augment.modifiers)
                {
                    if (modifier == null || modifier.statKey != statKey || modifier.operation != operation)
                        continue;

                    if (operation == GameplayModifierOperation.Add)
                        total += modifier.value * pair.Value;
                    else
                        total *= Mathf.Pow(modifier.value, pair.Value);
                }
            }

            return total;
        }

        [Serializable]
        private class SaveData
        {
            public List<string> keys = new();
            public List<int> values = new();
        }

        public string CaptureState()
        {
            var data = new SaveData();
            foreach (var pair in _selectedCounts)
            {
                data.keys.Add(pair.Key);
                data.values.Add(pair.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            _selectedCounts.Clear();
            _currentChoices.Clear();
            if (data == null)
                return;

            for (int i = 0; i < Mathf.Min(data.keys.Count, data.values.Count); i++)
                _selectedCounts[data.keys[i]] = data.values[i];

            EventBus.Publish(new StatModifiersChangedEvent { Source = "Augment" });
        }

        private void Shuffle(List<AugmentData> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int swap = _rng.Next(i + 1);
                (list[i], list[swap]) = (list[swap], list[i]);
            }
        }
    }
}
