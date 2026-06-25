# NODAL OS — Deployment Guide

## Prerequisites

- **.NET 9 SDK** (for bridge)
- **Node.js 20+** (for stealth engine)
- **Docker + Docker Compose** (for production)
- **Chrome** (for extension, only companion mode)

## Local Development

### 1. Bridge
```bash
cd Codigo-m12-audit
dotnet run --project src/OneBrain.ChromeLab.Bridge/ \
  -- --host 127.0.0.1 --port 8787 --allow-lan
```

### 2. Stealth Engine
```bash
cd stealth-engine
npm install
npm start
```

### 3. Stealth Panel
```bash
cd stealth-panel
npm install
npx serve . -p 8789
```

### 4. Chrome Extension
- Open chrome://extensions
- Enable "Developer mode"
- Load unpacked from `browser-extension/onebrain-chrome-lab/`

## Docker Production

### Setup
```bash
cp .env.example .env
# Edit .env with your API keys
```

### Build & Start
```bash
docker-compose build
docker-compose up -d
```

### Verify
```bash
curl http://localhost:8787/health
# {"status":"healthy","service":"onebrain-chrome-lab-bridge","version":"0.1.0",...}

curl http://localhost:8787/metrics
# # HELP nodalos_tasks_total ...
```

### Stop
```bash
docker-compose down
```

### With volume cleanup
```bash
docker-compose down -v
```

## Verifying Installation

1. Health check: `curl http://localhost:8787/health`
2. Create a stealth task:
```bash
curl -X POST http://localhost:8787/api/runs \
  -H "Content-Type: application/json" \
  -d '{"instruction":"navigate to https://example.com","mode":"stealth"}'
```
3. Check metrics: `curl http://localhost:8787/metrics`
4. View logs: `docker-compose logs -f`
