using FreeRedis;
using Furion;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WatchDog.src.Interfaces;
using WatchDog.src.Models;

namespace WatchDog.src.Hubs
{
    public class WatchdogSubscribe
    {
        private readonly RedisClient redisClient;
        private readonly IBroadcastHelper _broadcastHelper;

        public WatchdogSubscribe(RedisClient redisClient, IBroadcastHelper broadcastHelper)
        {
            this.redisClient = redisClient;
            _broadcastHelper = broadcastHelper;
        }

        internal void InitSubscribe()
        {
            if (redisClient != null)
            {
                redisClient.Subscribe(Consts.WatchDogMessageSubject, async (channel, message) =>
                {
                    try
                    {
                        WatchDogMessageSubjectBaseModel subChannelMessageBase = JsonConvert.DeserializeObject<WatchDogMessageSubjectBaseModel>(message.ToString());
                        switch (subChannelMessageBase?.type)
                        {
                            case "BroadcastWatchLog":
                                await _broadcastHelper.BroadcastWatchLog(JsonConvert.DeserializeObject<WatchLog>(subChannelMessageBase.data));
                                break;
                            case "BroadcastLog":
                                await _broadcastHelper.BroadcastLog(JsonConvert.DeserializeObject<WatchLoggerModel>(subChannelMessageBase.data));
                                break;
                            case "BroadcastExLog":
                                await _broadcastHelper.BroadcastExLog(JsonConvert.DeserializeObject<WatchExceptionLog>(subChannelMessageBase.data));
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                });
                Console.WriteLine("开始监听WatchDog Redis事件");
            }
        }

        public async Task BroadcastWatchLog(WatchLog log)
        {
            if (redisClient != null)
            {
                var message = new WatchDogMessageSubjectBaseModel
                {
                    type = "BroadcastWatchLog",
                    data = JsonConvert.SerializeObject(log)
                };
                await redisClient.PublishAsync(Consts.WatchDogMessageSubject, JsonConvert.SerializeObject(message));
            }
            else
            {
                await _broadcastHelper.BroadcastWatchLog(log);
            }
        }

        public async Task BroadcastLog(WatchLoggerModel log)
        {
            if (redisClient != null)
            {
                var message = new WatchDogMessageSubjectBaseModel
                {
                    type = "BroadcastLog",
                    data = JsonConvert.SerializeObject(log)
                };
                await redisClient.PublishAsync(Consts.WatchDogMessageSubject, JsonConvert.SerializeObject(message));
            }
            else
            {
                await _broadcastHelper.BroadcastLog(log);
            }
        }

        public async Task BroadcastExLog(WatchExceptionLog log)
        {
            if (redisClient != null)
            {
                var message = new WatchDogMessageSubjectBaseModel
                {
                    type = "BroadcastExLog",
                    data = JsonConvert.SerializeObject(log)
                };
                await redisClient.PublishAsync(Consts.WatchDogMessageSubject, JsonConvert.SerializeObject(message));
            }
            else
            {
                await _broadcastHelper.BroadcastExLog(log);
            }
        }
    }
}
