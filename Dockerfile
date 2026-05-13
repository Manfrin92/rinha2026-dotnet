FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects
COPY ["RinhaApi/RinhaApi.csproj", "RinhaApi/"]
COPY ["DataPreprocessor/DataPreprocessor.csproj", "DataPreprocessor/"]

# Restore dependencies
RUN dotnet restore "RinhaApi/RinhaApi.csproj"
RUN dotnet restore "DataPreprocessor/DataPreprocessor.csproj"

# Copy source
COPY RinhaApi/ RinhaApi/
COPY DataPreprocessor/ DataPreprocessor/

# Run data preprocessor (loads references and creates preprocessed-data.bin)
WORKDIR /src/DataPreprocessor
RUN dotnet restore && dotnet run -c Release

# Copy preprocessed data to publish directory
RUN cp preprocessed-data.bin /publish-data.bin

# Publish RinhaApi
WORKDIR /src/RinhaApi
RUN dotnet publish "RinhaApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /publish-data.bin ./preprocessed-data.bin
COPY ./references.json.gz /app/references.json.gz
ENTRYPOINT ["dotnet", "RinhaApi.dll"]
