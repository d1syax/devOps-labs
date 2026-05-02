#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="/opt/mywebapp"

echo "==> [1] Installing packages..."
apt-get update -qq
apt-get install -y -qq mariadb-server mariadb-client nginx dotnet-sdk-8.0

echo "==> [2] Creating users..."
id -u app      &>/dev/null || useradd --system --no-create-home --shell /sbin/nologin app

id -u student  &>/dev/null || useradd -m -s /bin/bash student
echo "student:12345678" | chpasswd
chage -d 0 student
usermod -aG sudo student

id -u teacher  &>/dev/null || useradd -m -s /bin/bash teacher
echo "teacher:12345678" | chpasswd
chage -d 0 teacher
usermod -aG sudo teacher

id -u operator &>/dev/null || useradd -m -s /bin/bash operator
echo "operator:12345678" | chpasswd
chage -d 0 operator

cat > /etc/sudoers.d/operator << 'EOF'
operator ALL=(root) NOPASSWD: \
    /bin/systemctl start mywebapp, \
    /bin/systemctl stop mywebapp, \
    /bin/systemctl restart mywebapp, \
    /bin/systemctl status mywebapp, \
    /bin/systemctl reload nginx
EOF
chmod 440 /etc/sudoers.d/operator

echo "==> [3] Setting up MariaDB..."
systemctl enable mariadb --now
mysql -u root << 'SQL'
CREATE DATABASE IF NOT EXISTS `mywebapp` CHARACTER SET utf8mb4;
CREATE USER IF NOT EXISTS 'mywebapp'@'127.0.0.1' IDENTIFIED BY 'mywebapp';
GRANT ALL PRIVILEGES ON `mywebapp`.* TO 'mywebapp'@'127.0.0.1';
FLUSH PRIVILEGES;
SQL

echo "==> [4] Building app..."
systemctl stop mywebapp 2>/dev/null || true
mkdir -p "$APP_DIR"
cd "$REPO_DIR/src"
dotnet publish -c Release -r linux-x64 --self-contained true -o "$APP_DIR"
cp "$REPO_DIR/deploy/init_db.sql" "$APP_DIR/init_db.sql"
chown -R app:app "$APP_DIR"
chmod +x "$APP_DIR/mywebapp"

echo "==> [5] Running DB migration..."
mysql -h 127.0.0.1 -u mywebapp -pmywebapp mywebapp < "$REPO_DIR/deploy/init_db.sql"

echo "==> [6] Installing systemd service..."
cp "$REPO_DIR/deploy/mywebapp.service" /etc/systemd/system/mywebapp.service
cp "$REPO_DIR/deploy/mywebapp.socket"  /etc/systemd/system/mywebapp.socket
systemctl daemon-reload
systemctl stop mywebapp.socket 2>/dev/null || true
systemctl disable mywebapp.socket 2>/dev/null || true
systemctl enable --now mywebapp

echo "==> [7] Configuring nginx..."
cp "$REPO_DIR/deploy/nginx.conf" /etc/nginx/sites-available/mywebapp
ln -sf /etc/nginx/sites-available/mywebapp /etc/nginx/sites-enabled/mywebapp
rm -f /etc/nginx/sites-enabled/default
nginx -t && systemctl enable --now nginx && systemctl reload nginx

echo "==> [8] Writing gradebook..."
echo "2" > /home/student/gradebook
chown student:student /home/student/gradebook

echo "==> [9] Blocking default user..."
for u in ubuntu debian vagrant; do
    id -u "$u" &>/dev/null && usermod -L "$u" && echo "  Locked: $u"
done

echo ""
echo "Done! App running at http://localhost"