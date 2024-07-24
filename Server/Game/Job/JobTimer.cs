using System;
using System.Collections.Generic;
using System.Text;
using ServerCore;

namespace Server.Game
{
    struct JobTimerElem : IComparable<JobTimerElem>
    {
        public int execTick; // 실행 시간
        public IJob job;

        public int CompareTo(JobTimerElem other)
        {
            return other.execTick - execTick;
        }
    }

    public class JobTimer
    {
        PriorityQueue<JobTimerElem> _pq = new PriorityQueue<JobTimerElem>();
        object _lock = new object();

        public void Push(IJob job, int tickAfter = 0)
        {
            JobTimerElem jobTimerElem;
            jobTimerElem.execTick = Environment.TickCount + tickAfter;
            jobTimerElem.job = job;

            lock (_lock)
            {
                _pq.Push(jobTimerElem);
            }
        }

        public void Flush()
        {
            while (true)
            {
                int now = Environment.TickCount;

                JobTimerElem jobTimerElem;

                lock (_lock)
                {
                    if (_pq.Count == 0)
                        break;

                    jobTimerElem = _pq.Peek();
                    if (jobTimerElem.execTick > now)
                        break;

                    _pq.Pop();
                }

                jobTimerElem.job.Execute();
            }
        }
    }
}
