# NODAL OS — Operations Guide

## Starting/Stopping

```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# Restart a specific service
docker-compose restart stealth-engine
```

## Monitoring

### Health
```bash
curl http://localhost:8787/health
```

### Metrics (Prometheus)
```bash
curl http://localhost:8787/metrics
```

Available metrics:
- `nodalos_tasks_total` — total stealth tasks
- `nodalos_tasks_active` — currently active
- `nodalos_runners_connected` — stealth runners online
- `nodalos_companion_runs_total` — companion runs

### Logs
```bash
docker-compose logs -f bridge
docker-compose logs -f stealth-engine
docker-compose logs --tail=100
```

## Handling Stuck Sessions

1. Identify stuck session:
```bash
curl http://localhost:8787/debug
```

2. Stop a specific run:
```bash
curl -X POST http://localhost:8787/api/runs/{runId}/stop
```

3. Force restart stealth engine:
```bash
docker-compose restart stealth-engine
```

## Proxy Management

### Adding proxies to running instance
1. Edit `stealth-engine/config/stealth.default.json`
2. Add to `proxy.staticProxies`
3. Restart: `docker-compose restart stealth-engine`

### Manual proxy rotation
```bash
# Via config update + restart above
```

## Backup & Restore

### Backup evidence
```bash
docker cp nodosaudit_bridge_1:/app/evidence ./backup/evidence-$(date +%Y%m%d)
```

### Backup config
```bash
cp config/chrome-lab.local.json ./backup/config-$(date +%Y%m%d).json
cp stealth-engine/config/stealth.default.json ./backup/stealth-config-$(date +%Y%m%d).json
```

## Troubleshooting

| Issue | Action |
|-------|--------|
| No stealth runner | Check `docker-compose logs stealth-engine` |
| CAPTCHA not solving | Verify API key in config |
| Extension not connecting | Verify token matches bridge |
| High memory usage | Reduce `StealthMaxSessions` |
