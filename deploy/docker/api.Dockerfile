FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . /src/system/agency-campaign-os
COPY --from=archon-framework . /src/frameworks/archon-framework

WORKDIR /src/system/agency-campaign-os/AgencyCampaign
RUN dotnet restore "AgencyCampaign.Api/AgencyCampaign.Api.csproj"
RUN dotnet publish "AgencyCampaign.Api/AgencyCampaign.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# A imagem base do .NET 10 e Ubuntu Noble, onde "apt install chromium" instala
# apenas um stub do snap (nao funciona em container). Instalar o Google Chrome
# estavel do repositorio oficial garante um binario real para o PuppeteerSharp.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        wget \
        gnupg \
        ca-certificates \
        fonts-liberation \
        fonts-noto \
    && wget -q -O /tmp/google-chrome.deb https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb \
    && apt-get install -y --no-install-recommends /tmp/google-chrome.deb \
    && rm -f /tmp/google-chrome.deb \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV CHROMIUM_EXECUTABLE_PATH=/usr/bin/google-chrome-stable

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AgencyCampaign.Api.dll"]
