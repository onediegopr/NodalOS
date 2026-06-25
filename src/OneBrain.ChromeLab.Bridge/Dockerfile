FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY bin/Release/net11.0/publish/ ./

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production
ENV Bridge__Host=0.0.0.0
ENV Bridge__Port=8787

EXPOSE 8787
EXPOSE 8788

HEALTHCHECK --interval=30s --timeout=10s --retries=3 \
  CMD curl -f http://localhost:8787/health || exit 1

ENTRYPOINT ["dotnet", "OneBrain.ChromeLab.Bridge.dll"]
