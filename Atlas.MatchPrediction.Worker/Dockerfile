FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Atlas.MatchPrediction.Worker/Atlas.MatchPrediction.Worker.csproj", "Atlas.MatchPrediction.Worker/"]
RUN dotnet restore "Atlas.MatchPrediction.Worker/Atlas.MatchPrediction.Worker.csproj"
COPY . .
WORKDIR "/src/Atlas.MatchPrediction.Worker"
RUN dotnet build "./Atlas.MatchPrediction.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Atlas.MatchPrediction.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Atlas.MatchPrediction.Worker.dll"]
