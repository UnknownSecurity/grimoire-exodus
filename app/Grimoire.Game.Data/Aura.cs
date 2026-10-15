using System;
using System.Threading.Tasks;
using System.Threading;

namespace Grimoire.Game.Data
{
    public class Aura
    {
        public int entityID
        { 
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public int Value
        {
            get;
            set;
        } = 0;

        public string realValue
        {
            get;
            set;
        }

        public string disableType
        {
            get;
            set;
        }

        public int Duration
        {
            get
            {
                return Math.Max(0, durationSpan - (int)time().TotalMilliseconds);
            }
            set { }
        }

        public DateTime timeNow = new DateTime();

        public int durationSpan;

        private int countdownGeneration;

        public TimeSpan time()
        {
            return DateTime.Now - timeNow;
        }

        public async void countdown(Aura aura)
        {
            int generation = Interlocked.Increment(ref countdownGeneration);
            await Task.Delay(Math.Max(0, durationSpan));
            if (generation != Volatile.Read(ref countdownGeneration))
                return;

            lock (World.AuraSyncRoot)
            {
                World.PlayerAuras.Remove(aura);
                World.EnemyAuras.Remove(aura);
            }
            World.OnAuraExpired(aura);
        }

        public void CancelCountdown()
        {
            Interlocked.Increment(ref countdownGeneration);
        }

    }
}
