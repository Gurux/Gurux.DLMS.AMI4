#!/bin/sh
# End commands with comments so this script also works with CRLF via sh start.sh.
cd -- "$(dirname -- "$0")/agent" || exit 1 #
exec dotnet Gurux.DLMS.AMI.Agent.dll "$@" #