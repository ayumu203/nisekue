# syntax=docker/dockerfile:1

# バックエンド (server/) のコンテナイメージ。
# amd64 / arm64 (Oracle Cloud Ampere A1 など) の両方でビルドできるよう、
# SDK ステージはビルドマシンのアーキテクチャで動かし、出力だけをターゲット向けにする。

ARG DOTNET_VERSION=10.0

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
ARG TARGETARCH
WORKDIR /src

# 依存関係の復元をソースコピーより先に行い、レイヤーキャッシュを効かせる
COPY server/server.csproj server/
RUN dotnet restore server/server.csproj -a "$TARGETARCH"

COPY server/ server/
RUN dotnet publish server/server.csproj \
    -c Release \
    -a "$TARGETARCH" \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0

COPY --from=build /app/publish .

# 公式イメージに含まれる非 root ユーザーで実行する
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "server.dll"]
