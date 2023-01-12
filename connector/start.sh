#!/bin/sh
#
# Parameters:
#   1 -- Application ID for dapr
#   2 -- Seconds to delay start

set -e

# Run balena base image entrypoint script
# /usr/bin/entry.sh echo ""

echo "balenaBlocks connector version: $(<VERSION)"
echo "------------------------------------------"
echo "Intelligently connecting data sources with data sinks"

if [ "x$DAPR_DEBUG" != "x" ]
then
    DAPR_LOGLEVEL="--log-level warn"
fi

# Place dapr components on tmpfs based file system so they don't remain on disk.
# Copy in any components the user already has placed in the target directory.
component_dir=/app/components
mv $component_dir /tmp
mkdir $component_dir
mount -t tmpfs -o mode=711 tmpfs $component_dir
rm -rf /tmp/components

# Allow time for networking and MQTT to stabilize.
sleep 10

./connector &
balena-idle