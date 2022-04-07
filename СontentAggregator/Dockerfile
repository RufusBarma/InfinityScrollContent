FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["СontentAggregator/СontentAggregator.csproj", "СontentAggregator/"]
RUN dotnet restore "СontentAggregator/СontentAggregator.csproj"
COPY . .
WORKDIR "/src/СontentAggregator"
RUN dotnet build "СontentAggregator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "СontentAggregator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "СontentAggregator.dll"]
