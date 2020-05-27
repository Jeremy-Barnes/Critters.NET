using CritterServer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CritterServer
{
    public class Game
    {
        User host;
        public int loops;
        public Game(User host)
        {
            this.host = host;
            this.loops = 0;
        }

        public void Run()
        {
            Stopwatch timer = new Stopwatch();

            while (loops < Int32.MaxValue)
            {
                timer.Start();

                loops++;
                timer.Stop();
                Thread.Sleep((TimeSpan.FromSeconds(1) - (timer.Elapsed*60))/60);

            }
        }

    }
}
