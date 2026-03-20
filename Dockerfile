FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Node.js for JS/CSS minification
RUN apt-get update && apt-get install -y --no-install-recommends nodejs npm && rm -rf /var/lib/apt/lists/*

# Install JS tooling first (layer cache friendly)
COPY package.json ./
RUN npm install

COPY ChimQuiz/ChimQuiz.csproj ChimQuiz/
RUN dotnet restore ChimQuiz/ChimQuiz.csproj

COPY . .

# Minify JS and CSS before publish
RUN npm run build

RUN dotnet publish ChimQuiz/ChimQuiz.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Run as non-root user (DS-0002 best practice)
RUN addgroup --system chimquiz && adduser --system --ingroup chimquiz chimquiz \
    && mkdir -p /data && chown chimquiz:chimquiz /data

COPY --from=build --chown=chimquiz:chimquiz /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DatabasePath=/data/chimquiz.db

ARG APP_VERSION=dev
ENV APP_VERSION=$APP_VERSION

USER chimquiz
EXPOSE 8080
ENTRYPOINT ["dotnet", "ChimQuiz.dll"]
