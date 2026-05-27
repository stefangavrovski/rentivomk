FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["RentivoMK/RentivoMK.csproj", "RentivoMK/"]
RUN dotnet restore "RentivoMK/RentivoMK.csproj"
COPY . .
WORKDIR "/src/RentivoMK"
RUN dotnet build "RentivoMK.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RentivoMK.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RentivoMK.dll"]