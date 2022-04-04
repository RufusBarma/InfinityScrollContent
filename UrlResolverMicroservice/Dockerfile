FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["UrlResolverMicroservice/UrlResolverMicroservice.csproj", "UrlResolverMicroservice/"]
RUN dotnet restore "UrlResolverMicroservice/UrlResolverMicroservice.csproj"
COPY . .
WORKDIR "/src/UrlResolverMicroservice"
RUN dotnet build "UrlResolverMicroservice.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UrlResolverMicroservice.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UrlResolverMicroservice.dll"]
