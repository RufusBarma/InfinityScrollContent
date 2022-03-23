FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RedditToken/RedditToken.csproj", "RedditToken/"]
RUN dotnet restore "RedditToken/RedditToken.csproj"
COPY . .
WORKDIR "/src/RedditToken"
RUN dotnet build "RedditToken.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RedditToken.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RedditToken.dll"]
