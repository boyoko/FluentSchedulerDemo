using EasyNetQ;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentSchedulerDemo
{
    public sealed class AdvanceBusHelper
    {
        private volatile static IAdvancedBus _instance = null;
        private static readonly object lockHelper = new object();
        private AdvanceBusHelper() { }

        public static IAdvancedBus CreateInstance()
        {
            if (_instance == null)
            {
                lock (lockHelper)
                {
                    if (_instance == null)
                        _instance = RabbitHutch.CreateBus(
                            "host=192.168.203.128;requestedHeartbeat=300;timeout=0;username=admin;password=admin;prefetchcount=100")
                            .Advanced;


                }
            }
            return _instance;
        }
    }
}
