using System;
using System.Reactive.Linq;
using System.Threading;
using Tasker.Models;

namespace Tasker
{
    public interface ISensorStateUpdater
    {
        public delegate void SensorStateReceivedDelegate(SensorState state);

        event SensorStateReceivedDelegate? SensorStateReceived;
        void Start(CancellationToken cancellationToken);

        IObservable<SensorState> CreateObservableDataStream()
        {
            var states = Observable.FromEvent<SensorStateReceivedDelegate, SensorState>(handler =>
                {
                    void StateHandler(SensorState state)
                    {
                        handler(state);
                    }

                    return StateHandler;
                }, stateHandler => SensorStateReceived += stateHandler,
                stateHandler => SensorStateReceived -= stateHandler);
            return states;
        }
    }
}