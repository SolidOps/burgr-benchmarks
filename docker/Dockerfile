ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
FROM ${DOTNET_SDK_IMAGE}

ARG CLAUDE_CODE_VERSION

RUN apt-get update \
    && apt-get install -y --no-install-recommends git curl ca-certificates ripgrep procps \
    && rm -rf /var/lib/apt/lists/* \
    && useradd --create-home --shell /bin/bash bench \
    && mkdir -p /work /bench/out /judge \
    && chown -R bench:bench /work /bench /judge

USER bench
ENV PATH=/home/bench/.local/bin:/home/bench/.dotnet/tools:$PATH \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DISABLE_AUTOUPDATER=1 \
    CLAUDE_CODE_DISABLE_NONESSENTIAL_TRAFFIC=1

RUN curl -fsSL https://claude.ai/install.sh | bash -s "${CLAUDE_CODE_VERSION}" \
    && claude --version \
    && git config --global user.name bench \
    && git config --global user.email bench@localhost \
    && git config --global init.defaultBranch main

# Warms the NuGet and Burgr caches with the packages of every case, so that a pass does not
# measure downloads. The copied cases are deleted in the same layer as they are used.
COPY --chown=bench:bench warm/ /tmp/warm/
RUN bash /tmp/warm/warm.sh && rm -rf /tmp/warm

COPY --chown=bench:bench pass.sh judge.sh /opt/bench/

WORKDIR /work
