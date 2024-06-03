# Use the Ubuntu base image
FROM ubuntu:latest

# Define environment variables
ENV PROWLARR_VERSION="master" \
    ARCH="x64" \
    PROWLARR_USER="prowlarr" \
    PROWLARR_GROUP="prowlarr" \
    PROWLARR_DIR="/opt/Prowlarr" \
    PROWLARR_DATA_DIR="/var/lib/prowlarr"

# Update and install dependencies
RUN apt-get update && \
    apt-get install -y curl sqlite3 tar libicu-dev && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Create the prowlarr user and group
RUN groupadd -r $PROWLARR_GROUP && \
    useradd -r -g $PROWLARR_GROUP $PROWLARR_USER

# Download and install Prowlarr from GitHub
RUN mkdir -p $PROWLARR_DATA_DIR && \
    chown -R $PROWLARR_USER:$PROWLARR_GROUP $PROWLARR_DATA_DIR && \
    ARCH=$(dpkg --print-architecture) && \
    if [ "$ARCH" = "amd64" ]; then ARCH="x64"; fi && \
    if [ "$ARCH" = "arm64" ]; then ARCH="arm64"; fi && \
    if [ "$ARCH" = "armhf" ]; then ARCH="arm"; fi && \
    LATEST_RELEASE=$(curl -s https://api.github.com/repos/donderjoekel/Prowlarr/releases/latest | grep "tag_name" | cut -d '"' -f 4) && \
    LATEST_RELEASENOV=${LATEST_RELEASE#v} && \
    curl -L -o Prowlarr.tar.gz "https://github.com/donderjoekel/Prowlarr/releases/download/${LATEST_RELEASE}/Prowlarr.sourcerarr.${LATEST_RELEASENOV}.linux-${ARCH}.tar.gz" && \
    tar -xvzf Prowlarr.tar.gz && \
    mv Prowlarr $PROWLARR_DIR && \
    chown -R $PROWLARR_USER:$PROWLARR_GROUP $PROWLARR_DIR && \
    rm Prowlarr.tar.gz

# Create and configure the systemd service file
RUN echo '[Unit]\n\
    Description=Prowlarr Daemon\n\
    After=syslog.target network.target\n\
    \n\
    [Service]\n\
    User=prowlarr\n\
    Group=prowlarr\n\
    Type=simple\n\
    ExecStart=/opt/Prowlarr/Prowlarr -nobrowser -data=/var/lib/prowlarr/\n\
    TimeoutStopSec=20\n\
    KillMode=process\n\
    Restart=on-failure\n\
    \n\
    [Install]\n\
    WantedBy=multi-user.target' > /etc/systemd/system/prowlarr.service

# Expose the port used by Prowlarr
EXPOSE 9696

# Set the user and working directory
USER $PROWLARR_USER
WORKDIR $PROWLARR_DIR

# Start Prowlarr
CMD ["./Prowlarr", "-nobrowser", "-data=/var/lib/prowlarr/"]
