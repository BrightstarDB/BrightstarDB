#!/bin/bash

CONFIGURATION=Debug
LIB_DIR=build
MONO_DIR=build
CURRENT_DIR=`pwd`
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
#buildComponent core BrightstarDB.Service
#buildComponent core BrightstarDB.ServerRunner

