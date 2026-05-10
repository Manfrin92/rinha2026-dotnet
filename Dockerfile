FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RinhaApi.csproj", "./"]
RUN dotnet restore "RinhaApi.csproj"
COPY . .
RUN dotnet publish "RinhaApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY ./references.json.gz /app/references.json.gz
ENTRYPOINT ["dotnet", "RinhaApi.dll"]
