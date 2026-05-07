FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . /src/system/agency-campaign-os
COPY --from=archon-framework . /src/frameworks/archon-framework

WORKDIR /src/system/agency-campaign-os/AgencyCampaign
RUN dotnet restore "AgencyCampaign.Api/AgencyCampaign.Api.csproj"
RUN dotnet publish "AgencyCampaign.Api/AgencyCampaign.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AgencyCampaign.Api.dll"]
