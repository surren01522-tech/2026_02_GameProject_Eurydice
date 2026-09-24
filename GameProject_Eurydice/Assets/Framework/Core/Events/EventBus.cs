using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameFramework.Core
{
    /// <summary>
    /// 타입 기반 이벤트 버스. 시스템 간 결합도를 낮추는 핵심.
    /// 사용 예)
    ///   EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
    ///   EventBus.Publish(new EnemyKilledEvent { EnemyId = "slime" });
    ///   EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled); // OnDisable에서 필수!
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            _handlers[type] = _handlers.TryGetValue(type, out var d)
                ? Delegate.Combine(d, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var d)) return;

            var result = Delegate.Remove(d, handler);
            if (result == null) _handlers.Remove(type);
            else _handlers[type] = result;
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (!_handlers.TryGetValue(typeof(T), out var d)) return;

            // 구독자 중 하나가 예외를 던져도 나머지는 실행되도록 개별 호출
            foreach (var handler in d.GetInvocationList())
            {
                try { ((Action<T>)handler).Invoke(evt); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        /// <summary>씬 리셋 등 전체 초기화가 필요할 때만 사용.</summary>
        public static void Clear() => _handlers.Clear();
    }
}
