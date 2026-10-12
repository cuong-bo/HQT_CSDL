#!/usr/bin/env bash
# One-time preparation of a fresh Ubuntu 22.04 / 24.04 VPS. Run as root:  sudo bash setup-vps.sh [--harden-ssh]
#
# Installs Docker, enables the firewall (22 and 80 only), fail2ban and automatic security updates, and creates the
# "deploy" user. --harden-ssh also switches off SSH password login, but only after it has found a key for "deploy".
set -euo pipefail
[ "$(id -u)" = 0 ] || { echo "run as root" >&2; exit 1; }

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get -y upgrade
apt-get install -y ufw fail2ban unattended-upgrades docker.io docker-compose-v2 git curl

id deploy >/dev/null 2>&1 || adduser --disabled-password --gecos "" deploy
usermod -aG docker deploy
install -d -m 700 -o deploy -g deploy /home/deploy/.ssh
if [ -f /root/.ssh/authorized_keys ] && [ ! -s /home/deploy/.ssh/authorized_keys ]; then
    install -m 600 -o deploy -g deploy /root/.ssh/authorized_keys /home/deploy/.ssh/authorized_keys
fi

# Firewall: SSH and HTTP. PostgreSQL (5432) is never opened; only the API container talks to it.
# Note: Docker publishes ports around ufw, so never add a "ports:" line for the db or api containers.
ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw allow 80/tcp
ufw --force enable

systemctl enable --now fail2ban
dpkg-reconfigure -f noninteractive unattended-upgrades

if [ "${1:-}" = "--harden-ssh" ]; then
    if [ -s /home/deploy/.ssh/authorized_keys ]; then
        install -d /etc/ssh/sshd_config.d
        printf 'PasswordAuthentication no\nPermitRootLogin prohibit-password\n' > /etc/ssh/sshd_config.d/99-hardening.conf
        systemctl reload ssh || systemctl reload sshd
        echo "SSH password login is now off. Test a key login as 'deploy' in a NEW terminal before closing this one."
    else
        echo "Not hardening SSH: /home/deploy/.ssh/authorized_keys is empty (you would lock yourself out)." >&2
    fi
fi

install -d -o deploy -g deploy /opt/hue-travel /var/backups/hue-travel
echo "Done. Next: as 'deploy', clone the repository into /opt/hue-travel and follow deploy/README.md."
