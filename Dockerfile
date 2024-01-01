FROM mono:5.18.1

RUN mkdir -p /usr/src/app/source /usr/src/app/build
WORKDIR /usr/src/app/source

COPY . /usr/src/app/source
RUN nuget restore -NonInteractive
RUN xbuild /property:Configuration=Release /property:OutDir=/usr/src/app/build/
# RUN msbuild /t:Restore /p:Configuration=Release /p:OutDir=/usr/src/app/build/
WORKDIR /usr/src/app/build

ENTRYPOINT [ "mono",  "BDInfo.exe" ]
