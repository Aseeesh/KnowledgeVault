using System;

namespace KnowledgeVault.Infrastructure.Configuration;

public static class AppConfig
{
    public static string GetConnectionString(string name)
    {
        return name switch
        {
            "PostgreSQL" => GetPostgresConnectionString(),
            "Redis" => GetRedisConnectionString(),
            _ => throw new ArgumentException($"Unknown connection string: {name}")
        };
    }

    public static string GetPostgresConnectionString()
    {
        var host = GetEnv("POSTGRES_HOST", "localhost");
        var port = GetEnv("POSTGRES_PORT", "5432");
        var db = GetEnv("POSTGRES_DB", "knowledgevault");
        var user = GetEnv("POSTGRES_USER", "kv_user");
        var password = GetEnv("POSTGRES_PASSWORD", "KvDev2024!");
        return $"Host={host};Port={port};Database={db};Username={user};Password={password}";
    }

    public static string GetRedisConnectionString()
    {
        var host = GetEnv("REDIS_HOST", "localhost");
        var port = GetEnv("REDIS_PORT", "6379");
        var password = GetEnv("REDIS_PASSWORD", "KvRedis2024!");
        return $"{host}:{port},password={password}";
    }

    public static string GetQdrantHost()
    {
        return GetEnv("QDRANT_HOST", "localhost");
    }

    public static int GetQdrantGrpcPort()
    {
        return int.Parse(GetEnv("QDRANT_GRPC_PORT", "6334"));
    }

    public static int GetQdrantHttpPort()
    {
        return int.Parse(GetEnv("QDRANT_PORT", "6333"));
    }

    public static RabbitMqConfig GetRabbitMqConfig()
    {
        return new RabbitMqConfig
        {
            Host = GetEnv("RABBITMQ_HOST", "localhost"),
            Port = int.Parse(GetEnv("RABBITMQ_PORT", "5672")),
            Username = GetEnv("RABBITMQ_USER", "kv_user"),
            Password = GetEnv("RABBITMQ_PASSWORD", "KvRabbit2024!")
        };
    }

    public static string GetAIEngineUrl()
    {
        return GetEnv("AI_ENGINE_URL", "http://localhost:8000");
    }

    private static string GetEnv(string key, string defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    public class RabbitMqConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5672;
        public string Username { get; set; } = "kv_user";
        public string Password { get; set; } = "KvRabbit2024!";
    }
}