FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Client.Telegram/Client.Telegram.csproj", "Client.Telegram/"]
RUN dotnet restore "Client.Telegram/Client.Telegram.csproj"
COPY . .
WORKDIR "/src/Client.Telegram"
RUN dotnet build "Client.Telegram.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Client.Telegram.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Client.Telegram.dll"]
