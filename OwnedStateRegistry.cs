using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DonQuixoteOverlay {
    internal sealed class OwnedStateRegistry<TTarget, TState> where TTarget : class {
        private sealed class ReferenceComparer : IEqualityComparer<TTarget> {
            public bool Equals(TTarget x, TTarget y) { return ReferenceEquals(x, y); }
            public int GetHashCode(TTarget value) { return RuntimeHelpers.GetHashCode(value); }
        }
        private readonly Dictionary<TTarget, TState> _states = new Dictionary<TTarget, TState>(new ReferenceComparer());
        internal int Count { get { return _states.Count; } }
        internal void Remember(TTarget target, TState state, bool firstOnly = true) {
            if (target == null || (firstOnly && _states.ContainsKey(target))) return;
            _states[target] = state;
        }
        internal bool TryGet(TTarget target, out TState state) { return _states.TryGetValue(target, out state); }
        internal void Restore(Action<TTarget, TState> restore, Predicate<TTarget> shouldRestore = null) {
            var snapshot = new KeyValuePair<TTarget, TState>[_states.Count];
            ((ICollection<KeyValuePair<TTarget, TState>>)_states).CopyTo(snapshot, 0);
            List<Exception> errors = null;
            foreach (var pair in snapshot) {
                if (shouldRestore != null && !shouldRestore(pair.Key)) continue;
                _states.Remove(pair.Key);
                try { restore(pair.Key, pair.Value); }
                catch (Exception ex) {
                    _states[pair.Key] = pair.Value; // Failed restores remain available for retry.
                    if (errors == null) errors = new List<Exception>();
                    errors.Add(ex);
                }
            }
            if (errors != null) throw new AggregateException("Game state restoration failed.", errors);
        }
    }

}
