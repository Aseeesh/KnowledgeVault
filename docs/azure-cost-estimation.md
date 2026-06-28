# Azure Cost Estimation — KnowledgeVault

## Free Tier Resources

| Service | SKU | Free Allowance | Monthly Cost |
|---------|-----|---------------|-------------|
| App Service (API) | F1 | 60 CPU-min/day, 1GB RAM | $0 |
| App Service (AI) | B1 | — | ~$13 |
| PostgreSQL Flexible | Burstable B1ms | 750hrs/mo (12 months) | $0* |
| Azure Cache for Redis | Basic C0 | — | ~$16 |
| Storage Account | Standard LRS | 5GB blob, 20K ops | $0 |
| Container Registry | Basic | 10GB storage | ~$5 |
| Static Web App | Free | 100GB bandwidth | $0 |
| Application Insights | — | 5GB/mo ingestion | $0 |
| Log Analytics | — | 5GB/mo | $0 |

**\* Free for first 12 months with Azure free account**

## Estimated Monthly Cost

| Scenario | Monthly Cost |
|----------|-------------|
| Free tier (first 12 months) | ~$18/mo |
| After free tier expires | ~$34/mo |
| With B1 for both API + AI | ~$47/mo |

## Cost Optimization Strategies

1. **Use F1 tier for API** — 60 CPU-minutes/day handles light portfolio demo traffic
2. **Azure Storage Queue replaces RabbitMQ** — Free tier covers ingestion needs
3. **Static Web App for frontend** — Free, global CDN, auto SSL
4. **Qdrant runs as sidecar** in AI Engine container (no separate service needed)
5. **Ollama not deployed to Azure** — Use API-based LLM (Groq free tier / Ollama on home server) for cloud deployment
6. **Set budget alerts** at $5, $10, $20 thresholds

## Scaling Path

When traffic grows beyond free tier:

| Tier | Trigger | Cost Impact |
|------|---------|-------------|
| F1 → B1 API | >60 CPU-min/day | +$13/mo |
| B1 → B2 AI | Memory pressure | +$13/mo |
| Basic → Standard Redis | >250MB cache | +$25/mo |
| Burstable → GP PostgreSQL | >1 vCore needed | +$50/mo |
