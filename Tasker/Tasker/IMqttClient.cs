using System;
using System.Threading;
using System.Threading.Tasks;
using Tasker.Models;

namespace Tasker
{
    public interface IMqttClient
    {
        Task<IObservable<MqttStringMessage>> CreateMessageStreamAsync(CancellationToken token);
    }
}