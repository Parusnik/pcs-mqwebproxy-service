﻿// Copyright (c) Parusnik.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Parusnik.Pcs.EventBus;
using Parusnik.Pcs.MqWebProxy.Api.Models;

namespace Parusnik.Pcs.MqWebProxy.Api.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class MqController : ControllerBase
    {
        private readonly IBus _bus;
        private readonly ILogger<MqController> _logger;

        public MqController(IBus bus, ILogger<MqController> logger)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Accepted)]
        public async Task<IActionResult> Post([FromBody] QueueMessage message, CancellationToken cancellationToken)
        {
            if (message == null)
            {
                return BadRequest();
            }

            var eventId = Guid.NewGuid();
            var @event = new IntegrationEvent(message.Message);

            try
            {

                var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{message.Topic}"));
                await endpoint.Send(@event, context =>
                {
                    context.MessageId = eventId;
                    context.CorrelationId = Guid.NewGuid();
                }, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending integration event: {IntegrationEventId} from {AppName} to queue {Queue} - ({@IntegrationEvent}).", eventId, Program.AppName, message.Topic, @event);

                throw;
            }

            return Accepted();
        }
    }
}
