FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Bot.Telegram/Bot.Telegram.csproj", "Bot.Telegram/"]
RUN dotnet restore "Bot.Telegram/Bot.Telegram.csproj"
COPY . .
WORKDIR "/src/Bot.Telegram"
RUN dotnet build "Bot.Telegram.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Bot.Telegram.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Bot.Telegram.dll"]
