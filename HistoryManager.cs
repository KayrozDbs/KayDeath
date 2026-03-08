using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace KayDeath
{
    public class HistoryManager
    {
        private const int MaxHistory = 50;
        private readonly ConcurrentDictionary<ulong, Queue<DamageEvent>> _damageHistory = new();
        private readonly ConcurrentDictionary<ulong, DeathRecap> _lastDeaths = new();

        public void AddDamage(ulong objectId, DamageEvent damage)
        {
            if (!_damageHistory.TryGetValue(objectId, out var queue))
            {
                queue = new Queue<DamageEvent>();
                _damageHistory[objectId] = queue;
            }

            lock (queue)
            {
                queue.Enqueue(damage);
                while (queue.Count > MaxHistory)
                {
                    queue.Dequeue();
                }
            }
        }

        public void RegisterDeath(ulong objectId, string playerName, List<string>? rezs = null, List<string>? defensives = null)
        {
            if (_damageHistory.TryGetValue(objectId, out var queue))
            {
                lock (queue)
                {
                    if (queue.Count == 0) return;

                    var events = queue.ToList();
                    
                    // Mark the last event as killing blow only if none are already marked
                    if (!events.Any(e => e.IsKillingBlow))
                    {
                        var lastEvent = events.Last();
                        if ((DateTime.Now - lastEvent.Timestamp).TotalSeconds < 5)
                        {
                            lastEvent.IsKillingBlow = true;
                        }
                    }

                    var recap = new DeathRecap
                    {
                        PlayerName = playerName,
                        DeathTime = DateTime.Now,
                        Events = events,
                        AvailableRezs = rezs ?? new(),
                        UnusedDefensives = defensives ?? new()
                    };

                    _lastDeaths[objectId] = recap;
                    
                    // Clear history for this entity after death recap is generated
                    queue.Clear();
                }
            }
        }

        public DeathRecap? GetLastDeath(ulong objectId)
        {
            return _lastDeaths.TryGetValue(objectId, out var recap) ? recap : null;
        }

        public List<DeathRecap> GetAllDeaths()
        {
            return _lastDeaths.Values.OrderByDescending(d => d.DeathTime).ToList();
        }
    }
}
