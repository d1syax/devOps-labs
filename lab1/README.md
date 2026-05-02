# Simple Inventory Service
Project to understand how to deploy without docker
IM-41 Boiko Danylo

# Variables
+ Variant: 2 (V2 = 1, V3 = 3, V5 = 3)
+ Web-app: Simple Inventory
+ Config: command line arguments
+ App port: 3000
+ Database: MariaDB

# Local run app

### Start MariaDB via Docker
```bash
docker run -d \
  --name mariadb \
  -e MYSQL_ROOT_PASSWORD=root \
  -e MYSQL_DATABASE=mywebapp \
  -e MYSQL_USER=mywebapp \
  -e MYSQL_PASSWORD=mywebapp \
  -p 3306:3306 \
  mariadb:10.11
```

### Run app
```bash
cd src
dotnet run -- \
  --host 127.0.0.1 --port 3000 \
  --db-host 127.0.0.1 --db-port 3306 \
  --db-name mywebapp --db-user mywebapp --db-password mywebapp
```

# Linux run app
+ Use ubuntu [image](https://ubuntu.com/download/server)
+ Clone repo
```bash
git clone https://github.com/d1syax/devOps-labs.git
```
+ Go to project folder
```bash
cd devOps-labs/lab1
```
+ Run script
```bash
sudo bash install.sh
```

# API
```http request
# Return OK if app run
GET /health/alive
###
# Return OK if DB connected
GET /health/ready
###
# List of endpoints
GET /
Accept: text/html
###
# List of items (id, name)
GET /items
###
# Create a new item
POST /items
###
# Item details by id
GET /items/<id>
```

# Testing
Run these commands from the virtual machine terminal to verify the deployment, network restrictions, and user permissions.

### 1. Nginx & API Endpoints
+ Test root endpoint (should return HTML page)
```bash
curl -i -H "Accept: text/html" http://localhost/
```
+ Test health check
```bash
curl -i http://localhost/health/alive
curl -i http://localhost/health/ready
```
+ Test business logic GET with JSON accept header
```bash
curl -i -H "Accept: application/json" http://localhost/items
```
+ Test business logic GET with HTML accept header
```bash
curl -i -H "Accept: text/html" http://localhost/items
```
+ Test business logic POST (should return 201 Created)
```bash
curl -i -X POST -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{"name":"Laptop","quantity":5}' http://localhost/items
```
+ Test item details
```bash
curl -i -H "Accept: application/json" http://localhost/items/1
```

### 2. Users and Permissions
+ Test `teacher` user (Default password: `12345678`)
```bash
su - teacher
sudo ls /root  # Expectation: Success (has admin rights)
exit
```
+ Test `operator` user (Default password: `12345678`)
```bash
su - operator
sudo systemctl restart mywebapp  # Expectation: Success (allowed command)
sudo ls /root                    # Expectation: Permission denied
exit
```
+ Verify default cloud user is locked
```bash
sudo passwd -S ubuntu
```

### 3. Systemd Service & Automation Artifacts
+ Verify the app is running
```bash
sudo systemctl status mywebapp
```
+ Verify nginx is running
```bash
sudo systemctl status nginx
```
+ Verify MariaDB is running
```bash
sudo systemctl status mariadb
```
+ Verify the automation script created the grade book
```bash
sudo cat /home/student/gradebook
```