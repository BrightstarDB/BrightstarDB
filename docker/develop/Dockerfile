FROM ubuntu:14.04.1
MAINTAINER  Kal Ahmed "kal@brightstardb.com"

RUN apt-get update && apt-get install -y curl udev git

ENV MONO_VERSION 3.10.0

RUN curl http://download.mono-project.com/repo/xamarin.gpg -o xamarin.gpg \
  && apt-key add xamarin.gpg \
  && echo "deb http://download.mono-project.com/repo/debian wheezy main" >> /etc/apt/sources.list.d/mono-xamarin.list \
  && apt-get update \
  && apt-get install -y mono-devel=$MONO_VERSION-0xamarin1

RUN rm -rf /var/lib/apt/lists/*

RUN mozroots --import --sync --quiet

RUN mozroots --import --machine --sync \
    && yes | certmgr -ssl -m https://go.microsoft.com  \ 
    && yes | certmgr -ssl -m https://nugetgallery.blob.core.windows.net \
    && yes | certmgr -ssl -m https://nuget.org

RUN git clone https://github.com/BrightstarDB/BrightstarDB.git /bssrc \
    && cd /bssrc \
    && git checkout develop \
    && cd mono \
    && ./build.sh \
    && mkdir /BrightstarDB \
    && mkdir /BrightstarDB/service \
    && cp -r /bssrc/mono/build/service/* /BrightstarDB/service \
    && rm -rf /bssrc

EXPOSE 8090

ADD BrightstarService.exe.config /BrightstarDB/service/BrightstarService.exe.config

WORKDIR /BrightstarDB/service
CMD ["/usr/bin/mono", "BrightstarService.exe" ]


 




