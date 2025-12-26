using Furion.EventBus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchDog.src.Hubs;
using WatchDog.src.Managers;
using WatchDog.src.Models;
using YFJK.Furion.Library.Extension;
using YFJK.Furion.Library.Models;

namespace WatchDog.src.Events
{
    public class WatchDotMainEvents : IEventSubscriber
    {
        private readonly WatchdogSubscribe _watchdogSubscribe;

        public WatchDotMainEvents(WatchdogSubscribe watchdogSubscribe)
        {
            _watchdogSubscribe = watchdogSubscribe;
        }

        [EventSubscribe(Consts.WatchDogMainLogEventBroadcastWatchLog)]
        public async Task WatchDogMainLogEventBroadcastWatchLog(EventHandlerExecutingContext context)
        {
            var data = context.Source.Payload as WatchLog;

            await DynamicDBManager.InsertWatchLog(data);
            await _watchdogSubscribe.BroadcastWatchLog(data);
        }
        [EventSubscribe(Consts.WatchDogMainLogEventBroadcastLog)]
        public async Task WatchDogMainLogEventBroadcastLog(EventHandlerExecutingContext context)
        {
            var data = context.Source.Payload as WatchLoggerModel;
            await DynamicDBManager.InsertLog(data);
            await _watchdogSubscribe.BroadcastLog(data);
        }
        [EventSubscribe(Consts.WatchDogMainLogEventBroadcastExLog)]
        public async Task WatchDogMainLogEventBroadcastExLog(EventHandlerExecutingContext context)
        {
            var data = context.Source.Payload as WatchExceptionLog;
            await DynamicDBManager.InsertWatchExceptionLog(data);
            await _watchdogSubscribe.BroadcastExLog(data);
        }
    }
}
