using System;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.Core;
using GameFramework.Data;
using GameFramework.Services;

namespace GameFramework.Gameplay
{
    public class TraitManager : MonoSingleton<TraitManager>, ISavable
    {
        [SerializeField] private TraitDatabase database;
        [SerializeField] private int startingPoints = 3;

        private readonly Dictionary<string, int> _ranks = new();
        private int _availablePoints;

        public string SaveKey => "traits";
        public TraitDatabase Database => database;
        public IReadOnlyDictionary<string, int> Ranks => _ranks;
        public int AvailablePoints => _availablePoints;

        protected override void OnInitialize()
        {
            if (database == null)
                database = Resources.Load<TraitDatabase>("TraitDatabase");

            _availablePoints = Mathf.Max(_availablePoints, startingPoints);
            SaveManager.Instance.Register(this);
        }

        public void AddPoints(int amount)
        {
            if (amount <= 0)
                return;

            _availablePoints += amount;
            EventBus.Publish(new TraitPointsChangedEvent { AvailablePoints = _availablePoints });
        }

        public int GetRank(string traitId)
            => _ranks.TryGetValue(traitId, out var rank) ? rank : 0;

        public bool CanInvest(string traitId)
        {
            var trait = database?.Get(traitId);
            if (trait == null)
                return false;

            int rank = GetRank(traitId);
            return _availablePoints >= Mathf.Max(1, trait.pointCost) && rank < Mathf.Max(1, trait.maxRank);
        }

        public bool Invest(string traitId)
        {
            if (!CanInvest(traitId))
                return false;

            var trait = database.Get(traitId);
            int nextRank = GetRank(traitId) + 1;
            _ranks[traitId] = nextRank;
            _availablePoints -= Mathf.Max(1, trait.pointCost);

            EventBus.Publish(new TraitInvestedEvent
            {
                TraitId = traitId,
                NewRank = nextRank,
                AvailablePoints = _availablePoints
            });
            EventBus.Publish(new TraitPointsChangedEvent { AvailablePoints = _availablePoints });
            EventBus.Publish(new StatModifiersChangedEvent { Source = "Trait" });
            return true;
        }

        public float GetModifierValue(string statKey, GameplayModifierOperation operation)
        {
            float total = operation == GameplayModifierOperation.Multiply ? 1f : 0f;
            if (database == null)
                return total;

            foreach (var pair in _ranks)
            {
                var trait = database.Get(pair.Key);
                if (trait == null)
                    continue;

                foreach (var modifier in trait.modifiersPerRank)
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
            public int availablePoints;
            public List<string> keys = new();
            public List<int> values = new();
        }

        public string CaptureState()
        {
            var data = new SaveData { availablePoints = _availablePoints };
            foreach (var pair in _ranks)
            {
                data.keys.Add(pair.Key);
                data.values.Add(pair.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            _ranks.Clear();
            _availablePoints = startingPoints;
            if (data == null)
                return;

            _availablePoints = data.availablePoints;
            for (int i = 0; i < Mathf.Min(data.keys.Count, data.values.Count); i++)
                _ranks[data.keys[i]] = data.values[i];

            EventBus.Publish(new TraitPointsChangedEvent { AvailablePoints = _availablePoints });
            EventBus.Publish(new StatModifiersChangedEvent { Source = "Trait" });
        }
    }
}
