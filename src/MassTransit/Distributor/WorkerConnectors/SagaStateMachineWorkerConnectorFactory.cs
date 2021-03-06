// Copyright 2007-2012 Chris Patterson, Dru Sellers, Travis Smith, et. al.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.Distributor.WorkerConnectors
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Magnum.Reflection;
    using Magnum.StateMachine;
    using Saga;
    using Saga.Configuration;

    public class SagaStateMachineWorkerConnectorFactory<TSaga> :
        IEnumerable<SagaWorkerConnector>
        where TSaga : SagaStateMachine<TSaga>, ISaga
    {
        readonly ISagaRepository<TSaga> _sagaRepository;
        ISagaPolicyFactory _policyFactory;

        public SagaStateMachineWorkerConnectorFactory(ISagaRepository<TSaga> sagaRepository)
        {
            _sagaRepository = sagaRepository;
            _policyFactory = new SagaPolicyFactory();
        }

        public IEnumerator<SagaWorkerConnector> GetEnumerator()
        {
            TSaga instance = FastActivator<TSaga>.Create(Guid.Empty);

            var inspector = new SagaStateMachineEventInspector<TSaga>();
            instance.Inspect(inspector);

            return inspector.GetResults()
                .Select(x => CreateConnector(x.SagaEvent.MessageType, x.SagaEvent.Event, x.States))
                .SelectMany(x => x.Create())
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        EventWorkerConnectorFactory CreateConnector(Type messageType, Event eevent, IEnumerable<State> states)
        {
            return (EventWorkerConnectorFactory)FastActivator.Create(typeof(EventWorkerConnectorFactory<,>),
                new[] {typeof(TSaga), messageType},
                new object[]
                    {
                        _sagaRepository, _policyFactory, eevent, states
                    });
        }
    }
}