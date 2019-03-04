using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.TestFramework;
using NUnit.Framework;

namespace MassTransitTests532
{
    public static class HeaderExtensions
    {
        private static readonly string UniqueIdHeaderName = "DD-UniqueId";

        public static int? GetUniqueId(this Headers headers)
        {
            if (headers.TryGetHeader(UniqueIdHeaderName, out object value))
            {
                return Convert.ToInt32(value);
            }

            return null;
        }

        public static void SetUniqueId(this SendHeaders headers, int value)
        {
            headers.Set(UniqueIdHeaderName, value);
        }
    }

    class A
    { }

    class B
    {
        public int? IdA { get; set; }
    }

    [TestFixture]
    public class Configuring_send_pipeline :
        InMemoryTestFixture
    {
        private int _currentId = 0;
        private readonly List<int> _issuedIds = new List<int>();

        private Task<ConsumeContext<B>> _handled;

        private int GetNextId()
        {
            int id = _currentId++;
            _issuedIds.Add(id);
            return id;
        }

        [Test]
        public async Task Should_have_unique_ids()
        {
            await InputQueueSendEndpoint.Send(new A());
            var bConsumeContext = await _handled;

            Assert.That(_issuedIds, Has.Count.EqualTo(2));
            Assert.That(bConsumeContext.Message.IdA, Is.EqualTo(_issuedIds[0]));
            Assert.That(bConsumeContext.Headers.GetUniqueId(), Is.EqualTo(_issuedIds[1]));
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            Handler<A>(configurator, ctx => ctx.Send(InputQueueAddress, new B { IdA = ctx.Headers.GetUniqueId() }));
            _handled = Handled<B>(configurator);
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            configurator.ConfigureSend(cfg => cfg.UseSendExecute(ctx => ctx.Headers.SetUniqueId(GetNextId())));
        }
    }
}