FROM mcr.microsoft.com/playwright/dotnet:v1.44.0-jammy AS build
WORKDIR /src

COPY ["CvCreator.sln", "."]
COPY ["CvCreator.API/CvCreator.API.csproj", "CvCreator.API/"]
COPY ["CvCreator.Application/CvCreator.Application.csproj", "CvCreator.Application/"]
COPY ["CvCreator.Domain/CvCreator.Domain.csproj", "CvCreator.Domain/"]
COPY ["CvCreator.Infrastructure/CvCreator.Infrastructure.csproj", "CvCreator.Infrastructure/"]

RUN dotnet restore "CvCreator.sln"
RUN playwright install --with-deps
COPY . .

WORKDIR "/src/CvCreator.API"
RUN dotnet publish "CvCreator.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS final
WORKDIR /app

ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
COPY --from=build /app/publish .
COPY --from=build /ms-playwright /ms-playwright
EXPOSE 8080
ENTRYPOINT ["dotnet", "CvCreator.API.dll"]