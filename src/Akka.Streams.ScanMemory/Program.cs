using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.Streams.Dsl;
using Akka.Util;

namespace Akka.Streams.ScanMemory
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var system = ActorSystem.Create("MySystem");
            var streamBits = Source.ActorRef<int>(1000, OverflowStrategy.DropHead)
                .GroupedWithin(100, TimeSpan.FromMilliseconds(100))
                .Scan(new SortedSet<int>(), (set, ints) =>
                {
                    foreach (var i in ints)
                        set.Add(i);

                    return set;
                }).PreMaterialize(system.Materializer());

            var source = streamBits.Item2;
            var actor = streamBits.Item1;

            system.Scheduler.ScheduleTellRepeatedly(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(1), actor, ThreadLocalRandom.Current.Next(), ActorRefs.NoSender);
            system.Scheduler.ScheduleTellOnce(TimeSpan.FromMinutes(10), actor, PoisonPill.Instance, ActorRefs.NoSender); // terminate stream after 10 minutes

            await source.RunForeach(i => { Console.WriteLine("[{0}]", string.Join(",", i)); }, system.Materializer());
            await system.Terminate();
        }
    }
}
