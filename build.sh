#!/bin/sh

echo -n $(date) > texmond/BuildDate.txt

/usr/bin/xbuild /p:Configuration=Release texmond.sln /p:TargetFrameworkVersion=v4.5 /p:TargetFrameworkProfile="" /p:DefineConstants=UNIX || exit $?

# If you want a stand-alone executable that could potentially run without Mono installed:
#PKG_CONFIG_PATH="/usr/lib/pkgconfig/" /usr/bin/mkbundle  --env "MONO_ENV_OPTIONS=-O=inline" --simple --deps --library /usr/lib/libMonoPosixHelper.so --library /usr/lib/libmono-btls-shared.so --machine-config /etc/mono/4.5/machine.config --config /etc/mono/config -L /usr/src/mono-6.0.0.319/mcs/class/lib/net_4_x-linux/ --static --deps -z -o texmond/bin/Release/texmond texmond/bin/Release/texmond.exe || exit $?

echo Done at $(date)
