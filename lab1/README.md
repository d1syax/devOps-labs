# Лабораторна робота 1
## Тема: Розгортання Web-сервісу з автоматизацією
**Працював над лабораторною роботою:**
* **Бойко Данило Сергійович**

## Варіант індивідуальних завдань
N = 2 \
V<sub>2</sub> = (N % 2) + 1 = 1 \
V<sub>3</sub> = (N % 3) + 1 = 3 \
V<sub>5</sub> = (N % 5) + 1 = 3

## Документація по розробленому веб-застосунку
Simple Inventory - сервіс обліку обладнання

Об'єкт інвентарю містить наступні поля:
- id
- name
- quantity
- created_at

API сервісу складається з 3 ендпоінтів:
- `GET /items` — вивести список усіх предметів в інвентарі (id, name)
- `POST /items` (name, quantity) — створити новий запис у системі обліку
- `GET /items/<id>` — вивести детальну інформацію по запису (id, name, quantity, created_at)

Системні ендпоінти:
- `GET /` — повертає HTML-сторінку зі списком усіх ендпоінтів
- `GET /health/alive` — завжди повертає `HTTP 200 OK`
- `GET /health/ready` — перевіряє підключення до бази даних. Повертає `HTTP 200 OK`, якщо підключення успішне, інакше `HTTP 500`

## Порт застосунку та конфігурація
Конфігурація через аргументи командного рядка \
**Порт застосунку: 3000** \
**База даних: MariaDB**

## Реалізація веб-застосунку
- **Мова програмування:** C# (.NET 9)
- **Фреймворк:** ASP.NET Core Minimal API
- **ORM:** Entity Framework Core + Pomelo.EntityFrameworkCore.MySql

## Структура проекту
```
mywebapp/
├── src/
│   ├── Models/
│   │   └── Item.cs
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   └── AppDbContextFactory.cs
│   ├── Migrations/
│   ├── Program.cs
│   └── mywebapp.csproj
├── deploy/
│   ├── init_db.sql
│   ├── mywebapp.service
│   ├── mywebapp.socket
│   └── nginx.conf
├── install.sh
└── README.md
```

# Налаштування середовища

### Клонування репозиторію
```bash
git clone https://github.com/d1syax/devOps-labs.git
cd mywebapp
```

### Локальний запуск (для розробки)
```bash
cd src
dotnet run -- --host 127.0.0.1 --port 3000 \
  --db-host 127.0.0.1 --db-port 3306 \
  --db-name mywebapp --db-user mywebapp --db-password mywebapp
```

# Документація по розгортанню

- Образ для віртуальної машини: [Ubuntu Server 22.04 LTS](https://ubuntu.com/download/server)

### Клонування репозиторію
```bash
git clone https://github.com/d1syax/devOps-labs.git
cd mywebapp
```

### Запуск скрипта розгортання
```bash
sudo bash install.sh
```

# Тестування

### 1. Nginx & API Endpoints

- Тест кореневого ендпоінту (має повернути HTML-сторінку)
```bash
curl -i -H "Accept: text/html" http://localhost/
```

- Тест health-check ендпоінтів
```bash
curl -i http://localhost/health/alive
curl -i http://localhost/health/ready
```

- Тест бізнес-логіки GET із заголовком Accept: application/json
```bash
curl -i -H "Accept: application/json" http://localhost/items
```

- Тест бізнес-логіки GET із заголовком Accept: text/html
```bash
curl -i -H "Accept: text/html" http://localhost/items
```

- Тест бізнес-логіки POST
```bash
curl -i -X POST -H "Content-Type: application/json" \
  -d '{"name":"Laptop","quantity":5}' http://localhost/items
```

- Тест отримання конкретного запису
```bash
curl -i -H "Accept: application/json" http://localhost/items/1
```

### 2. Користувачі та права доступу

- `teacher` user (пароль за замовчуванням: `12345678`)
```bash
su - teacher
sudo ls /root
exit
```

- `operator` user (пароль за замовчуванням: `12345678`)
```bash
su - operator
sudo systemctl restart mywebapp
sudo ls /root   # має повернути Permission denied
exit
```

- Підтвердження що користувач за замовчуванням заблокований
```bash
sudo passwd -S ubuntu
```

# Результати автоматизації

- Перевірка того, що веб-застосунок працює
```bash
sudo systemctl status mywebapp
```

- Перевірка того, що сокет активний
```bash
sudo systemctl status mywebapp.socket
```

- Перевірка того, що скрипт створив файл із номером варіанту
```bash
sudo cat /home/student/gradebook
```