using System.Text;
using System.Text.Json;
using KnowledgeVault.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace KnowledgeVault.Infrastructure.MessageQueue;

public class RabbitMqService : IMessageQueueService, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqService> _logger;

    private RabbitMqService(IConnection connection, IChannel channel, ILogger<RabbitMqService> logger)
    {
        _connection = connection;
        _channel = channel;
        _logger = logger;
    }

    public static async Task<RabbitMqService> CreateAsync(IConfiguration config, ILogger<RabbitMqService> logger)
    {
        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMQ:Host"] ?? "localhost",
            UserName = config["RabbitMQ:Username"] ?? "kv_user",
            Password = config["RabbitMQ:Password"] ?? "KvRabbit2024!"
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync("document.ingest", durable: true, exclusive: false, autoDelete: false);
        await channel.QueueDeclareAsync("document.ingest.dlq", durable: true, exclusive: false, autoDelete: false);

        logger.LogInformation("RabbitMQ connected to {Host}", factory.HostName);
        return new RabbitMqService(connection, channel, logger);
    }

    public async Task PublishAsync<T>(string queueName, T message, CancellationToken ct = default)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties { Persistent = true };

        await _channel.BasicPublishAsync("", queueName, mandatory: false, basicProperties: props, body: body, cancellationToken: ct);
        _logger.LogDebug("Published message to {Queue}", queueName);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        GC.SuppressFinalize(this);
    }
}
