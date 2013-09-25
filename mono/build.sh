#!/bin/bash

CONFIGURATION=Debug
MONO_DIR=mono
CURRENT_DIR=`pwd`
LIB_DIR="$CURRENT_DIR"/build
export MSBuildExtensionsPath="$CURRENT_DIR"/xbuild

echo $MSBuildExtensionsPath
cd ..

if [ ! -d "$LIB_DIR" ]
then
	mkdir "$LIB_DIR"
fi

function buildComponent {
	xbuild /p:Configuration="$CONFIGURATION" src/"$1"/"$2"/"$2".csproj
	cp src/"$1"/"$2"/bin/"$CONFIGURATION"/"$2".dll "$LIB_DIR"
}

buildComponent core BrightstarDB

# TODO : Add build of REST service and tests here when ready.



