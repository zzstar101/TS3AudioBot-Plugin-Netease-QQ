# 使用官方的alpine镜像，Docker会自动根据当前架构拉取正确的版本
FROM alpine:3.15

# 更新包并安装依赖
RUN apk update && apk add --no-cache \
    wget \
    unzip \
    opus-dev \
    youtube-dl \
    ffmpeg \
    icu-libs \
    krb5-libs \
    libgcc \
    libintl \
    libssl1.1 \
    libstdc++ \
    zlib

# 下载并安装.NET Core Runtime 3.1（根据当前架构自动选择版本）
RUN arch=$(uname -m) \
    && if [ "$arch" = "aarch64" ]; then \
        dotnet_arch="arm64"; \
       elif [ "$arch" = "x86_64" ]; then \
        dotnet_arch="x64"; \
       else \
        echo "Unsupported architecture: $arch"; \
        exit 1; \
       fi \
    && wget -O dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Runtime/3.1.23/dotnet-runtime-3.1.23-linux-$dotnet_arch.tar.gz \
    && mkdir -p /usr/share/dotnet \
    && tar -C /usr/share/dotnet -xzf dotnet.tar.gz \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet.tar.gz

# 下载TS3AudioBot
RUN mkdir -p /app \
    && wget -O /app/TS3AudioBot.zip https://github.com/Splamy/TS3AudioBot/releases/download/0.12.0/TS3AudioBot_dotnetcore3.1.zip \
    && unzip /app/TS3AudioBot.zip -d /app \
    && rm /app/TS3AudioBot.zip

# 创建用户和数据目录
RUN adduser -D ts3bot \
    && mkdir -p /app/data \
    && chown -R ts3bot:ts3bot /app

# 切换用户
USER ts3bot

# 设置工作目录
WORKDIR /app/data

# 暴露端口
EXPOSE 58913

# 启动命令
CMD ["dotnet", "/app/TS3AudioBot.dll", "--non-interactive"]
