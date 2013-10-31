#!/bin/bash

if [ -z "$CONFIGURATION" ] 
then
    CONFIGURATION=Debug
fi

if [ -z "$NUNIT_DIR" ] 
then
    NUNIT_DIR=../../NUnit-2.6.3
fi

NUNIT="$NUNIT_DIR"/bin/nunit-console.exe

NUNIT_OPTIONS=-labels -timeout=10000
function build {
    xbuild /p:Configuration="$CONFIGURATION" ../src/"$1"/"$2"/"$2".csproj
}

function runtest {
    mono "$NUNIT" ../src/"$1"/"$2"/bin/"$CONFIGURATION"/"$2".dll "$NUNIT_OPTIONS"
}

# Build Tests
build core BrightstarDB.Tests
#build core BrightstarDB.EntityFramework.Tests
#build core BrightstardB.InternalTests
#build core BrightstarDB.Server.Modules.Tests
#build core BrightstarDB.Server.IntegrationTests

# Run Tests
runtest core BrightstarDB.Tests
