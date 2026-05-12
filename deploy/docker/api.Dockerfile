FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . /src/system/agency-campaign-os
COPY --from=archon-framework . /src/frameworks/archon-framework

WORKDIR /src/system/agency-campaign-os/AgencyCampaign
RUN dotnet restore "AgencyCampaign.Api/AgencyCampaign.Api.csproj"
RUN dotnet publish "AgencyCampaign.Api/AgencyCampaign.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    fonts-noto \
    libnss3 \
    libatk-bridge2.0-0 \
    libxss1 \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV CHROMIUM_EXECUTABLE_PATH=/usr/bin/chromium

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AgencyCampaign.Api.dll"]
